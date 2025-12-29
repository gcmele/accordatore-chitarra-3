using System;
using System.Linq;

namespace AccordatoreChitarra
{
    /// <summary>
    /// Pitch detection engine using autocorrelation and FFT for accurate frequency detection
    /// Optimized for guitar tuning (82 Hz - 1320 Hz range)
    /// </summary>
    public class PitchDetector
    {
        #region Fields

        private readonly int _sampleRate;
        private readonly int _minFrequency;
        private readonly int _maxFrequency;
        private readonly float _threshold;

        #endregion

        #region Properties

        /// <summary>
        /// Gets the sample rate used for pitch detection
        /// </summary>
        public int SampleRate => _sampleRate;

        /// <summary>
        /// Gets the minimum detectable frequency in Hz
        /// </summary>
        public int MinFrequency => _minFrequency;

        /// <summary>
        /// Gets the maximum detectable frequency in Hz
        /// </summary>
        public int MaxFrequency => _maxFrequency;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the PitchDetector class
        /// </summary>
        /// <param name="sampleRate">Sample rate of the audio (default: 44100 Hz)</param>
        /// <param name="minFrequency">Minimum frequency to detect (default: 70 Hz)</param>
        /// <param name="maxFrequency">Maximum frequency to detect (default: 1500 Hz)</param>
        /// <param name="threshold">Minimum signal threshold (default: 0.1)</param>
        public PitchDetector(int sampleRate = 44100, int minFrequency = 70, int maxFrequency = 1500, float threshold = 0.1f)
        {
            _sampleRate = sampleRate;
            _minFrequency = minFrequency;
            _maxFrequency = maxFrequency;
            _threshold = threshold;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Detects the pitch (fundamental frequency) from audio samples using autocorrelation
        /// </summary>
        /// <param name="samples">Audio samples (normalized -1.0 to 1.0)</param>
        /// <returns>PitchDetectionResult containing frequency and confidence</returns>
        public PitchDetectionResult DetectPitch(float[] samples)
        {
            if (samples == null || samples.Length == 0)
            {
                return new PitchDetectionResult
                {
                    Frequency = 0,
                    Confidence = 0,
                    IsValid = false
                };
            }

            // Calculate RMS to check if signal is strong enough
            float rms = CalculateRMS(samples);
            if (rms < _threshold)
            {
                return new PitchDetectionResult
                {
                    Frequency = 0,
                    Confidence = 0,
                    IsValid = false,
                    RMS = rms
                };
            }

            // Apply autocorrelation
            float frequency = AutocorrelationPitchDetection(samples);

            // Validate frequency range
            if (frequency < _minFrequency || frequency > _maxFrequency)
            {
                return new PitchDetectionResult
                {
                    Frequency = frequency,
                    Confidence = 0,
                    IsValid = false,
                    RMS = rms
                };
            }

            // Calculate confidence based on autocorrelation clarity
            float confidence = CalculateConfidence(samples, frequency);

            return new PitchDetectionResult
            {
                Frequency = frequency,
                Confidence = confidence,
                IsValid = confidence > 0.5f, // Confidence threshold
                RMS = rms
            };
        }

        /// <summary>
        /// Identifies the musical note from a frequency
        /// </summary>
        /// <param name="frequency">Frequency in Hz</param>
        /// <returns>MusicalNote object with note name and cents offset</returns>
        public static MusicalNote GetNoteFromFrequency(float frequency)
        {
            if (frequency <= 0)
            {
                return new MusicalNote
                {
                    NoteName = "",
                    NoteNameAnglo = "",
                    Octave = 0,
                    Frequency = 0,
                    CentsOffset = 0,
                    IsValid = false
                };
            }

            // A4 = 440 Hz as reference
            const float A4 = 440.0f;
            const int A4_MIDI = 69;

            // Calculate MIDI note number
            float halfStepsFromA4 = 12.0f * (float)Math.Log(frequency / A4, 2);
            int midiNote = (int)Math.Round(halfStepsFromA4) + A4_MIDI;

            // Calculate cents offset from perfect pitch
            float perfectFrequency = A4 * (float)Math.Pow(2, (midiNote - A4_MIDI) / 12.0);
            float centsOffset = 1200.0f * (float)Math.Log(frequency / perfectFrequency, 2);

            // Get note name
            int noteIndex = midiNote % 12;
            int octave = (midiNote / 12) - 1;

            string[] noteNamesItalian = { "DO", "DO#", "RE", "RE#", "MI", "FA", "FA#", "SOL", "SOL#", "LA", "LA#", "SI" };
            string[] noteNamesAnglo = { "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B" };

            return new MusicalNote
            {
                NoteName = noteNamesItalian[noteIndex],
                NoteNameAnglo = noteNamesAnglo[noteIndex],
                Octave = octave,
                MidiNote = midiNote,
                Frequency = perfectFrequency,
                ActualFrequency = frequency,
                CentsOffset = centsOffset,
                IsValid = true
            };
        }

        #endregion

        #region Private Methods - Autocorrelation

        /// <summary>
        /// Performs autocorrelation-based pitch detection
        /// </summary>
        private float AutocorrelationPitchDetection(float[] samples)
        {
            int minLag = _sampleRate / _maxFrequency;
            int maxLag = _sampleRate / _minFrequency;
            int bufferSize = Math.Min(samples.Length, maxLag * 2);

            // Calculate autocorrelation
            float[] autocorrelation = new float[maxLag + 1];

            for (int lag = minLag; lag <= maxLag; lag++)
            {
                float sum = 0;
                for (int i = 0; i < bufferSize - lag; i++)
                {
                    sum += samples[i] * samples[i + lag];
                }
                autocorrelation[lag] = sum;
            }

            // Find the first peak (highest autocorrelation value after minimum lag)
            int peakLag = FindFirstPeak(autocorrelation, minLag, maxLag);

            if (peakLag == 0)
                return 0;

            // Parabolic interpolation for better precision
            float refinedLag = ParabolicInterpolation(autocorrelation, peakLag);

            // Convert lag to frequency
            float frequency = _sampleRate / refinedLag;

            return frequency;
        }

        /// <summary>
        /// Finds the first significant peak in autocorrelation
        /// </summary>
        private int FindFirstPeak(float[] autocorrelation, int minLag, int maxLag)
        {
            float maxValue = float.MinValue;
            int maxIndex = 0;

            for (int i = minLag; i <= maxLag; i++)
            {
                if (autocorrelation[i] > maxValue)
                {
                    maxValue = autocorrelation[i];
                    maxIndex = i;
                }
            }

            return maxIndex;
        }

        /// <summary>
        /// Parabolic interpolation for sub-sample precision
        /// </summary>
        private float ParabolicInterpolation(float[] data, int index)
        {
            if (index <= 0 || index >= data.Length - 1)
                return index;

            float alpha = data[index - 1];
            float beta = data[index];
            float gamma = data[index + 1];

            float offset = 0.5f * (alpha - gamma) / (alpha - 2 * beta + gamma);

            return index + offset;
        }

        /// <summary>
        /// Calculates confidence of pitch detection based on autocorrelation clarity
        /// </summary>
        private float CalculateConfidence(float[] samples, float frequency)
        {
            if (frequency <= 0)
                return 0;

            int period = (int)(_sampleRate / frequency);
            if (period <= 0 || period >= samples.Length / 2)
                return 0;

            // Calculate correlation at detected period
            float sum = 0;
            float sumSquares = 0;
            int count = Math.Min(samples.Length - period, period * 2);

            for (int i = 0; i < count; i++)
            {
                sum += samples[i] * samples[i + period];
                sumSquares += samples[i] * samples[i];
            }

            if (sumSquares == 0)
                return 0;

            // Normalized correlation coefficient
            float correlation = sum / sumSquares;

            // Clamp to 0-1 range
            return Math.Max(0, Math.Min(1, correlation));
        }

        /// <summary>
        /// Calculates Root Mean Square (RMS) of audio samples
        /// </summary>
        private float CalculateRMS(float[] samples)
        {
            if (samples == null || samples.Length == 0)
                return 0;

            float sum = 0;
            for (int i = 0; i < samples.Length; i++)
            {
                sum += samples[i] * samples[i];
            }

            return (float)Math.Sqrt(sum / samples.Length);
        }

        #endregion
    }

