namespace Search.OpenAI.RagAudio.Services;

public sealed class AudioPlayer(IJSRuntime jsRuntime) : IAsyncDisposable
{
    private AudioWorkletNode? _playbackNode;
    private AudioContext? _context;

    public async Task InitializeAsync(int sampleRate)
    {
        _context = await AudioContext.CreateAsync(jsRuntime, new AudioContextOptions
        {
            SampleRate = sampleRate,
        });

        await using var audioWorklet = await _context.GetAudioWorkletAsync();
        await audioWorklet.AddModuleAsync("js/audio-playback-worklet.js");

        _playbackNode = await AudioWorkletNode.CreateAsync(
            jsRuntime, _context, "audio-playback-worklet");

        var destination = await _context.GetDestinationAsync();
        await _playbackNode.ConnectAsync(destination);
    }

    public async Task PlayAsync(Uint16Array buffer)
    {
        if (_playbackNode is not null)
        {
            var messagePort = await _playbackNode.GetPortAsync();

            await messagePort.PostMessageAsync(buffer);
        }
    }

    public async Task StopAsync()
    {
        if (_playbackNode is not null)
        {
            var messagePort = await _playbackNode.GetPortAsync();

            await messagePort.PostMessageAsync(null!);
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_context is not null)
        {
            await _context.CloseAsync();

            _context = null;
        }
    }
}
