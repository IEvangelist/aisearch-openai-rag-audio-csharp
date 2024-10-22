namespace Search.OpenAI.RagAudio.Pages;

public sealed partial class Home
{
    private bool _isRecording = false;
    private CancellationTokenSource _cancellationTokenSource = new();
    private GroundingFile? _selectedFile;
    private GroundingFile[] _groundingFiles = [];

    [Inject]
    public required ILogger<Home> Logger { get; set; }

    [Inject]
    public required AudioPlayerService AudioPlayerService { get; set; }

    [Inject]
    public required AudioRecorderService AudioRecorderService { get; set; }

    [Inject]
    public required WebSocketService WebSocketService { get; set; }

    protected override void OnAfterRender(bool firstRender)
    {
        if (firstRender)
        {
            AudioRecorderService.OnAudioRecordedAsync += AddUserAudioAsync;
        }
    }

    private async Task OnToggleListeningAsync()
    {
        if (_isRecording)
        {
            await _cancellationTokenSource.CancelAsync();

            await AudioRecorderService.StopAsync();
            await AudioPlayerService.StopAsync();

            await InputAudioBufferClearAsync();

            _isRecording = false;
        }
        else
        {
            _cancellationTokenSource = new();

            _ = Task.Run(StartSessionAsync);

            await AudioRecorderService.StartAsync();
            await AudioPlayerService.ResetAsync();

            _isRecording = true;
        }
    }

    private async Task StartSessionAsync()
    {
        await WebSocketService.ConnectAsync(
            OnWebSocketOpen,
            OnWebSocketClose,
            OnWebSocketError,
            OnWebSocketMessageAsync,
            _cancellationTokenSource.Token);

        // TODO: Handle "whisper-1" model addition when enableInputAudioTranscription.

        await WebSocketService.SendJsonMessageAsync(
            new SessionUpdateCommand(
                Type: "session.update",
                Session: new Session(new TurnDetection("server_vad"))),
            SerializationContext.Default.SessionUpdateCommand);
    }

    private Task AddUserAudioAsync(string base64Audio)
    {
        var command = new InputAudioBufferAppendCommand(
            "input_audio_buffer.append", base64Audio);

        return WebSocketService.SendJsonMessageAsync(
            command, SerializationContext.Default.InputAudioBufferAppendCommand);
    }

    private Task InputAudioBufferClearAsync()
    {
        var command = new InputAudioBufferClearCommand(
            "input_audio_buffer.clear");

        return WebSocketService.SendJsonMessageAsync(
            command, SerializationContext.Default.InputAudioBufferClearCommand);
    }

    private void OnWebSocketOpen()
    {
        if (Logger.IsEnabled(LogLevel.Information))
        {
            Logger.LogInformation(
                "WebSocket connection opened.");
        }
    }

    private void OnWebSocketClose()
    {
        if (Logger.IsEnabled(LogLevel.Information))
        {
            Logger.LogInformation(
                "WebSocket connection closed.");
        }
    }

    private void OnWebSocketError(Exception exception)
    {
        if (Logger.IsEnabled(LogLevel.Warning))
        {
            Logger.LogWarning(
                exception, "WebSocket connection error: {Error}", exception.Message);
        }
    }

    private async Task OnWebSocketMessageAsync(Message? message)
    {
        if (message is null)
        {
            return;
        }

        if (Logger.IsEnabled(LogLevel.Information))
        {
            Logger.LogInformation(
                "Received WebSocket message: {Message}", message);
        }

        if (message is ResponseAudioDelta delta)
        {
            await AudioPlayerService.PlayAsync(delta.Delta);
        }

        if (message is { Type: "input_audio_buffer.speech_started" })
        {
            await AudioPlayerService.StopAsync();
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
}
