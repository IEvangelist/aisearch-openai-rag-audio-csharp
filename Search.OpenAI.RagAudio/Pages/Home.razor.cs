namespace Search.OpenAI.RagAudio.Pages;

public sealed partial class Home(
    AudioPlayerService audioPlayerService,
    AudioRecorderService audioRecorderService,
    WebSocketService webSocketService,
    ILogger<Home> logger) : IDisposable
{
    private bool _isRecording = false;
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

        // TODO: Handle "whisper-1" model addition when enableInputAudioTranscription.

        await webSocketService.SendJsonMessageAsync(
            new SessionUpdateCommand(
                Type: "session.update",
                Session: new Session(new TurnDetection("server_vad"))),
            SerializationContext.Default.SessionUpdateCommand);
    }

    private Task AddUserAudioAsync(string base64Audio)
    {
        var command = new InputAudioBufferAppendCommand(
            "input_audio_buffer.append", base64Audio);

        return webSocketService.SendJsonMessageAsync(
            command, SerializationContext.Default.InputAudioBufferAppendCommand);
    }

    private Task InputAudioBufferClearAsync()
    {
        var command = new InputAudioBufferClearCommand(
            "input_audio_buffer.clear");

        return webSocketService.SendJsonMessageAsync(
            command, SerializationContext.Default.InputAudioBufferClearCommand);
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

    private async Task OnWebSocketMessageAsync(Message? message)
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

        if (message is ResponseAudioDelta delta)
        {
            await audioPlayerService.PlayAsync(delta.Delta);
        }

        if (message is { Type: "input_audio_buffer.speech_started" })
        {
            await audioPlayerService.StopAsync();
        }

        if (message is ExtensionMiddleTierToolResponse toolResponse)
        {
            var result = JsonSerializer.Deserialize(
                toolResponse.ToolResult, SerializationContext.Default.ToolResult);

            if (result is null)
            {
                return;
            }

            _groundingFiles = [
                ..result.Sources.Select(
                    static source => new GroundingFile(
                        Id: source.ChunkId,
                        Name: source.Title,
                        Content: source.Chunk))
            ];
        }
    }

    public void Dispose()
    {
        audioRecorderService.OnAudioRecordedAsync -= AddUserAudioAsync;

        _cancellationTokenSource?.Dispose();
    }
}
