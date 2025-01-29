export async function start(deviceId) {
    // Ensure the deviceId is used for the audio constraints
    const stream = await navigator.mediaDevices.getUserMedia({
        video: false,
        audio: { 
            sampleRate: 24000,
            deviceId: deviceId ? { exact: deviceId } : undefined // Use deviceId if provided
        }
    });

    // Create an AudioContext with the desired sample rate
    const audioCtx = new AudioContext({ sampleRate: 24000 });

    // Use the media stream as the input source
    const inputSource = audioCtx.createMediaStreamSource(stream);

    const pendingSources = [];
    let currentPlaybackEndTime = 0;

    return {
        enqueue(data) {
            const bufferSource = toAudioBufferSource(audioCtx, data);
            pendingSources.push(bufferSource);
            bufferSource.onended = () => pendingSources.splice(pendingSources.indexOf(bufferSource), 1);
            currentPlaybackEndTime = Math.max(currentPlaybackEndTime, audioCtx.currentTime + 0.5);
            bufferSource.start(currentPlaybackEndTime);
            currentPlaybackEndTime += bufferSource.buffer.duration;
        },

        clear() {
            pendingSources.forEach(source => source.stop());
            pendingSources.length = 0;
            currentPlaybackEndTime = 0;
        }
    };
}

function toAudioBufferSource(audioCtx, data) {
    // Convert Int16Array to Float32Array
    const int16Samples = new Int16Array(data.buffer.slice(data.byteOffset, data.byteOffset + data.byteLength));
    const numSamples = int16Samples.length;
    const float32Samples = new Float32Array(numSamples);
    for (let i = 0; i < numSamples; i++) {
        float32Samples[i] = int16Samples[i] / 0x7FFF;
    }

    const audioBuffer = new AudioBuffer({
        length: numSamples,
        sampleRate: audioCtx.sampleRate,
    });

    audioBuffer.copyToChannel(float32Samples, 0, 0);

    const bufferSource = audioCtx.createBufferSource();
    bufferSource.buffer = audioBuffer;
    bufferSource.connect(audioCtx.destination);
    
    return bufferSource;
}