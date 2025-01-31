namespace Search.OpenAI.RagAudio.Web.Realtime;

public sealed class RealtimeConversationProcessor(
    AzureOpenAIClient client,
    RealtimeToolFactory toolFactory,
    ILocalStorageService localStorage,
    IOptions<AzureOptions> options,
    ILogger<RealtimeConversationProcessor> logger) : IDisposable
{
    private readonly AzureOptions _options = options.Value;
    private readonly HashSet<AIFunction> _registeredFunctions = [];

    private RealtimeConversationSession? _session;
    public byte[] _webSocketReceiveBuffer = new byte[1024 * 8];

    void IDisposable.Dispose() => _session?.Dispose();

    public async Task StartConversationAsync(
        IRealtimeConversationHandler handler,
        CancellationToken cancellationToken = default)
    {
        _session ??= await GetRealtimeConversationSessionAsync(cancellationToken);

        logger.LogInformation("Client connected to the real-time server.");

        _ = Task.Run(async () => await ProcessConversationAsync(handler, cancellationToken), cancellationToken);
    }

    private async ValueTask<ConversationVoice> TryGetPreferredVoiceAsync()
    {
        var preferredVoice = await localStorage.GetItemAsync<string>("voice");

        return preferredVoice?.ToLower() switch
        {
            "echo" => ConversationVoice.Echo,
            "shimmer" => ConversationVoice.Shimmer,
            _ => ConversationVoice.Alloy
        };
    }

    private async Task<RealtimeConversationSession?> GetRealtimeConversationSessionAsync(CancellationToken cancellationToken)
    {
        var conversationClient = client.GetRealtimeConversationClient(
            _options.AzureOpenAIDeployment ?? "gpt-4o-realtime-preview-1001");

        var session = await conversationClient.StartConversationSessionAsync(cancellationToken)
            .ConfigureAwait(false);

        var preferredVoice = await TryGetPreferredVoiceAsync();

        var sessionOptions = new ConversationSessionOptions()
        {
            Voice = preferredVoice,
            TurnDetectionOptions = ConversationTurnDetectionOptions.CreateServerVoiceActivityTurnDetectionOptions(),
            ContentModalities = ConversationContentModalities.Audio | ConversationContentModalities.Text,
            InputTranscriptionOptions = new() { Model = "whisper-1" },
            Instructions = RealtimeDefaults.SystemMessageInstructions,
        };

        foreach (var tool in toolFactory.CreateRealtimeTools())
        {
            _registeredFunctions.Add(tool);

            sessionOptions.Tools.Add(tool.ToConversationFunctionTool());
        }

        await session.ConfigureSessionAsync(sessionOptions, cancellationToken);

        return session;
    }

    private async Task ProcessConversationAsync(IRealtimeConversationHandler handler, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(handler);

        if (_session is null)
        {
            throw new InvalidOperationException(
                $"Internal error: '_session' is null.");
        }

        var transcription = new StringBuilder();

        // We'll start a task that gets the client's voice input to send to OpenAI.
        var sendAudioTask = SendAudioInputAsync(handler, cancellationToken);

        // The bot will greet us immediately upon connecting.
        // var startResponseTask = _session.StartResponseAsync(cancellationToken);

        // Starts the process of registering the callers handlers and transcription listeners.
        var handleResponsesTask = HandleResponseAsync(handler, transcription, cancellationToken);

        await Task.WhenAll(tasks: [
                sendAudioTask,
                // startResponseTask,
                handleResponsesTask
            ])
            .ConfigureAwait(false);
    }

    private async Task SendAudioInputAsync(IRealtimeConversationHandler handler, CancellationToken cancellationToken)
    {
        var audioReader = await handler.GetAudioReaderAsync(cancellationToken);

        using var stream = audioReader.AsStream();

        await _session!.SendInputAudioAsync(stream, cancellationToken).ConfigureAwait(false);
    }

    private async Task HandleResponseAsync(IRealtimeConversationHandler handler, StringBuilder transcription, CancellationToken cancellationToken)
    {
        await foreach (var update in _session!.ReceiveUpdatesAsync(cancellationToken).ConfigureAwait(false))
        {
            var conversationUpdateTask = handler.OnConversationUpdateAsync(update);
            var conversationStatusTask = handler.OnConversationStatusAsync(GetStatus(update.Kind));
            var continuationTask = update switch
            {
                ConversationErrorUpdate errorUpdate => OnConversationErrorAsync(errorUpdate),

                ConversationItemStreamingFinishedUpdate streamingFinished => OnConversationStreamingFinishedAsync(
                    streamingFinished,
                    cancellationToken),

                ConversationItemStreamingPartDeltaUpdate outputDelta => OnStreamingOutputDeltaAsync(
                    handler,
                    outputDelta,
                    transcription,
                    cancellationToken),

                ConversationResponseFinishedUpdate responseFinished => OnResponseFinishedAsync(
                    responseFinished,
                    cancellationToken),

                ConversationItemStreamingAudioTranscriptionFinishedUpdate or ConversationItemStreamingTextFinishedUpdate =>
                    OnStreamingTextTranscriptionFinishedAsync(
                        handler,
                        transcription,
                        cancellationToken),

                _ => Task.CompletedTask
            };

            await Task.WhenAll(
                    conversationUpdateTask,
                    conversationStatusTask,
                    continuationTask
                )
                .ConfigureAwait(false);
        }
    }

    private async Task OnConversationStreamingFinishedAsync(ConversationItemStreamingFinishedUpdate streamingFinished, CancellationToken cancellationToken)
    {
        if (await streamingFinished.GetFunctionCallOutputAsync(
                _registeredFunctions,
                logger,
                cancellationToken) is { } output)
        {
            logger.LogInformation("Attempting to add a conversation item: {Output}", output);

            await _session!.AddItemAsync(output, cancellationToken);
        }
    }
    private Task OnConversationErrorAsync(ConversationErrorUpdate error)
    {
        logger.LogError("Error code: {Code}, Error event id: {ErrorEventId}, Event id: {EventId}, Parameter: {Param}, Message: {Message}",
            error.ErrorCode, error.ErrorEventId, error.EventId, error.ParameterName, error.Message);

        return Task.CompletedTask;
    }

    private static RealtimeStatus GetStatus(ConversationUpdateKind kind)
    {
        return kind switch
        {
            ConversationUpdateKind.Unknown
                or ConversationUpdateKind.ResponseFinished
                or ConversationUpdateKind.ItemContentPartFinished
                or ConversationUpdateKind.InputTranscriptionFinished
                or ConversationUpdateKind.ItemContentPartFinished
                or ConversationUpdateKind.ItemStreamingFinished
                or ConversationUpdateKind.ItemStreamingPartAudioFinished
                or ConversationUpdateKind.ItemStreamingPartAudioTranscriptionFinished
                or ConversationUpdateKind.ItemStreamingPartTextFinished => RealtimeStatus.StandingBy,

            ConversationUpdateKind.Error => RealtimeStatus.Error,

            _ => RealtimeStatus.Conversating
        };
    }

    private async Task OnStreamingOutputDeltaAsync(IRealtimeConversationHandler handler, ConversationItemStreamingPartDeltaUpdate outputDelta, StringBuilder transcription, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrEmpty(outputDelta.Text))
        {
            transcription.Append(outputDelta.Text);
            logger.LogDebug("Appended text to transcription buffer: {Item}", outputDelta.Text);
        }

        if (!string.IsNullOrEmpty(outputDelta.AudioTranscript))
        {
            transcription.Append(outputDelta.AudioTranscript);
            logger.LogDebug("Appended audio-text to transcription buffer: {Item}", outputDelta.AudioTranscript);
        }

        if (outputDelta.AudioBytes is { } bytes)
        {
            var audioBytes = bytes.ToArray();
            await handler.OnAudioReceivedAsync(audioBytes);
        }
    }

    private async Task OnResponseFinishedAsync(ConversationResponseFinishedUpdate responseFinished, CancellationToken cancellationToken)
    {
        // If we added one or more function call results, instruct the model to respond to them.
        if (responseFinished.CreatedItems.Any(item => !string.IsNullOrEmpty(item.FunctionName)))
        {
            await _session!.StartResponseAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task OnStreamingTextTranscriptionFinishedAsync(IRealtimeConversationHandler handler, StringBuilder transcription, CancellationToken cancellationToken)
    {
        await handler.OnTranscriptReadyAsync(transcription.ToString());

        transcription.Clear();
    }
}
