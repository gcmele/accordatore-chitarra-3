/**
 * AudioCapture - Web Audio API implementation for microphone capture
 * Compatible with iOS Safari, Chrome, Firefox, Edge
 * Uses ScriptProcessorNode as fallback for older browsers
 */
class AudioCapture {
    constructor(sampleRate = 44100, bufferSize = 4096) {
        this.sampleRate = sampleRate;
        this.bufferSize = bufferSize;
        this.audioContext = null;
        this.mediaStream = null;
        this.sourceNode = null;
        this.processorNode = null;
        this.isRecording = false;
        this.onAudioData = null;
    }

    /**
     * Initializes audio context and requests microphone permission
     * @returns {Promise<void>}
     */
    async initialize() {
        try {
            // Request microphone permission
            this.mediaStream = await navigator.mediaDevices.getUserMedia({
                audio: {
                    echoCancellation: false,
                    noiseSuppression: false,
                    autoGainControl: false,
                    sampleRate: this.sampleRate
                }
            });

            // Create AudioContext
            // Safari requires webkitAudioContext
            const AudioContextClass = window.AudioContext || window.webkitAudioContext;
            this.audioContext = new AudioContextClass({ sampleRate: this.sampleRate });

            // Create source node from media stream
            this.sourceNode = this.audioContext.createMediaStreamSource(this.mediaStream);

            // Use ScriptProcessorNode for compatibility with iOS Safari
            // AudioWorklet is not supported on older iOS versions
            this.processorNode = this.audioContext.createScriptProcessor(
                this.bufferSize,
                1, // mono input
                1  // mono output
            );

            // Set up audio processing
            this.processorNode.onaudioprocess = (event) => {
                if (!this.isRecording) return;

                const inputBuffer = event.inputBuffer;
                const samples = inputBuffer.getChannelData(0); // Get mono channel

                // Call the callback with audio data
                if (this.onAudioData) {
                    this.onAudioData(samples);
                }
            };

            return {
                success: true,
                message: 'Microfono inizializzato correttamente'
            };
        } catch (error) {
            console.error('Error initializing audio:', error);
            return {
                success: false,
                message: this.getErrorMessage(error)
            };
        }
    }

    /**
     * Starts audio capture
     */
    start() {
        if (!this.audioContext || !this.sourceNode || !this.processorNode) {
            throw new Error('Audio non inizializzato. Chiamare initialize() prima.');
        }

        // Resume audio context if suspended (required for iOS Safari)
        if (this.audioContext.state === 'suspended') {
            this.audioContext.resume();
        }

        // Connect nodes
        this.sourceNode.connect(this.processorNode);
        this.processorNode.connect(this.audioContext.destination);

        this.isRecording = true;
    }

    /**
     * Stops audio capture
     */
    stop() {
        if (!this.isRecording) return;

        this.isRecording = false;

        // Disconnect nodes
        if (this.sourceNode && this.processorNode) {
            try {
                this.sourceNode.disconnect(this.processorNode);
                this.processorNode.disconnect(this.audioContext.destination);
            } catch (error) {
                console.error('Error disconnecting nodes:', error);
            }
        }
    }

    /**
     * Releases all audio resources
     */
    dispose() {
        this.stop();

        if (this.processorNode) {
            this.processorNode.onaudioprocess = null;
            this.processorNode = null;
        }

        if (this.sourceNode) {
            this.sourceNode = null;
        }

        if (this.mediaStream) {
            this.mediaStream.getTracks().forEach(track => track.stop());
            this.mediaStream = null;
        }

        if (this.audioContext) {
            this.audioContext.close();
            this.audioContext = null;
        }

        this.isRecording = false;
    }

    /**
     * Gets user-friendly error message
     * @param {Error} error
     * @returns {string}
     */
    getErrorMessage(error) {
        if (error.name === 'NotAllowedError' || error.name === 'PermissionDeniedError') {
            return 'Accesso al microfono negato. Concedi i permessi per usare l\'accordatore.';
        } else if (error.name === 'NotFoundError' || error.name === 'DevicesNotFoundError') {
            return 'Nessun microfono trovato. Collega un microfono e riprova.';
        } else if (error.name === 'NotReadableError' || error.name === 'TrackStartError') {
            return 'Impossibile accedere al microfono. Potrebbe essere in uso da un\'altra app.';
        } else if (error.name === 'OverconstrainedError' || error.name === 'ConstraintNotSatisfiedError') {
            return 'Configurazione audio non supportata dal tuo dispositivo.';
        } else if (error.name === 'NotSupportedError') {
            return 'Il tuo browser non supporta l\'accesso al microfono.';
        } else if (error.name === 'SecurityError') {
            return 'Errore di sicurezza. Assicurati di usare HTTPS.';
        } else {
            return `Errore: ${error.message || 'Errore sconosciuto'}`;
        }
    }

    /**
     * Checks if Web Audio API is supported
     * @returns {boolean}
     */
    static isSupported() {
        return !!(navigator.mediaDevices && 
                  navigator.mediaDevices.getUserMedia && 
                  (window.AudioContext || window.webkitAudioContext));
    }
}
