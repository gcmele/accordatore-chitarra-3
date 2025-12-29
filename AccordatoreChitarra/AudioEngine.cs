using System;
using System.Collections.Generic;
using System.Linq;
using NAudio.Wave;

namespace AccordatoreChitarra
{
    /// <summary>
    /// Manages audio input using NAudio, including device enumeration, 
    /// buffer management, audio stream processing, and gain control.
    /// </summary>
    public class AudioEngine : IDisposable
    {
        #region Fields

        private WaveInEvent _waveIn;
        private int _sampleRate;
        private int _channels;
        private int _bufferMilliseconds;
        private float _gain;
        private bool _isRecording;
        private bool _disposed;
        private int _selectedDeviceNumber;

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the sample rate for audio capture (default: 44100 Hz)
        /// </summary>
        public int SampleRate
        {
            get => _sampleRate;
            set
            {
                if (_isRecording)
                    throw new InvalidOperationException("Cannot change sample rate while recording.");
                _sampleRate = value;
            }
        }

        /// <summary>
        /// Gets or sets the number of channels (default: 1 for mono)
        /// </summary>
        public int Channels
        {
            get => _channels;
            set
            {
                if (_isRecording)
                    throw new InvalidOperationException("Cannot change channels while recording.");
                _channels = value;
            }
        }

        /// <summary>
        /// Gets or sets the buffer size in milliseconds (default: 100ms)
        /// </summary>
        public int BufferMilliseconds
        {
            get => _bufferMilliseconds;
            set
            {
                if (_isRecording)
                    throw new InvalidOperationException("Cannot change buffer size while recording.");
                _bufferMilliseconds = value;
            }
        }

        /// <summary>
        /// Gets or sets the gain multiplier for audio input (default: 1.0f)
        /// Valid range: 0.0f to 10.0f
        /// </summary>
        public float Gain
        {
            get => _gain;
            set
            {
                if (value < 0.0f || value > 10.0f)
                    throw new ArgumentOutOfRangeException(nameof(value), "Gain must be between 0.0 and 10.0");
                _gain = value;
            }
        }

        /// <summary>
        /// Gets whether the audio engine is currently recording
        /// </summary>
        public bool IsRecording => _isRecording;

        /// <summary>
        /// Gets or sets the selected microphone device number
        /// </summary>
        public int SelectedDeviceNumber
        {
            get => _selectedDeviceNumber;
            set
            {
                if (_isRecording)
                    throw new InvalidOperationException("Cannot change device while recording.");
                if (value < -1 || value >= WaveInEvent.DeviceCount)
                    throw new ArgumentOutOfRangeException(nameof(value), "Invalid device number.");
                _selectedDeviceNumber = value;
            }
        }

        #endregion

        #region Events

        /// <summary>
        /// Event raised when audio data is available
        /// </summary>
        public event EventHandler<AudioDataAvailableEventArgs> AudioDataAvailable;

        /// <summary>
        /// Event raised when recording has stopped
        /// </summary>
        public event EventHandler<StoppedEventArgs> RecordingStopped;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the AudioEngine class with default settings
        /// </summary>
        public AudioEngine()
        {
            _sampleRate = 44100;
            _channels = 1;
            _bufferMilliseconds = 100;
            _gain = 1.0f;
            _selectedDeviceNumber = -1; // Default device
            _isRecording = false;
            _disposed = false;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Enumerates all available microphone devices
        /// </summary>
        /// <returns>List of available audio input devices</returns>
        public static List<AudioDeviceInfo> EnumerateDevices()
        {
            var devices = new List<AudioDeviceInfo>();

            for (int i = 0; i < WaveInEvent.DeviceCount; i++)
            {
                try
                {
                    var capabilities = WaveInEvent.GetCapabilities(i);
                    devices.Add(new AudioDeviceInfo
                    {
                        DeviceNumber = i,
                        Name = capabilities.ProductName,
                        Channels = capabilities.Channels,
                        SupportsWaveFormat = true
                    });
                }
                catch (Exception ex)
                {
                    // Log or handle device enumeration error
                    System.Diagnostics.Debug.WriteLine($"Error enumerating device {i}: {ex.Message}");
                }
            }

            return devices;
        }

        /// <summary>
        /// Gets information about a specific device
        /// </summary>
        /// <param name="deviceNumber">The device number</param>
        /// <returns>Device information</returns>
        public static AudioDeviceInfo GetDeviceInfo(int deviceNumber)
        {
            if (deviceNumber < 0 || deviceNumber >= WaveInEvent.DeviceCount)
                throw new ArgumentOutOfRangeException(nameof(deviceNumber), "Invalid device number.");

            var capabilities = WaveInEvent.GetCapabilities(deviceNumber);
            return new AudioDeviceInfo
            {
                DeviceNumber = deviceNumber,
                Name = capabilities.ProductName,
                Channels = capabilities.Channels,
                SupportsWaveFormat = true
            };
        }

        /// <summary>
        /// Starts audio recording from the selected device
        /// </summary>
        public void StartRecording()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(AudioEngine));

            if (_isRecording)
                throw new InvalidOperationException("Already recording.");

