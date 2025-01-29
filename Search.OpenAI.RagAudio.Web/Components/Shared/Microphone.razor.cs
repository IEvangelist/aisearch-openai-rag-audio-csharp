namespace Search.OpenAI.RagAudio.Web.Components.Shared;

public sealed partial class Microphone(ILocalStorageService localStorage, IJSRuntime js, ILogger<Microphone> logger)
{
    private readonly SemaphoreSlim _writeAudioSemaphore = new(1);
    private readonly Pipe _microphonePipe = new();

    private IJSObjectReference? _module;
    private IJSObjectReference? _microphone;

    [Parameter, EditorRequired]
    public EventCallback<PipeReader> OnMicrophoneAvailable { get; set; }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
        {
            return;
        }

        if (_module is null)
        {
            _module = await js.InvokeAsync<IJSObjectReference>("import", "../js/microphone.js");

            logger.LogInformation("Initialized microphone module.");
        }
    }

    public async Task StartAsync()
    {
        if (_module is null)
        {
            logger.LogWarning("Microphone module is null");

            return;
        }

        if (_microphone is null)
        {
            var deviceId = await localStorage.GetItemAsync<string>("microphone");

            _microphone = await _module.InvokeAsync<IJSObjectReference>("start", DotNetObjectReference.Create(this), deviceId);

            logger.LogInformation("Initialized microphone object.");
        }
    }

    public async ValueTask StopAsync()
    {
        await (_module?.InvokeVoidAsync("stop") ?? ValueTask.CompletedTask);
    }

    [JSInvokable]
    public Task OnMicConnectedAsync()
    {
        logger.LogInformation("Microphone is now available.");

        return OnMicrophoneAvailable.InvokeAsync(_microphonePipe.Reader);
    }

    [JSInvokable]
    public Task ReceiveAudioDataAsync(byte[] data) => InvokeAsync(async () =>
    {
        if (await _writeAudioSemaphore.WaitAsync(0))
        {
            try
            {
                await _microphonePipe.Writer.WriteAsync(data);
            }
            finally
            {
                _writeAudioSemaphore.Release();
            }
        }
    });
}
