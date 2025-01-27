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

        logger.LogInformation("Client connected to the realtime server.");

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
            throw new InvalidOperationException($"Internal error: attempting to start service session loop without a client WebSocket");
        }


        var transcription = new StringBuilder();

        var sendAudioTask = SendAudioInputAsync(handler, cancellationToken);
        var startResponseTask = _session.StartResponseAsync(cancellationToken);
        var handleResponsesTask = HandleResponseAsync(handler, transcription, cancellationToken);

        await Task.WhenAll(sendAudioTask, startResponseTask, handleResponsesTask).ConfigureAwait(false);
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

    private async ValueTask OnSpeechFinishedAsync(ConversationInputSpeechFinishedUpdate speechFinished, CancellationToken cancellationToken)
    {
        await _session!.StartResponseAsync(cancellationToken).ConfigureAwait(false);
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
            await handler.OnPlayAudioAsync(audioBytes);
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
