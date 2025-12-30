/**
 * PitchDetector - JavaScript implementation of autocorrelation-based pitch detection
 * Ported from the C# PitchDetector.cs
 * Optimized for guitar tuning (70-1500 Hz range)
 */
class PitchDetector {
    constructor(sampleRate = 44100, minFrequency = 70, maxFrequency = 1500, threshold = 0.1) {
        this.sampleRate = sampleRate;
        this.minFrequency = minFrequency;
        this.maxFrequency = maxFrequency;
        this.threshold = threshold;
    }

    /**
     * Detects the pitch (fundamental frequency) from audio samples using autocorrelation
     * @param {Float32Array} samples - Audio samples (normalized -1.0 to 1.0)
     * @returns {Object} PitchDetectionResult with frequency, confidence, isValid, rms
     */
    detectPitch(samples) {
        if (!samples || samples.length === 0) {
            return {
                frequency: 0,
                confidence: 0,
                isValid: false,
                rms: 0
            };
        }

        // Calculate RMS to check if signal is strong enough
        const rms = this.calculateRMS(samples);
        if (rms < this.threshold) {
            return {
                frequency: 0,
                confidence: 0,
                isValid: false,
                rms: rms
            };
        }

        // Apply autocorrelation
        const frequency = this.autocorrelationPitchDetection(samples);

        // Validate frequency range
        if (frequency < this.minFrequency || frequency > this.maxFrequency) {
            return {
                frequency: frequency,
                confidence: 0,
                isValid: false,
                rms: rms
            };
        }

        // Calculate confidence based on autocorrelation clarity
        const confidence = this.calculateConfidence(samples, frequency);

        return {
            frequency: frequency,
            confidence: confidence,
            isValid: confidence > 0.5, // Confidence threshold
            rms: rms
        };
    }

    /**
     * Performs autocorrelation-based pitch detection
     * @param {Float32Array} samples
     * @returns {number} Detected frequency in Hz
     */
    autocorrelationPitchDetection(samples) {
        const minLag = Math.floor(this.sampleRate / this.maxFrequency);
        const maxLag = Math.floor(this.sampleRate / this.minFrequency);
        const bufferSize = Math.min(samples.length, maxLag * 2);

        // Calculate autocorrelation
        const autocorrelation = new Float32Array(maxLag + 1);

        for (let lag = minLag; lag <= maxLag; lag++) {
            let sum = 0;
            for (let i = 0; i < bufferSize - lag; i++) {
                sum += samples[i] * samples[i + lag];
            }
            autocorrelation[lag] = sum;
        }

        // Find the first peak (highest autocorrelation value after minimum lag)
        const peakLag = this.findFirstPeak(autocorrelation, minLag, maxLag);

        if (peakLag === 0) {
            return 0;
        }

        // Parabolic interpolation for better precision
        const refinedLag = this.parabolicInterpolation(autocorrelation, peakLag);

        // Convert lag to frequency
        const frequency = this.sampleRate / refinedLag;

        return frequency;
    }

    /**
     * Finds the first significant peak in autocorrelation
     * @param {Float32Array} autocorrelation
     * @param {number} minLag
     * @param {number} maxLag
     * @returns {number} Index of the peak
     */
    findFirstPeak(autocorrelation, minLag, maxLag) {
        let maxValue = -Infinity;
        let maxIndex = 0;

        for (let i = minLag; i <= maxLag; i++) {
            if (autocorrelation[i] > maxValue) {
                maxValue = autocorrelation[i];
                maxIndex = i;
            }
        }

        return maxIndex;
    }

    /**
     * Parabolic interpolation for sub-sample precision
     * @param {Float32Array} data
     * @param {number} index
     * @returns {number} Refined index with sub-sample precision
     */
    parabolicInterpolation(data, index) {
        if (index <= 0 || index >= data.length - 1) {
            return index;
        }

        const alpha = data[index - 1];
        const beta = data[index];
        const gamma = data[index + 1];

        const offset = 0.5 * (alpha - gamma) / (alpha - 2 * beta + gamma);

        return index + offset;
    }

    /**
     * Calculates confidence of pitch detection based on autocorrelation clarity
     * @param {Float32Array} samples
     * @param {number} frequency
     * @returns {number} Confidence value (0.0 to 1.0)
     */
    calculateConfidence(samples, frequency) {
        if (frequency <= 0) {
            return 0;
        }

        const period = Math.floor(this.sampleRate / frequency);
        if (period <= 0 || period >= samples.length / 2) {
            return 0;
        }

        // Calculate correlation at detected period
        let sum = 0;
        let sumSquares = 0;
        const count = Math.min(samples.length - period, period * 2);

        for (let i = 0; i < count; i++) {
            sum += samples[i] * samples[i + period];
            sumSquares += samples[i] * samples[i];
        }

        if (sumSquares === 0) {
            return 0;
        }

        // Normalized correlation coefficient
        const correlation = sum / sumSquares;

        // Clamp to 0-1 range
        return Math.max(0, Math.min(1, correlation));
    }

    /**
     * Calculates Root Mean Square (RMS) of audio samples
     * @param {Float32Array} samples
     * @returns {number} RMS value
     */
    calculateRMS(samples) {
        if (!samples || samples.length === 0) {
            return 0;
        }

        let sum = 0;
        for (let i = 0; i < samples.length; i++) {
            sum += samples[i] * samples[i];
        }

        return Math.sqrt(sum / samples.length);
    }

    /**
     * Identifies the musical note from a frequency
     * @param {number} frequency - Frequency in Hz
     * @returns {Object} MusicalNote object with note name, octave, cents offset, etc.
     */
    static getNoteFromFrequency(frequency) {
        if (frequency <= 0) {
            return {
                noteName: '',
                noteNameAnglo: '',
                octave: 0,
                frequency: 0,
                actualFrequency: 0,
                centsOffset: 0,
                isValid: false
            };
        }

        // A4 = 440 Hz as reference
        const A4 = 440.0;
        const A4_MIDI = 69;

        // Calculate MIDI note number
        const halfStepsFromA4 = 12.0 * Math.log2(frequency / A4);
        const midiNote = Math.round(halfStepsFromA4) + A4_MIDI;

        // Calculate cents offset from perfect pitch
        const perfectFrequency = A4 * Math.pow(2, (midiNote - A4_MIDI) / 12.0);
        const centsOffset = 1200.0 * Math.log2(frequency / perfectFrequency);

        // Get note name
        const noteIndex = midiNote % 12;
        const octave = Math.floor(midiNote / 12) - 1;

        const noteNamesItalian = ['DO', 'DO#', 'RE', 'RE#', 'MI', 'FA', 'FA#', 'SOL', 'SOL#', 'LA', 'LA#', 'SI'];
        const noteNamesAnglo = ['C', 'C#', 'D', 'D#', 'E', 'F', 'F#', 'G', 'G#', 'A', 'A#', 'B'];

        return {
            noteName: noteNamesItalian[noteIndex],
            noteNameAnglo: noteNamesAnglo[noteIndex],
            octave: octave,
            midiNote: midiNote,
            frequency: perfectFrequency,
            actualFrequency: frequency,
            centsOffset: centsOffset,
            isValid: true,
            isInTune: Math.abs(centsOffset) < 5,
            tuningStatus: Math.abs(centsOffset) < 5 ? 'In Tune' : (centsOffset > 0 ? 'Sharp' : 'Flat')
        };
    }
}
