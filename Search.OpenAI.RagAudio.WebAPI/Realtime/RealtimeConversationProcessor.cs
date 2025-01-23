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
                SerializationContext.Default.ClientSendableConnectedMessage,
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
            InputTranscriptionOptions = new()
            {
                Model = "whisper-1",
            }
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

        while (!receiveResult.CloseStatus.HasValue)
        {
            if (receiveResult is { MessageType: WebSocketMessageType.Binary })
            {
                logger.LogInformation("Received audio from the client.");

                var bytesReceivedFromClient = new ArraySegment<byte>(_webSocketReceiveBuffer, 0, receiveResult.Count);

                // Temporary workaround for pre-2.2 bug with SendInputAudioAsync(BinaryData)
                await SendAudioToServiceViaWorkaroundAsync(bytesReceivedFromClient, cancellationToken)
                    .ConfigureAwait(false);

                //await _session.SendInputAudioAsync(
                //        BinaryData.FromBytes(bytesReceivedFromClient),
                //        cancellationToken
                //    )
                //    .ConfigureAwait(false);
            }
            else
            {
                logger.LogInformation("Received non-audio message from the client.");

                var rawMessageFromClient = Encoding.UTF8.GetString(_webSocketReceiveBuffer, 0, receiveResult.Count);

                var clientMessage = JsonSerializer.Deserialize<IClientMessageDiscriminator>(
                    rawMessageFromClient, SerializationContext.Default.IClientMessageDiscriminator);

                if (clientMessage is ClientReceivableUserMessage clientUserMessage)
                {
                    await _session.AddItemAsync(
                        ConversationItem.CreateUserMessage([clientUserMessage.Text]), cancellationToken).ConfigureAwait(false);

                    await _session.StartResponseAsync(cancellationToken).ConfigureAwait(false);
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

        await foreach (var update in _session.ReceiveUpdatesAsync(cancellationToken).ConfigureAwait(false))
        {
            if (update is ConversationInputSpeechStartedUpdate)
            {
                logger.LogInformation("Starting speech...");

                await SendMessageToClientAsync(
                        clientWebSocket,
                        new ClientSendableSpeechStartedMessage(),
                        SerializationContext.Default.ClientSendableSpeechStartedMessage,
                        cancellationToken
                    )
                    .ConfigureAwait(false);
            }

            if (update is ConversationItemStreamingPartDeltaUpdate deltaUpdate)
            {
                logger.LogInformation("Handling streaming part delta update...");

                var contentIdForClient = $"{deltaUpdate.ItemId}-{deltaUpdate.ContentPartIndex}";

                if (!string.IsNullOrEmpty(deltaUpdate.Text))
                {
                    logger.LogInformation("Handling text...");

                    var clientDeltaMessage = new ClientSendableTextDeltaMessage(deltaUpdate.Text, contentIdForClient);

                    await SendMessageToClientAsync(
                            clientWebSocket,
                            clientDeltaMessage,
                            SerializationContext.Default.ClientSendableTextDeltaMessage,
                            cancellationToken
                        )
                        .ConfigureAwait(false);
                }

                if (!string.IsNullOrEmpty(deltaUpdate.AudioTranscript))
                {
                    logger.LogInformation("Handling audio transcript...");

                    var clientDeltaMessage = new ClientSendableTextDeltaMessage(deltaUpdate.AudioTranscript, contentIdForClient);

                    await SendMessageToClientAsync(
                            clientWebSocket,
                            clientDeltaMessage,
                            SerializationContext.Default.ClientSendableTextDeltaMessage,
                            cancellationToken
                        )
                        .ConfigureAwait(false);
                }

                if (deltaUpdate.AudioBytes is not null)
                {
                    logger.LogInformation("Handling audio bytes...");

                    await clientWebSocket.SendAsync(
                            deltaUpdate.AudioBytes.ToArray(),
                            WebSocketMessageType.Binary,
                            endOfMessage: true,
                            cancellationToken
                        )
                        .ConfigureAwait(false);
                }
            }

            if (update is ConversationInputTranscriptionFinishedUpdate transcriptionFinishedUpdate)
            {
                logger.LogInformation("Handling finished input transcription...");

                var transcriptionMessage = new ClientSendableTranscriptionMessage(
                    transcriptionFinishedUpdate.ItemId,
                    transcriptionFinishedUpdate.Transcript);

                await SendMessageToClientAsync(
                        clientWebSocket,
                        transcriptionMessage,
                        SerializationContext.Default.ClientSendableTranscriptionMessage,
                        cancellationToken
                    )
                    .ConfigureAwait(false);
            }
        }
    }

    /// <summary>
    /// A helper that serializes and transmits a simplified protocol message that can be sent to a frontend client.
    /// </summary>
    private static async Task SendMessageToClientAsync<TMessage>(
        WebSocket clientWebSocket,
        TMessage message,
        JsonTypeInfo<TMessage> jsonTypeInfo,
        CancellationToken cancellationToken = default) where TMessage : IClientSendableMessage
    {
        ArgumentNullException.ThrowIfNull(clientWebSocket);

        var serializedMessage = JsonSerializer.Serialize(message, jsonTypeInfo);

        var messageBytes = Encoding.UTF8.GetBytes(serializedMessage);

        await clientWebSocket.SendAsync(
            messageBytes,
            WebSocketMessageType.Text,
            endOfMessage: true,
            cancellationToken
        ).ConfigureAwait(false);
    }

    private async Task SendAudioToServiceViaWorkaroundAsync(ArraySegment<byte> audioSegment, CancellationToken cancellationToken = default)
    {
        if (_session is null)
        {
            throw new InvalidOperationException(
                "Internal error: attempting to send audio to service with no active session");
        }

        var base64Audio = Convert.ToBase64String(audioSegment);
        var audioBody = BinaryData.FromString($$"""
            {
                "type": "input_audio_buffer.append",
                "audio": "{{base64Audio}}"
            }
            """);

        var cancellationOptions = new RequestOptions()
        {
            CancellationToken = cancellationToken,
        };

        await _session.SendCommandAsync(audioBody, cancellationOptions)
            .ConfigureAwait(false);
    }
}
