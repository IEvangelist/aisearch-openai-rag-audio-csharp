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

    private RealtimeStatus _status;

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
                await _cancellationTokenSource.CancelAsync();
                _cancellationTokenSource = new();

                if (_isListening)
                {
                    await _speaker.ClearPlaybackAsync();

                    _isListening = false;
                }
                else
                {
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
        InvokeAsync(() =>
        {
            if (conversation is null)
            {
                return;
            }

            _ = conversation;
        });

    Task IRealtimeConversationHandler.OnAudioReceivedAsync(byte[] audioBytes) =>
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

    Task IRealtimeConversationHandler.OnTranscriptReadyAsync(string transcript) => InvokeAsync(() =>
    {
        _transcript.Add(transcript);

        StateHasChanged();
    });

    Task IRealtimeConversationHandler.OnConversationStatusAsync(RealtimeStatus status) => InvokeAsync(() =>
    {
        _status = status;

        StateHasChanged();
    });
}

