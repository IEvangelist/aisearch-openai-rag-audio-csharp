export async function setAudioOutputDevice(element, deviceId) {
    if (deviceId && element && element.setSinkId) {

        const allowed = await navigator.permissions.query({ name: 'microphone' });
        if (!allowed || allowed.state !== 'granted') {
            const stream = await navigator.mediaDevices.getUserMedia({ video: false, audio: { sampleRate: 24000 } });
            stream.getTracks().forEach(track => track.stop());
        };

        const devices = await navigator.mediaDevices.enumerateDevices();
        if (devices.some(d => d.deviceId === deviceId)) {
            await element.setSinkId(deviceId);
        }
    }
}

export function getClientSpeakers() {
    return getDevices('audiooutput');
}

export function getClientMicrophones() {
    return getDevices('audioinput');
}

const getDevices = async (kind) => {
    if (!navigator.mediaDevices || !navigator.mediaDevices.enumerateDevices) {
        console.log('navigator.mediaDevices.enumerateDevices is not supported.');
    }

    let devices = await navigator.mediaDevices.enumerateDevices();
    if (devices &&
        (devices.length === 0 || devices.every(d => d.label === ""))) {
        await navigator.mediaDevices.getUserMedia({
            audio: true
        });
    }

    devices = await navigator.mediaDevices.enumerateDevices();
    const filteredDevices = devices.filter(device => device.kind === kind);

    return JSON.stringify(filteredDevices.map(s => ({
        Label: s.label,
        DeviceId: s.deviceId,
        Kind: s.kind
    })));
}