    #region Result Classes

    /// <summary>
    /// Result of pitch detection
    /// </summary>
    public class PitchDetectionResult
    {
        /// <summary>
        /// Detected frequency in Hz
        /// </summary>
        public float Frequency { get; set; }

        /// <summary>
        /// Confidence level (0.0 to 1.0)
        /// </summary>
        public float Confidence { get; set; }

        /// <summary>
        /// Whether the detection is valid and reliable
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// RMS (Root Mean Square) level of the signal
        /// </summary>
        public float RMS { get; set; }
    }

    /// <summary>
    /// Represents a musical note with frequency and pitch information
    /// </summary>
    public class MusicalNote
    {
        /// <summary>
        /// Note name in Italian notation (DO, RE, MI, etc.)
        /// </summary>
        public string NoteName { get; set; }

        /// <summary>
        /// Note name in Anglo-Saxon notation (C, D, E, etc.)
        /// </summary>
        public string NoteNameAnglo { get; set; }

        /// <summary>
        /// Octave number
        /// </summary>
        public int Octave { get; set; }

        /// <summary>
        /// MIDI note number
        /// </summary>
        public int MidiNote { get; set; }

        /// <summary>
        /// Perfect frequency for this note in Hz
        /// </summary>
        public float Frequency { get; set; }

        /// <summary>
        /// Actual detected frequency in Hz
        /// </summary>
        public float ActualFrequency { get; set; }

        /// <summary>
        /// Offset in cents from perfect pitch (-50 to +50)
        /// Negative = flat, Positive = sharp
        /// </summary>
        public float CentsOffset { get; set; }

        /// <summary>
        /// Whether the note detection is valid
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// Gets the full note name with octave (e.g., "E2" or "MI2")
        /// </summary>
        public string FullName => IsValid ? $"{NoteName}{Octave}" : "";

        /// <summary>
        /// Gets the full Anglo-Saxon note name with octave
        /// </summary>
        public string FullNameAnglo => IsValid ? $"{NoteNameAnglo}{Octave}" : "";

        /// <summary>
        /// Returns whether the note is in tune (within ±5 cents)
        /// </summary>
        public bool IsInTune => IsValid && Math.Abs(CentsOffset) < 5;

        /// <summary>
        /// Returns tuning status: "Sharp" (+), "Flat" (-), or "In Tune"
        /// </summary>
        public string TuningStatus
        {
            get
            {
                if (!IsValid) return "";
                if (IsInTune) return "In Tune";
                return CentsOffset > 0 ? "Sharp" : "Flat";
            }
        }
    }

    #endregion
}