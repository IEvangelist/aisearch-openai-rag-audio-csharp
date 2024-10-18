namespace Search.OpenAI.RagAudio.Services;

// Based on:
//  https://github.com/Azure-Samples/aisearch-openai-rag-audio/blob/main/app/frontend/src/hooks/useAudioPlayer.tsx

public sealed class AudioPlayerService(
    AudioPlayer player,
    IJSInProcessRuntime jsRuntime)
{
    private const int SAMPLE_RATE = 24_000;

    public async Task ResetAsync()
    {
        await player.StopAsync();
        await player.InitializeAsync(SAMPLE_RATE);
    }

    public async Task PlayAsync(string audio)
    {
        var binary = Convert.FromBase64String(audio);

        var pcmData = new short[binary.Length / 2];
        Buffer.BlockCopy(binary, 0, pcmData, 0, binary.Length);

        // TODO: populate from audio data.
        var uint16Array = await Uint16Array.CreateAsync(jsRuntime);

        await player.PlayAsync(uint16Array);
    }

    public Task StopAsync() => player.StopAsync();
}
