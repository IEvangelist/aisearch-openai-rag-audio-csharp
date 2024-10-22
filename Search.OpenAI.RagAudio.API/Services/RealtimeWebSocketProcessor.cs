namespace Search.OpenAI.RagAudio.API.Services;

internal sealed class RealtimeWebSocketProcessor(
    ILogger<RealtimeWebSocketProcessor> logger,
    IOptions<AzureOptions> options) : IToolRegistry, IDisposable
{
    private const string HeaderRequestId = "x-ms-client-request-id";

    private static readonly TokenRequestContext s_tokenRequestContext = new(["https://cognitiveservices.azure.com/.default"]);

    private readonly ClientWebSocket _clientWebSocket = new();
    private readonly Dictionary<string, Tool> _tools = new(StringComparer.OrdinalIgnoreCase);
    private readonly WebSocketForwarder _webSocketForwarder = new();
    private readonly AzureOptions _azureOptions = options.Value;

    private TokenCredential? _tokenCredential;
    private AzureKeyCredential? _azureKeyCredential;

    void IToolRegistry.RegisterTool(Tool tool) => _tools[tool.Name] = tool;

    void IDisposable.Dispose() => _clientWebSocket.Dispose();

    [MemberNotNull(nameof(_azureKeyCredential))]
    internal RealtimeWebSocketProcessor WithAzureKeyCredential(string key)
    {
        _azureKeyCredential = new(key);

        return this;
    }

    [MemberNotNull(nameof(_tokenCredential))]
    internal RealtimeWebSocketProcessor WithTokenCredential(TokenCredential tokenCredential)
    {
        _tokenCredential = tokenCredential;

        return this;
    }

    internal async Task ProcessAsync(HttpContext context, WebSocket serverWebSocket)
    {
        using var clientWebSocket = await ConnectToOpenAIRealtimeAsync(context);

        SessionState sessionState = new(IsPendingTools: true);

        var cancellationToken = context.RequestAborted;

        await _webSocketForwarder.ForwardWhileCommunicatingAsync(
            clientWebSocket,
            serverWebSocket,
            OnProcessClientMessageAsync,
            async message => await OnProcessServerMessageAsync(sessionState, message),
            cancellationToken);

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("Disconnecting from OpenAI Realtime WebSocket.");
        }
    }

    private async Task<ClientWebSocket> ConnectToOpenAIRealtimeAsync(HttpContext context)
    {
        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("Connecting to OpenAI Realtime WebSocket.");
        }

        var realtimeUri = _azureOptions.AzureOpenAIEndpoint.ToRealtimeUri(
            _azureOptions.AzureOpenAIDeployment);

        ClientWebSocket clientWebSocket = new();

        if (context.Request.Headers.TryGetValue(HeaderRequestId, out var value))
        {
            clientWebSocket.Options.SetRequestHeader(
                headerName: HeaderRequestId,
                headerValue: value);
        }

        if (_tokenCredential is not null)
        {
            var accessToken = await _tokenCredential.GetTokenAsync(
                s_tokenRequestContext, CancellationToken.None);

            var token = accessToken.Token;

            clientWebSocket.Options.SetRequestHeader("Authorization", $"Bearer {token}");
        }
        else
        {
            clientWebSocket.Options.SetRequestHeader("api-key", _azureKeyCredential!.Key);
        }

        await clientWebSocket.ConnectAsync(realtimeUri, CancellationToken.None);

        return clientWebSocket;
    }

    private async Task<ProcessorResult> OnProcessClientMessageAsync(MemoryStream message)
    {
        // TODO: there are more messages where some settings such as instructions or voice
        // could be set, need to handle all of them to ensure clients can't override server settings

        JsonNode? json = null;

        if (RealtimeMessage.GetMessageType(message) is "session.update")
        {
            json = JsonNode.Parse(message)!;

            if (json["session"] is JsonNode session)
            {
                if (Defaults.Instructions is { } instructions)
                {
                    session["instructions"] = instructions;
                }
                if (Defaults.Temperature is { } temperature)
                {
                    session["temperature"] = temperature;
                }
                if (Defaults.MaxToken is { } maxTokens)
                {
                    session["max_response_output_tokens"] = maxTokens;
                }
                session["tool_choice"] = _tools.Count is 0 ? "none" : "auto";
                session["tools"] = new JsonArray(items: [
                        .._tools.Values.Select(t => t.SchemaJsonNode.DeepClone())
                    ]);
            }
        }

        if (json is not null)
        {
            await WriteJsonToMemoryStreamAsync(json, message);
        }

        return new ProcessorResult(message);
    }

    private async Task<ProcessorResult> OnProcessServerMessageAsync(SessionState sessionState, MemoryStream message)
    {
        JsonNode? json = null;
        JsonNode? backwardJson = null;
        RealtimeMessage msg;
        var shouldForward = true;

        switch (RealtimeMessage.GetMessageType(message))
        {
            case "session.created":
                json = JsonNode.Parse(message)!;
                if (json["session"] is JsonNode session)
                {
                    session["instructions"] = "";
                    session["tools"] = new JsonArray();
                    session["tool_choice"] = "none";
                    session["max_response_output_tokens"] = null;
                }
                break;

            case "response.output_item.added":
                msg = new RealtimeMessage(json = JsonNode.Parse(message)!);
                if (msg.ItemType is "function_call")
                {
                    shouldForward = false;
                }
                break;

            case "conversation.item.created":
                msg = new RealtimeMessage(json = JsonNode.Parse(message)!);
                var type = msg.ItemType;
                if (type is "function_call" || type is "function_call_output")
                {
                    shouldForward = false;
                }
                break;

            case "response.output_item.done":
                msg = new RealtimeMessage(json = JsonNode.Parse(message)!);
                if (msg.ItemType is "function_call")
                {
                    shouldForward = false;
                    if (msg.ToolCallId is string toolCallId && msg.ToolCallName is string toolName)
                    {
                        sessionState = sessionState with { IsPendingTools = true };
                        if (_tools.TryGetValue(toolName, out var tool))
                        {
                            var result = await tool.Target.Invoke(msg.ToolCallArguments);

                            backwardJson = new JsonObject
                            {
                                ["type"] = "conversation.item.create",
                                ["item"] = new JsonObject
                                {
                                    ["type"] = "function_call_output",
                                    ["call_id"] = toolCallId,
                                    ["output"] = tool.Destination is ToolResultDestination.Server ? result : null
                                }
                            };

                            if (tool.Destination is ToolResultDestination.Client)
                            {
                                json = new JsonObject
                                {
                                    ["type"] = "extension.middle_tier_tool_response",
                                    ["previous_item_id"] = toolCallId,
                                    ["tool_name"] = toolName,
                                    ["tool_result"] = result
                                };
                                shouldForward = true;
                            }
                        }
                    }
                }
                break;

            case "response.done":
                if (sessionState.IsPendingTools)
                {
                    sessionState = sessionState with { IsPendingTools = false };
                    backwardJson = new JsonObject
                    {
                        ["type"] = "response.create"
                    };
                }
                msg = new RealtimeMessage(json = JsonNode.Parse(message)!);
                if (!msg.TrimToolResponses())
                {
                    json = null; // No changes, forward the original message
                }
                break;

            case "response.function_call_arguments.delta":
            case "response.function_call_arguments.done":
                shouldForward = false;
                break;
        }

        if (json != null && shouldForward)
        {
            await WriteJsonToMemoryStreamAsync(json, message);
        }

        MemoryStream? backwardMessage = null;
        if (backwardJson != null)
        {
            backwardMessage = new();
            await WriteJsonToMemoryStreamAsync(backwardJson, backwardMessage);
        }

        return new ProcessorResult(shouldForward ? message : null, backwardMessage);
    }

    private static async Task WriteJsonToMemoryStreamAsync(JsonNode json, MemoryStream stream)
    {
        // Clear the stream
        stream.SetLength(0);

        await using Utf8JsonWriter writer = new(stream);

        json.WriteTo(writer);

        writer.Flush();
    }
}
