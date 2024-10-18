namespace Search.OpenAI.RagAudio.Pages;

public sealed partial class Home
{
    private bool _isRecording = false;

    [Inject]
    public required AudioPlayerService AudioPlayerService { get; set; }

    [Inject]
    public required AudioRecorderService AudioRecorderService { get; set; }
}
