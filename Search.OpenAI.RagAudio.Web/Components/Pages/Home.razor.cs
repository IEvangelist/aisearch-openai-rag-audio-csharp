namespace Search.OpenAI.RagAudio.Web.Components.Pages;

public sealed partial class Home(
    RealtimeConversationProcessor realtimeConversation,
    MicrophoneSignal signal,
    ILogger<Home> logger) : IRealtimeConversationHandler
{
    private bool _isListening = false;
    private string _message = "Start a conversation?";
    private string _control = "";
    private List<string> _transcript = [];

    private CancellationTokenSource _cancellationTokenSource = new();
    private GroundingFile? _selectedFile;
    private GroundingFile[] _groundingFiles = [];

    private Microphone? _microphone;
    private Speaker? _speaker;
    private PipeReader? _microphoneInput;

    private void OnMicrophoneAvailable(PipeReader microphoneInput)
    {
        _microphoneInput = microphoneInput;

        signal.MicrophoneAvailable();
    }

    private Task OnToggleListeningAsync()
    {
        if (_microphone is null || _speaker is null)
        {
            return Task.CompletedTask;
        }

        return InvokeAsync(async () =>
        {
            try
            {
                if (_isListening)
                {
                    await _cancellationTokenSource.CancelAsync();

                    await _speaker.ClearPlaybackAsync();

                    _isListening = false;
                }
                else
                {
                    await _cancellationTokenSource.CancelAsync();
                    _cancellationTokenSource = new();

                    await _microphone.StartAsync();

                    await StartSessionAsync();

                    _isListening = true;
                }
            }
            catch (Exception ex)
            {
                await DispatchExceptionAsync(ex);
            }
        });
    }

    private async Task StartSessionAsync()
    {
        if (_speaker is null)
        {
            return;
        }

        await realtimeConversation.StartConversationAsync(this, _cancellationTokenSource.Token);
    }

    Task IRealtimeConversationHandler.OnConversationUpdateAsync(ConversationUpdate? conversation) =>
        InvokeAsync(async () =>
        {
            if (conversation is null)
            {
                return;
            }

            //if (message is ClientSendableControlMessage control)
            //{
            //    _control = control.Action;
            //}

            //if (message is ClientSendableTextDeltaMessage delta)
            //{
            //}

            //if (message is ClientSendableSpeechStartedMessage started)
            //{
            //    await (_speaker?.ClearPlaybackAsync() ?? Task.CompletedTask);

            //    _message = started.Message;
            //}

            //if (message is ClientSendableConnectedMessage greeting)
            //{
            //    _message = greeting.Greeting;
            //}

            //if (message is ClientSendableTranscriptionMessage transcript)
            //{
            //    _transcript.Add(transcript.Transcription);
            //}

            //if (message is ExtensionMiddleTierToolResponse toolResponse)
            //{
            //    var result = JsonSerializer.Deserialize(
            //        toolResponse.ToolResult, SerializationContext.Default.ToolResult);

            //    if (result is null)
            //    {
            //        return;
            //    }

            //    _groundingFiles = [
            //        ..result.Sources.Select(
            //            static source => new GroundingFile(
            //                Id: source.ChunkId,
            //                Name: source.Title,
            //                Content: source.Chunk))
            //    ];
            //}

            StateHasChanged();
        });

    Task IRealtimeConversationHandler.OnPlayAudioAsync(byte[] audioBytes) =>
        InvokeAsync(async () =>
        {
            if (_speaker is not null)
            {
                logger.LogInformation("Enqueuing audio data, received {Count:0,0} bytes.", audioBytes.Length);

                var enqueued = await _speaker.EnqueueAsync(audioBytes);
                if (enqueued is false)
                {
                    logger.LogWarning("Unable to enqueue audio...");
                }

                StateHasChanged();
            }
            else
            {
                logger.LogWarning("Speaker is unavailable, unable to enqueue audio...");
            }
        });

    async Task<PipeReader> IRealtimeConversationHandler.GetAudioReaderAsync(CancellationToken cancellationToken)
    {
        await signal.WaitForMicrophoneAvailabilityAsync(cancellationToken);

        return _microphoneInput!;
    }
}

