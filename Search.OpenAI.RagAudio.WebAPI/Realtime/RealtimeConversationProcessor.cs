namespace Search.OpenAI.RagAudio.WebAPI.Realtime;

#pragma warning disable OPENAI002 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

public sealed class RealtimeConversationProcessor(
    AzureOpenAIClient client,
    IOptions<AzureOptions> options,
    ILogger<RealtimeConversationProcessor> logger) : IDisposable
{
    private readonly AzureOptions _options = options.Value;

    private RealtimeConversationSession? _session;
    public byte[] _webSocketReceiveBuffer = new byte[1024 * 8];

    void IDisposable.Dispose() => _session?.Dispose();

    public async Task ProcessAsync(
        WebSocket clientWebSocket,
        CancellationToken cancellationToken = default)
    {
        _session ??= await GetRealtimeConversationSessionAsync(cancellationToken);

        logger.LogInformation("Client connected to the realtime server.");

        await SendMessageToClientAsync(
                clientWebSocket,
                new ClientSendableConnectedMessage(
                    "Hello, you're connected to the ASP.NET Core Minimal API realtime server."),
                cancellationToken
            )
            .ConfigureAwait(false);

        logger.LogInformation("Sent greeting message.");

        await Task.WhenAny(
            HandleMessagesFromClientAsync(clientWebSocket, cancellationToken),
            HandleUpdatesFromServiceAsync(clientWebSocket, cancellationToken));
    }

    private async Task<RealtimeConversationSession?> GetRealtimeConversationSessionAsync(CancellationToken cancellationToken)
    {
        var conversationClient = client.GetRealtimeConversationClient(
            _options.AzureOpenAIDeployment ?? "gpt-4o-realtime-preview-1001");

        var session = await conversationClient.StartConversationSessionAsync(cancellationToken)
            .ConfigureAwait(false);

        var sessionOptions = new ConversationSessionOptions()
        {
            Voice = ConversationVoice.Echo,
            TurnDetectionOptions = ConversationTurnDetectionOptions.CreateServerVoiceActivityTurnDetectionOptions(),
            InputTranscriptionOptions = new()
            {
                Model = "whisper-1",
            },
            Instructions = $"""
                You are a helpful assistant.
                The user is listening to answers with audio, so it's **super** important that answers are _as short as possible_, a single sentence if at all possible.
                The current date is {DateTime.Now.ToLongDateString()}
                """
        };

        await session.ConfigureSessionAsync(sessionOptions, cancellationToken);

        return session;
    }

    /// <summary>
    /// The task that manages receipt of incoming simplified protocol messages from the frontend client.
    /// </summary>
    private async Task HandleMessagesFromClientAsync(WebSocket clientWebSocket, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(clientWebSocket);
        
        if (_session is null)
        {
            throw new InvalidOperationException(
                "Internal error: attempting to start client WebSocket loop without an active session");
        }

        var receiveResult = await clientWebSocket.ReceiveAsync(_webSocketReceiveBuffer, cancellationToken)
            .ConfigureAwait(false);

        await _session.StartResponseAsync(cancellationToken).ConfigureAwait(false);

        while (!receiveResult.CloseStatus.HasValue)
        {
            // Handle client-sent audio bytes.
            if (receiveResult is { MessageType: WebSocketMessageType.Binary })
            {
                var audioBytesFromClient = new ArraySegment<byte>(_webSocketReceiveBuffer, 0, receiveResult.Count);

                await _session.SendInputAudioAsync(
                        BinaryData.FromBytes(audioBytesFromClient),
                        cancellationToken
                    )
                    .ConfigureAwait(false);
            }
            else // Handle client-sent text-based messages.
            {
                logger.LogInformation("Received non-audio message from the client.");

                var rawMessageFromClient = Encoding.UTF8.GetString(_webSocketReceiveBuffer, 0, receiveResult.Count);

                logger.LogInformation("Raw client message: {Msg}", rawMessageFromClient);

                var clientMessage = JsonSerializer.Deserialize(
                    rawMessageFromClient, SerializationContext.Default.ClientReceivableMessageBase);

                if (clientMessage is ClientReceivableUserMessage clientUserMessage)
                {
                    await _session.AddItemAsync(
                            ConversationItem.CreateUserMessage([clientUserMessage.Text]), cancellationToken)
                        .ConfigureAwait(false);
                }
                else if (clientMessage is ClientReceivableClearBufferMessage)
                {
                    await _session.ClearInputAudioAsync(cancellationToken);
                }
                else
                {
                    throw new InvalidOperationException(
                        $"Unexpected message from client: {rawMessageFromClient}");
                }
            }

            receiveResult = await clientWebSocket.ReceiveAsync(_webSocketReceiveBuffer, cancellationToken)
                .ConfigureAwait(false);
        }
    }

    /// <summary>
    /// The task that manages the incoming updates from realtime API messages and model responses.
    /// </summary>
    private async Task HandleUpdatesFromServiceAsync(WebSocket clientWebSocket, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(clientWebSocket);

        if (_session is null)
        {
            throw new InvalidOperationException($"Internal error: attempting to start service session loop without a client WebSocket");
        }

        var transcription = new StringBuilder();

        await foreach (var update in _session.ReceiveUpdatesAsync(cancellationToken).ConfigureAwait(false))
        {
            var updateTask = update switch
            {
                ConversationSessionStartedUpdate conversationSessionStarted => OnConversationStartedAsync(
                    clientWebSocket,
                    conversationSessionStarted,
                    cancellationToken),

                ConversationInputSpeechStartedUpdate speechStarted => OnSpeechStartedAsync(
                    clientWebSocket,
                    speechStarted,
                    cancellationToken),

                ConversationInputSpeechFinishedUpdate speechFinished => OnSpeechFinishedAsync(
                    clientWebSocket,
                    speechFinished,
                    cancellationToken),

                ConversationItemStreamingPartDeltaUpdate outputDelta => OnStreamingOutputDeltaAsync(
                    clientWebSocket,
                    outputDelta,
                    transcription,
                    cancellationToken),

                ConversationItemStreamingFinishedUpdate itemFinished => OnStreamingFinishedAsync(
                    itemFinished,
                    cancellationToken),

                ConversationResponseFinishedUpdate responseFinished => OnResponseFinishedAsync(
                    responseFinished,
                    cancellationToken),

                ConversationItemStreamingAudioTranscriptionFinishedUpdate or ConversationItemStreamingTextFinishedUpdate =>
                    OnStreamingTextTranscriptionFinishedAsync(
                        clientWebSocket,
                        transcription,
                        cancellationToken),

                _ => Task.CompletedTask
            };

            await updateTask;
        }
    }

    private static async Task OnConversationStartedAsync(WebSocket clientWebSocket, ConversationSessionStartedUpdate conversationSessionStarted, CancellationToken cancellationToken)
    {
        await SendMessageToClientAsync(
                clientWebSocket,
                new ClientSendableControlMessage("Conversation started..."),
                cancellationToken
            )
            .ConfigureAwait(false);
    }

    private static async Task OnSpeechStartedAsync(WebSocket clientWebSocket, ConversationInputSpeechStartedUpdate speechStarted, CancellationToken cancellationToken)
    {
        await SendMessageToClientAsync(
                clientWebSocket,
                new ClientSendableControlMessage("Speech started..."),
                cancellationToken
            )
            .ConfigureAwait(false);
    }

    private static async Task OnSpeechFinishedAsync(WebSocket clientWebSocket, ConversationInputSpeechFinishedUpdate speechFinished, CancellationToken cancellationToken)
    {
        await SendMessageToClientAsync(
                clientWebSocket,
                new ClientSendableControlMessage("Speech finished..."),
                cancellationToken
            )
            .ConfigureAwait(false);
    }

    private async Task OnStreamingOutputDeltaAsync(WebSocket clientWebSocket, ConversationItemStreamingPartDeltaUpdate outputDelta, StringBuilder transcription, CancellationToken cancellationToken)
    {
        var text = outputDelta.Text ?? outputDelta.AudioTranscript;

        transcription.Append(text);

        logger.LogInformation("Appended text to transcription buffer: {Item}", text);

        if (outputDelta.AudioBytes is not null)
        {
            await clientWebSocket.SendAsync(
                    outputDelta.AudioBytes.ToArray(),
                    WebSocketMessageType.Binary,
                    endOfMessage: true,
                    cancellationToken
                )
                .ConfigureAwait(false);
        }
    }

    private async Task OnStreamingFinishedAsync(ConversationItemStreamingFinishedUpdate itemFinished, CancellationToken cancellationToken)
    {
        logger.LogInformation("Item finished: {Item}", itemFinished);

        if (!string.IsNullOrEmpty(itemFinished.FunctionName))
        {
            if (await itemFinished.GetFunctionCallOutputAsync([]) is { } output)
            {
                await _session!.AddItemAsync(output, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    private async Task OnResponseFinishedAsync(ConversationResponseFinishedUpdate responseFinished, CancellationToken cancellationToken)
    {
        logger.LogInformation("Response finished: {Item}", responseFinished);

        // If we added one or more function call results, instruct the model to respond to them.
        if (responseFinished.CreatedItems.Any(item => !string.IsNullOrEmpty(item.FunctionName)))
        {
            await _session!.StartResponseAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task OnStreamingTextTranscriptionFinishedAsync(WebSocket clientWebSocket, StringBuilder transcription, CancellationToken cancellationToken)
    {
        var text = transcription.ToString();

        logger.LogInformation("Transcription finished: {Item}", text);

        var message = new ClientSendableTranscriptionMessage("transcription", text);

        transcription.Clear();

        await SendMessageToClientAsync(clientWebSocket, message,cancellationToken)
            .ConfigureAwait(false);        
    }

    /// <summary>
    /// A helper that serializes and transmits a simplified protocol message that can be sent to a frontend client.
    /// </summary>
    private static async Task SendMessageToClientAsync<TMessage>(
        WebSocket clientWebSocket,
        TMessage message,
        CancellationToken cancellationToken = default) where TMessage : ClientSendableMessageBase
    {
        ArgumentNullException.ThrowIfNull(clientWebSocket);

        var serializedMessage = JsonSerializer.Serialize(message, SerializationContext.Default.ClientSendableMessageBase);

        var messageBytes = Encoding.UTF8.GetBytes(serializedMessage);

        await clientWebSocket.SendAsync(
            messageBytes,
            WebSocketMessageType.Text,
            endOfMessage: true,
            cancellationToken
        ).ConfigureAwait(false);
    }
}
