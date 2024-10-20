const MIN_UINT8 = 0;
const MAX_UINT8 = 255;

class PCMAudioProcessor extends AudioWorkletProcessor {
    constructor() {
        super();
    }

    process(inputs, outputs, parameters) {
        const input = inputs[0];
        if (input.length > 0) {
            const float32Buffer = input[0];
            const uint8Array = this.float32ToUInt8(float32Buffer);
            this.port.postMessage(uint8Array);
        }
        return true;
    }

    float32ToUInt8(float32Array) {
        const uint8Array = new Int16Array(float32Array.length);
        for (let i = 0; i < float32Array.length; i++) {
            // Convert from number in domain [-1, +1] to domain [0, 255]
            let val = Math.floor((float32Array[i] + 1) / 2 * MAX_UINT8);
            // Ensure that we limit to between 0 and 255 in case number had been gained above 100% volume.
            val = Math.max(MIN_UINT8, Math.min(MAX_UINT8, val));
            uint8Array[i] = val;
        }
        return uint8Array;
    }
}

registerProcessor("audio-processor-worklet", PCMAudioProcessor);