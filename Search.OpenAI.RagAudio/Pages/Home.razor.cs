namespace Search.OpenAI.RagAudio.Pages;

public sealed partial class Home(
    AudioPlayerService audioPlayerService,
    AudioRecorderService audioRecorderService,
    WebSocketService webSocketService,
    ILogger<Home> logger) : IDisposable
{
    private bool _isRecording = false;
    private string _message = "Start a conversation?";
    private CancellationTokenSource _cancellationTokenSource = new();
    private GroundingFile? _selectedFile;
    private GroundingFile[] _groundingFiles = [];

    protected override void OnAfterRender(bool firstRender)
    {
        if (firstRender)
        {
            audioRecorderService.OnAudioRecordedAsync += AddUserAudioAsync;
        }
    }

    private async Task OnToggleListeningAsync()
    {
        if (_isRecording)
        {
            await _cancellationTokenSource.CancelAsync();

            await audioRecorderService.StopAsync();
            await audioPlayerService.StopAsync();

            await InputAudioBufferClearAsync();

            _isRecording = false;
        }
        else
        {
            await _cancellationTokenSource.CancelAsync();
            _cancellationTokenSource = new();

            _ = Task.Run(StartSessionAsync);

            await audioRecorderService.StartAsync();
            await audioPlayerService.ResetAsync();

            _isRecording = true;
        }
    }

    private async Task StartSessionAsync()
    {
        await webSocketService.ConnectAsync(
            OnWebSocketOpen,
            OnWebSocketClose,
            OnWebSocketError,
            OnWebSocketMessageAsync,
            _cancellationTokenSource.Token);
    }

    private Task AddUserAudioAsync(byte[] audioBytes)
    {
        return webSocketService.SendAudioBytesAsync(audioBytes);
    }

    private Task InputAudioBufferClearAsync()
    {
        return webSocketService.SendJsonMessageAsync(
            new ClientReceivableClearBufferMessage(),
            SerializationContext.Default.ClientReceivableClearBufferMessage);
    }

    private void OnWebSocketOpen()
    {
        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation(
                "WebSocket connection opened.");
        }
    }

    private void OnWebSocketClose()
    {
        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation(
                "WebSocket connection closed.");
        }
    }

    private void OnWebSocketError(Exception exception)
    {
        if (logger.IsEnabled(LogLevel.Warning))
        {
            logger.LogWarning(
                exception, "WebSocket connection error: {Error}", exception.Message);
        }
    }

    private async Task OnWebSocketMessageAsync(ClientSendableMessageBase? message)
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

        if (message is ClientSendableTextDeltaMessage delta)
        {
            await audioPlayerService.PlayAsync(delta.Delta);
        }

        if (message is ClientSendableSpeechStartedMessage)
        {
            await audioPlayerService.StopAsync();
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
    }

    public void Dispose()
    {
        audioRecorderService.OnAudioRecordedAsync -= AddUserAudioAsync;

        _cancellationTokenSource?.Dispose();
    }
}
