namespace Search.OpenAI.RagAudio.Web.Realtime;

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

    public async Task StartConversationAsync(
        IRealtimeConversationHandler handler,
        CancellationToken cancellationToken = default)
    {
        _session ??= await GetRealtimeConversationSessionAsync(cancellationToken);

        logger.LogInformation("Client connected to the real-time server.");

        _ = Task.Run(async () => await ProcessConversationAsync(handler, cancellationToken), cancellationToken);
    }

    private async Task<RealtimeConversationSession?> GetRealtimeConversationSessionAsync(CancellationToken cancellationToken)
    {
        var conversationClient = client.GetRealtimeConversationClient(
            _options.AzureOpenAIDeployment ?? "gpt-4o-realtime-preview-1001");

        var session = await conversationClient.StartConversationSessionAsync(cancellationToken)
            .ConfigureAwait(false);

        var sessionOptions = new ConversationSessionOptions()
        {
            Voice = ConversationVoice.Shimmer,
            TurnDetectionOptions = ConversationTurnDetectionOptions.CreateServerVoiceActivityTurnDetectionOptions(),
            ContentModalities = ConversationContentModalities.Audio,
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
        var startResponseTask = _session.StartResponseAsync(cancellationToken);

        // Starts the process of registering the callers handlers and transcription listeners.
        var handleResponsesTask = HandleResponseAsync(handler, transcription, cancellationToken);

        await Task.WhenAll(tasks: [
                sendAudioTask,
                startResponseTask,
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
            await handler.OnConversationUpdateAsync(update);

            var updateTask = update switch
            {
                ConversationItemStreamingPartDeltaUpdate outputDelta => OnStreamingOutputDeltaAsync(
                    handler,
                    outputDelta,
                    transcription,
                    cancellationToken),

                ConversationItemStreamingFinishedUpdate itemFinished => OnStreamingFinishedAsync(
                    itemFinished,
                    cancellationToken),

                ConversationResponseFinishedUpdate responseFinished => OnResponseFinishedAsync(
                    responseFinished,
                    cancellationToken),

                ConversationInputSpeechFinishedUpdate speechFinished => OnSpeechFinishedAsync(
                    speechFinished,
                    cancellationToken),

                ConversationItemStreamingAudioTranscriptionFinishedUpdate or ConversationItemStreamingTextFinishedUpdate =>
                    OnStreamingTextTranscriptionFinishedAsync(
                        transcription,
                        cancellationToken),

                _ => ValueTask.CompletedTask
            };

            await updateTask;
        }
    }

    private ValueTask OnSpeechFinishedAsync(ConversationInputSpeechFinishedUpdate speechFinished, CancellationToken cancellationToken)
    {
        return ValueTask.CompletedTask;
    }

    private async ValueTask OnStreamingOutputDeltaAsync(IRealtimeConversationHandler handler, ConversationItemStreamingPartDeltaUpdate outputDelta, StringBuilder transcription, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrEmpty(outputDelta.Text))
        {
            transcription.Append(outputDelta.Text);
            logger.LogInformation("Appended text to transcription buffer: {Item}", outputDelta.Text);
        }

        if (!string.IsNullOrEmpty(outputDelta.AudioTranscript))
        {
            transcription.Append(outputDelta.AudioTranscript);
            logger.LogInformation("Appended audio-text to transcription buffer: {Item}", outputDelta.AudioTranscript);
        }

        if (outputDelta.AudioBytes is { } bytes)
        {
            var audioBytes = bytes.ToArray();
            await handler.OnAudioReceivedAsync(audioBytes);
        }

        switch (outputDelta.Kind)
        {
            case ConversationUpdateKind.Unknown:
                break;
            case ConversationUpdateKind.SessionStarted:
                break;
            case ConversationUpdateKind.SessionConfigured:
                break;
            case ConversationUpdateKind.ItemCreated:
                break;
            case ConversationUpdateKind.ConversationCreated:
                break;
            case ConversationUpdateKind.ItemDeleted:
                break;
            case ConversationUpdateKind.ItemTruncated:
                break;
            case ConversationUpdateKind.ResponseStarted:
                break;
            case ConversationUpdateKind.ResponseFinished:
                break;
            case ConversationUpdateKind.RateLimitsUpdated:
                break;
            case ConversationUpdateKind.ItemStreamingStarted:
                break;
            case ConversationUpdateKind.ItemStreamingFinished:
                break;
            case ConversationUpdateKind.ItemContentPartStarted:
                break;
            case ConversationUpdateKind.ItemContentPartFinished:
                break;
            case ConversationUpdateKind.ItemStreamingPartAudioDelta:
                break;
            case ConversationUpdateKind.ItemStreamingPartAudioFinished:
                break;
            case ConversationUpdateKind.ItemStreamingPartAudioTranscriptionDelta:
                break;
            case ConversationUpdateKind.ItemStreamingPartAudioTranscriptionFinished:
                break;
            case ConversationUpdateKind.ItemStreamingPartTextDelta:
                break;
            case ConversationUpdateKind.ItemStreamingPartTextFinished:
                break;
            case ConversationUpdateKind.ItemStreamingFunctionCallArgumentsDelta:
                break;
            case ConversationUpdateKind.ItemStreamingFunctionCallArgumentsFinished:
                break;
            case ConversationUpdateKind.InputSpeechStarted:
                break;
            case ConversationUpdateKind.InputSpeechStopped:
                break;
            case ConversationUpdateKind.InputTranscriptionFinished:
                break;
            case ConversationUpdateKind.InputTranscriptionFailed:
                break;
            case ConversationUpdateKind.InputAudioCommitted:
                break;
            case ConversationUpdateKind.InputAudioCleared:
                break;
            case ConversationUpdateKind.Error:
                break;
            default:
                break;
        }
    }

    private ValueTask OnStreamingFinishedAsync(ConversationItemStreamingFinishedUpdate itemFinished, CancellationToken cancellationToken)
    {
        return ValueTask.CompletedTask;
    }

    private async ValueTask OnResponseFinishedAsync(ConversationResponseFinishedUpdate responseFinished, CancellationToken cancellationToken)
    {
        logger.LogInformation("Response finished: {Item}", responseFinished);

        // If we added one or more function call results, instruct the model to respond to them.
        if (responseFinished.CreatedItems.Any(item => !string.IsNullOrEmpty(item.FunctionName)))
        {
            await _session!.StartResponseAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    private ValueTask OnStreamingTextTranscriptionFinishedAsync(StringBuilder transcription, CancellationToken cancellationToken)
    {
        return ValueTask.CompletedTask;
    }
}
