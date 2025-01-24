namespace Search.OpenAI.RagAudio.Shared;

public sealed partial class Microphone(IJSRuntime js, ILogger<Microphone> logger) : IAsyncDisposable
{
    private readonly SemaphoreSlim _writeAudioSemaphore = new(1);
    private readonly Pipe _microphonePipe = new();

    private IJSObjectReference? _module;
    private IJSObjectReference? _microphone;

    [Parameter, EditorRequired]
    public EventCallback<PipeReader> OnMicrophoneAvailable { get; set; }

    protected override async Task OnInitializedAsync()
    {
        _module = await js.InvokeAsync<IJSObjectReference>("import", "../js/microphone.js");

        logger.LogInformation("Initialized microphone module.");
    }

    public async Task StartAsync()
    {
        if (_module is null)
        {
            logger.LogWarning("Microphone module is null");

            return;
        }

        _microphone ??= await _module.InvokeAsync<IJSObjectReference>("start", DotNetObjectReference.Create(this));

        logger.LogInformation("Initialized microphone object.");
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

    async ValueTask IAsyncDisposable.DisposeAsync()
    {
        await _microphonePipe.Writer.CompleteAsync();

        try
        {
            await (_microphone?.DisposeAsync() ?? ValueTask.CompletedTask);
            await (_module?.DisposeAsync() ?? ValueTask.CompletedTask);
        }
        catch (JSDisconnectedException)
        {
            // Not an error
        }
    }
}
