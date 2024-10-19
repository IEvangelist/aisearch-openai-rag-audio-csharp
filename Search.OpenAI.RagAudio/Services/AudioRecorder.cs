namespace Search.OpenAI.RagAudio.Services;

public sealed class AudioRecorder(IJSRuntime jsRuntime) : IAsyncDisposable
{
    private AudioContext? _context;
    private MediaStream? _mediaStream;
    private MediaStreamAudioSourceNode? _mediaStreamSource;
    private AudioWorkletNode? _workletNode;

    public event Func<Int16Array, Task>? OnDataAvailable;

    public async Task StartAsync(MediaStream stream)
    {
        try
        {
            if (_context is not null)
            {
                await _context.CloseAsync();
            }

            _context = await AudioContext.CreateAsync(jsRuntime, new AudioContextOptions
            {
                SampleRate = 24_000,
            });

            await using var audioWorklet = await _context.GetAudioWorkletAsync();
            await audioWorklet.AddModuleAsync("js/audio-processor-worklet.js");

            _mediaStream = stream;
            _mediaStreamSource = await _context.CreateMediaStreamSourceAsync(_mediaStream);

            _workletNode = await AudioWorkletNode.CreateAsync(
                jsRuntime, _context, "audio-processor-worklet");

            var port = await _workletNode.GetPortAsync();

            // Start message port to ensure that messages can be received.
            await port.StartAsync();

            var messageListener = await EventListener<MessageEvent>.CreateAsync(_context.JSRuntime, async e =>
            {
                if (OnDataAvailable is not null)
                {
                    await using var buffer = await Int16Array.CreateAsync(_context.JSRuntime, await e.GetDataAsync());
                    await OnDataAvailable.Invoke(buffer);
                }
            });

            await port!.AddOnMessageEventListenerAsync(messageListener);

            await _mediaStreamSource.ConnectAsync(_workletNode);

            var destination = await _context.GetDestinationAsync();
            await _workletNode.ConnectAsync(destination);
        }
        catch (Exception ex)
        {
            if (Debugger.IsAttached)
            {
                Debugger.Break();
                _ = ex;
            }

            await StopAsync();
        }
    }

    public async Task StopAsync()
    {
        if (_mediaStream is not null)
        {
            var tracks = await _mediaStream.GetTracksAsync();

            foreach (var track in tracks)
            {
                await track.StopAsync();
            }

            _mediaStream = null;
        }

        if (_context is not null)
        {
            await _context.CloseAsync();

            _context = null;
        }

        _mediaStreamSource = null;
        _workletNode = null;
    }

    public async ValueTask DisposeAsync() => await StopAsync();
}