            try
            {
                // Initialize WaveInEvent
                _waveIn = new WaveInEvent
                {
                    DeviceNumber = _selectedDeviceNumber,
                    WaveFormat = new WaveFormat(_sampleRate, 16, _channels),
                    BufferMilliseconds = _bufferMilliseconds
                };

                // Subscribe to events
                _waveIn.DataAvailable += OnDataAvailable;
                _waveIn.RecordingStopped += OnRecordingStopped;

                // Start recording
                _waveIn.StartRecording();
                _isRecording = true;
            }
            catch (Exception ex)
            {
                // Clean up on error
                if (_waveIn != null)
                {
                    _waveIn.DataAvailable -= OnDataAvailable;
                    _waveIn.RecordingStopped -= OnRecordingStopped;
                    _waveIn.Dispose();
                    _waveIn = null;
                }
                throw new InvalidOperationException("Failed to start recording.", ex);
            }
        }

        /// <summary>
        /// Stops audio recording
        /// </summary>
        public void StopRecording()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(AudioEngine));

            if (!_isRecording)
                return;

            try
            {
                _waveIn?.StopRecording();
                _isRecording = false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error stopping recording: {ex.Message}");
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Handles the DataAvailable event from WaveInEvent
        /// </summary>
        private void OnDataAvailable(object sender, WaveInEventArgs e)
        {
            if (e.BytesRecorded == 0 || AudioDataAvailable == null)
                return;

            try
            {
                // Convert bytes to float samples
                int sampleCount = e.BytesRecorded / 2; // 16-bit samples = 2 bytes per sample
                float[] samples = new float[sampleCount];

                for (int i = 0; i < sampleCount; i++)
                {
                    // Convert 16-bit PCM to float (-1.0 to 1.0)
                    short sample = BitConverter.ToInt16(e.Buffer, i * 2);
                    samples[i] = sample / 32768f;

                    // Apply gain
                    samples[i] *= _gain;

                    // Clamp to prevent clipping
                    if (samples[i] > 1.0f)
                        samples[i] = 1.0f;
                    else if (samples[i] < -1.0f)
                        samples[i] = -1.0f;
                }

                // Raise event with processed audio data
                var eventArgs = new AudioDataAvailableEventArgs
                {
                    Samples = samples,
                    SampleRate = _sampleRate,
                    Channels = _channels,
                    BytesRecorded = e.BytesRecorded
                };

                AudioDataAvailable?.Invoke(this, eventArgs);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error processing audio data: {ex.Message}");
            }
        }

        /// <summary>
        /// Handles the RecordingStopped event from WaveInEvent
        /// </summary>
        private void OnRecordingStopped(object sender, StoppedEventArgs e)
        {
            _isRecording = false;

            // Raise event
            RecordingStopped?.Invoke(this, e);

            // Log any errors
            if (e.Exception != null)
            {
                System.Diagnostics.Debug.WriteLine($"Recording stopped with error: {e.Exception.Message}");
            }
        }

        #endregion

        #region IDisposable Implementation

        /// <summary>
        /// Disposes the AudioEngine and releases all resources
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Protected implementation of Dispose pattern
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
            {
                // Stop recording if active
                if (_isRecording)
                {
                    try
                    {
                        StopRecording();
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error stopping recording during disposal: {ex.Message}");
                    }
                }

                // Dispose WaveInEvent
                if (_waveIn != null)
                {
                    _waveIn.DataAvailable -= OnDataAvailable;
                    _waveIn.RecordingStopped -= OnRecordingStopped;
                    _waveIn.Dispose();
                    _waveIn = null;
                }
            }

            _disposed = true;
        }

        /// <summary>
        /// Finalizer
        /// </summary>
        ~AudioEngine()
        {
            Dispose(false);
        }

        #endregion
    }

    #region Event Arguments

    /// <summary>
    /// Event arguments for audio data available event
    /// </summary>
    public class AudioDataAvailableEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the audio samples as float array (normalized to -1.0 to 1.0)
        /// </summary>
        public float[] Samples { get; set; }

        /// <summary>
        /// Gets the sample rate of the audio data
        /// </summary>
        public int SampleRate { get; set; }

        /// <summary>
        /// Gets the number of channels
        /// </summary>
        public int Channels { get; set; }

        /// <summary>
        /// Gets the number of bytes recorded
        /// </summary>
        public int BytesRecorded { get; set; }
    }

    #endregion

    #region Audio Device Info

    /// <summary>
    /// Information about an audio input device
    /// </summary>
    public class AudioDeviceInfo
    {
        /// <summary>
        /// Gets or sets the device number
        /// </summary>
        public int DeviceNumber { get; set; }

        /// <summary>
        /// Gets or sets the device name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the number of channels supported
        /// </summary>
        public int Channels { get; set; }

        /// <summary>
        /// Gets or sets whether the device supports the required wave format
        /// </summary>
        public bool SupportsWaveFormat { get; set; }

        /// <summary>
        /// Returns a string representation of the device info
        /// </summary>
        public override string ToString()
        {
            return $"{DeviceNumber}: {Name} ({Channels} channels)";
        }
    }

    #endregion
}
