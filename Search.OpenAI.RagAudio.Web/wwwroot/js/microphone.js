let audioCtx;
let micStreamSource;
let workletNode;

export async function start(component, deviceId) {
    console.log(`Starting microphone with device: ${deviceId}`);

    try {
        // Prompt user for access.
        await navigator.mediaDevices.getUserMedia({ video: false, audio: true });

        const devices = await navigator.mediaDevices.enumerateDevices();

        // Filter the list to find audio input devices (microphones)
        const microphones = devices.filter(device => device.kind === 'audioinput');

        microphones.forEach(mic => {
            console.log('Microphone:', mic);
        });

        // Prioritize the passed deviceId, fallback to the default microphone
        const selectedDevice = deviceId
            ? microphones.find(device => device.deviceId === deviceId)
            : microphones.find(device => device.deviceId === 'default');

        if (!selectedDevice) {
            throw new Error(`Microphone with deviceId '${deviceId || 'default'}' not found.`);
        }

        console.log(`Using '${selectedDevice.label}' microphone.`);

        // Obtain the microphone stream using the selected device
        const microphoneStream = await navigator.mediaDevices.getUserMedia({
            video: false,
            audio: {
                deviceId: selectedDevice.deviceId,
                sampleRate: 24000
            }
        });

        processMicrophoneData(microphoneStream, component);

        return microphoneStream;
    } catch (ex) {
        throw new Error(`Unable to access microphone: ${ex.toString()}`);
    }
}

async function processMicrophoneData(microphoneStream, component) {
    audioCtx = new AudioContext({ sampleRate: 24000 });
    micStreamSource = audioCtx.createMediaStreamSource(microphoneStream);

    const workletBlobUrl = URL.createObjectURL(new Blob([`
        registerProcessor('sendAudioDataWorklet', class param extends AudioWorkletProcessor {
            constructor() { super(); }
            process(input, output, parameters) {
              this.port.postMessage(input[0]);
              return true;
            }
          });
        `],
        { type: 'application/javascript' }));
    await audioCtx.audioWorklet.addModule(workletBlobUrl);
    workletNode = new AudioWorkletNode(audioCtx, 'sendAudioDataWorklet', {});
    micStreamSource.connect(workletNode);
    workletNode.port.onmessage = async (e) => {
        // Convert float32 to int16
        const float32Samples = e.data[0];
        const numSamples = float32Samples.length;
        const int16Samples = new Int16Array(numSamples);
        for (let i = 0; i < numSamples; i++) {
            int16Samples[i] = float32Samples[i] * 0x7FFF;
        }

        component.invokeMethodAsync('ReceiveAudioDataAsync', new Uint8Array(int16Samples.buffer));
    }

    await component.invokeMethodAsync('OnMicConnectedAsync');
}

export function stop() {
    console.log('Stopping microphone');

    if (workletNode) {
        workletNode.port.close();
        workletNode.disconnect();
        workletNode = null;
    }

    if (micStreamSource) {
        micStreamSource.disconnect();
        micStreamSource = null;
    }

    if (audioCtx && audioCtx.state !== "closed") {
        audioCtx.close();
        audioCtx = null;
    }

    console.log('Microphone listening stopped.');
}