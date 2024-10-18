namespace Search.OpenAI.RagAudio.Services;

// Based on:
//  https://github.com/Azure-Samples/aisearch-openai-rag-audio/blob/main/app/frontend/src/hooks/useAudioRecorder.tsx

public sealed class AudioRecorderService(
    AudioRecorder recorder,
    IMediaDevicesService mediaDevicesService,
    IJSInProcessRuntime jsRuntime)
{
    private const int BUFFER_SIZE = 4_800;

    private MediaDevices? _mediaDevices;
    private Uint8Array? _buffer;

    public event Action<string>? OnAudioRecorded;

    public async Task StartAsync()
    {
        _mediaDevices ??= await mediaDevicesService.GetMediaDevicesAsync();

        var stream = await _mediaDevices.GetUserMediaAsync(new()
        {
            Audio = true,
        });

        recorder.OnDataAvailable += OnHandleAudioDataAsync;

        await recorder.StartAsync(stream);
    }

    public Task StopAsync()
    {
        recorder.OnDataAvailable -= OnHandleAudioDataAsync;

        return recorder.StopAsync();
    }

    private async Task OnHandleAudioDataAsync(Int16Array data)
    {
        var array = await Uint8Array.CreateAsync(jsRuntime, data);
        var length = await AppendToBufferAsync(array);

        if (length >= BUFFER_SIZE)
        {
            var buffer = await _buffer.GetAsArrayAsync();

            var toSend = buffer.Take(BUFFER_SIZE).ToArray();
            buffer = buffer.Skip(BUFFER_SIZE).ToArray();

            var regularArray = Encoding.UTF8.GetString(toSend);
            var base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(regularArray));

            OnAudioRecorded?.Invoke(base64);
        }
    }

    [MemberNotNull(nameof(_buffer))]
    private async Task<long> AppendToBufferAsync(Uint8Array newData)
    {
#pragma warning disable CS8774 // Member must have a non-null value when exiting.
        _buffer ??= await Uint8Array.CreateAsync(jsRuntime);
#pragma warning restore CS8774 // Member must have a non-null value when exiting.

        var bufferLength = await _buffer.GetLengthAsync();
        var newDataLength = await newData.GetLengthAsync();

        // var buffer = await Uint8Array.CreateAsync(jsRuntime, bufferLength + newDataLength);  

        // await buffer.SetAsync(_buffer);  
        // await buffer.SetAsync(newData, bufferLength);  

        // _buffer = buffer;  

        return bufferLength + newDataLength;
    }
}
