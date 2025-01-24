namespace Search.OpenAI.RagAudio.Pages;

public sealed partial class Home(
    WebSocketService webSocketService,
    MicrophoneSignal signal,
    ILogger<Home> logger) : IRealtimeWebSocketHandler, IDisposable
{
    private bool _isRecording = false;
    private string _message = "Start a conversation?";
    private string _control = "";
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

    private Task OnToggleListeningAsync() => InvokeAsync(async () =>
    {
        if (_microphone is null || _speaker is null)
        {
            return;
        }

        try
        {
            if (_isRecording)
            {
                await _cancellationTokenSource.CancelAsync();

                await _speaker.ClearPlaybackAsync();

                await InputAudioBufferClearAsync();

                _isRecording = false;
            }
            else
            {
                await _cancellationTokenSource.CancelAsync();
                _cancellationTokenSource = new();

                await _microphone.StartAsync();

                _ = Task.Run(StartSessionAsync);

                _isRecording = true;
            }
        }
        finally
        {
            StateHasChanged();
        }
    });

    private async Task StartSessionAsync()
    {
        if (_speaker is null)
        {
            return;
        }

        await webSocketService.ConnectAsync(this, _cancellationTokenSource.Token);
    }

    Task InputAudioBufferClearAsync() =>
        InvokeAsync(() => webSocketService.SendJsonMessageAsync(new ClientReceivableClearBufferMessage()));

    void IRealtimeWebSocketHandler.OnWebSocketOpen()
    {
        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation(
                "WebSocket connection opened.");
        }
    }

    void IRealtimeWebSocketHandler.OnWebSocketClose()
    {
        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation(
                "WebSocket connection closed.");
        }
    }

    void IRealtimeWebSocketHandler.OnWebSocketError(Exception exception)
    {
        if (logger.IsEnabled(LogLevel.Warning))
        {
            logger.LogWarning(
                exception, "WebSocket connection error: {Error}", exception.Message);
        }
    }

    Task IRealtimeWebSocketHandler.OnWebSocketMessageAsync(ClientSendableMessageBase? message) =>
        InvokeAsync(async () =>
    {
        if (message is null)
        {
            return;
        }

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation(
                "Received WebSocket message: {Message}", message);
        }

        if (message is ClientSendableControlMessage control)
        {
            _control = control.Action;
        }

        if (message is ClientSendableTextDeltaMessage delta)
        {
        }

        if (message is ClientSendableSpeechStartedMessage started)
        {
            await (_speaker?.ClearPlaybackAsync() ?? Task.CompletedTask);

            _message = started.Message;
        }

        if (message is ClientSendableConnectedMessage greeting)
        {
            _message = greeting.Greeting;
        }

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

    Task IRealtimeWebSocketHandler.OnWebSocketAudioAsync(byte[] audioBytes) =>
        InvokeAsync(async () =>
    {
        if (_speaker is not null)
        {
            logger.LogInformation("Enqueuing audio data, received {Count:0,0} bytes.", audioBytes.Length);

            var enqueued = await _speaker.EnqueueAsync(audioBytes);
            if (enqueued is false)
            {
                logger.LogWarning("Unable to enqueue audio...");

                await OnToggleListeningAsync();
            }

            StateHasChanged();
        }
    });

    void IDisposable.Dispose() => _cancellationTokenSource?.Dispose();

    async Task<PipeReader> IRealtimeWebSocketHandler.GetAudioReaderAsync(CancellationToken cancellationToken)
    {
        await signal.WaitForMicrophoneAvailabilityAsync(cancellationToken);

        return _microphoneInput!;
    }
}
