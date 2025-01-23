namespace Search.OpenAI.RagAudio.Services;

// Based on:
//  https://github.com/Azure-Samples/aisearch-openai-rag-audio/blob/main/app/frontend/src/hooks/useAudioRecorder.tsx

public sealed class AudioRecorderService(
    AudioRecorder recorder,
    IMediaDevicesService mediaDevicesService,
    IJSInProcessRuntime jsRuntime,
    ILogger<AudioRecorder> logger)
{
    private const int BUFFER_SIZE = 4_800;

    private MediaDevices? _mediaDevices;
    private Uint8Array? _buffer;

    public event Func<byte[], Task>? OnAudioRecordedAsync;

    public async Task StartAsync()
    {
        logger.LogInformation("Starting audio recording.");

        _mediaDevices ??= await mediaDevicesService.GetMediaDevicesAsync();

        var stream = await _mediaDevices.GetUserMediaAsync(new()
        {
            Audio = new MediaTrackConstraints()
            {
                SampleRate = 48_000,
                SampleSize = 16,
                ChannelCount = 1,
            },
        });

        recorder.OnDataAvailable += OnHandleAudioDataAsync;

        await recorder.StartAsync(stream);
    }

    public Task StopAsync()
    {
        logger.LogInformation("Stopping audio recording.");

        recorder.OnDataAvailable -= OnHandleAudioDataAsync;

        return recorder.StopAsync();
    }

    private async Task OnHandleAudioDataAsync(Uint8Array array)
    {
        //var buffer = await array.GetAsArrayAsync();

        //if (OnAudioRecordedAsync is { } handler)
        //{
        //    await handler.Invoke(buffer);
        //}

        var length = await AppendToBufferAsync(array);

        if (length >= BUFFER_SIZE)
        {
            logger.LogInformation("Sending buffered audio.");

            var buffer = await _buffer.GetAsArrayAsync();

            var toSend = buffer.Take(BUFFER_SIZE).ToArray();
            buffer = buffer.Skip(BUFFER_SIZE).ToArray();

            var regularArray = Encoding.UTF8.GetString(toSend);
            var bytes = Encoding.UTF8.GetBytes(regularArray);

            if (OnAudioRecordedAsync is { } handler)
            {
                await handler.Invoke(bytes);
            }
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
