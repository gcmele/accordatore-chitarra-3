using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace AccordatoreChitarra
{
    /// <summary>
    /// Main window for the guitar tuner application
    /// Handles audio processing, pitch detection, and UI updates
    /// </summary>
    public partial class MainWindow : Window
    {
        #region Fields

        private AudioEngine _audioEngine;
        private PitchDetector _pitchDetector;
        private DispatcherTimer _uiUpdateTimer;
        
        // Thread-safe audio buffer
        private readonly object _bufferLock = new object();
        private float[] _audioBuffer;
        private float _currentRMS = 0;
        
        // UI state
        private bool _isRunning = false;
        
        // Standard guitar tuning frequencies (Hz)
        private readonly Dictionary<string, float> _guitarTuning = new Dictionary<string, float>
        {
            { "E", 82.41f },   // E2 - 6th string (low E)
            { "A", 110.00f },  // A2 - 5th string
            { "D", 146.83f },  // D3 - 4th string
            { "G", 196.00f },  // G3 - 3rd string
            { "B", 246.94f },  // B3 - 2nd string
            { "e", 329.63f }   // E4 - 1st string (high E)
        };

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes the MainWindow
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
            
            // Initialize audio engine and pitch detector
            _audioEngine = new AudioEngine
            {
                SampleRate = 44100,
                Channels = 1,
                BufferMilliseconds = 100,
                Gain = 1.0f
            };
            
            _pitchDetector = new PitchDetector(
                sampleRate: 44100,
                minFrequency: 70,
                maxFrequency: 1500,
                threshold: 0.05f
            );
            
            // Initialize UI update timer (20 FPS = 50ms interval)
            _uiUpdateTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(50)
            };
            _uiUpdateTimer.Tick += UiUpdateTimer_Tick;
            
            // Subscribe to audio engine events
            _audioEngine.AudioDataAvailable += AudioEngine_AudioDataAvailable;
            _audioEngine.RecordingStopped += AudioEngine_RecordingStopped;
            
            // Subscribe to UI events
            StartStopButton.Click += StartStopButton_Click;
            GainSlider.ValueChanged += GainSlider_ValueChanged;
            MicrophoneComboBox.SelectionChanged += MicrophoneComboBox_SelectionChanged;
            
            // Populate microphone list
            PopulateMicrophoneList();
            
            // Handle window closing
            Closing += MainWindow_Closing;
        }

        #endregion

        #region Microphone Management

        /// <summary>
        /// Populates the microphone combo box with available devices
        /// </summary>
        private void PopulateMicrophoneList()
        {
            try
            {
                MicrophoneComboBox.Items.Clear();
                
                var devices = AudioEngine.EnumerateDevices();
                
                if (devices.Count == 0)
                {
                    MicrophoneComboBox.Items.Add("Nessun microfono disponibile");
                    MicrophoneComboBox.SelectedIndex = 0;
                    MicrophoneComboBox.IsEnabled = false;
                    return;
                }
                
                // Add default device
                MicrophoneComboBox.Items.Add("Microfono Predefinito");
                
                // Add all available devices
                foreach (var device in devices)
                {
                    MicrophoneComboBox.Items.Add(device.Name);
                }
                
                // Select default device
                MicrophoneComboBox.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Errore durante l'enumerazione dei microfoni: {ex.Message}",
                    "Errore", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// Handles Start/Stop button click
        /// </summary>
        private void StartStopButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!_isRunning)
                {
                    // Start recording
                    _audioEngine.StartRecording();
                    _uiUpdateTimer.Start();
                    _isRunning = true;
                    
                    // Update button
                    StartStopButton.Content = "FERMA";
                    StartStopButton.Background = new SolidColorBrush(Color.FromRgb(231, 76, 60)); // Red
                    
                    // Disable microphone selection while running
                    MicrophoneComboBox.IsEnabled = false;
                }
                else
                {
                    // Stop recording
                    StopTuner();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Errore nell'avvio dell'accordatore: {ex.Message}",
                    "Errore", MessageBoxButton.OK, MessageBoxImage.Error);
                StopTuner();
            }
        }

        /// <summary>
        /// Handles gain slider value changes
        /// </summary>
        private void GainSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_audioEngine == null)
                return;
            
            // Scale slider value (0-100) to gain (0-2)
            float gain = (float)(e.NewValue / 50.0);
            _audioEngine.Gain = gain;
            
            // Update display
            if (GainValueDisplay != null)
            {
                GainValueDisplay.Text = $"{(int)e.NewValue}%";
            }
        }

        /// <summary>
        /// Handles microphone selection changes
        /// </summary>
        private void MicrophoneComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_audioEngine == null || _isRunning)
                return;
            
            int selectedIndex = MicrophoneComboBox.SelectedIndex;
            
            // -1 for default device, or device number for specific device
            _audioEngine.SelectedDeviceNumber = selectedIndex == 0 ? -1 : selectedIndex - 1;
        }

        /// <summary>
        /// Handles audio data available event from audio engine
        /// </summary>
        private void AudioEngine_AudioDataAvailable(object sender, AudioDataAvailableEventArgs e)
        {
            if (e.Samples == null || e.Samples.Length == 0)
                return;
            
            // Thread-safe buffer update
            lock (_bufferLock)
            {
                _audioBuffer = (float[])e.Samples.Clone();
                
                // Calculate RMS for level meter
                _currentRMS = CalculateRMS(_audioBuffer);
            }
        }

        /// <summary>
        /// Handles recording stopped event
        /// </summary>
        private void AudioEngine_RecordingStopped(object sender, NAudio.Wave.StoppedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                if (e.Exception != null)
                {
                    MessageBox.Show($"Errore durante la registrazione: {e.Exception.Message}",
                        "Errore", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                StopTuner();
            });
        }

        /// <summary>
        /// Handles UI update timer tick (20 FPS)
        /// </summary>
        private void UiUpdateTimer_Tick(object sender, EventArgs e)
        {
            try
            {
                float[] bufferCopy = null;
                float rms = 0;
                
                // Get a copy of the audio buffer
                lock (_bufferLock)
                {
                    if (_audioBuffer != null)
                    {
                        bufferCopy = (float[])_audioBuffer.Clone();
                    }
                    rms = _currentRMS;
                }
                
                // Update level meter
                UpdateLevelMeter(rms);
                
                // Process audio if available
                if (bufferCopy != null && bufferCopy.Length > 0)
                {
                    ProcessAudioAndUpdateUI(bufferCopy);
                }
                else
                {
                    // No audio data - reset displays
                    ResetDisplays();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in UI update: {ex.Message}");
            }
        }

        /// <summary>
        /// Handles window closing event
        /// </summary>
        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                StopTuner();
                
                // Dispose resources
                _uiUpdateTimer?.Stop();
                _audioEngine?.Dispose();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error during cleanup: {ex.Message}");
            }
        }

        #endregion

        #region Audio Processing

        /// <summary>
        /// Processes audio buffer and updates UI with pitch detection results
        /// </summary>
        private void ProcessAudioAndUpdateUI(float[] samples)
        {
            // Detect pitch
            var pitchResult = _pitchDetector.DetectPitch(samples);
            
            if (!pitchResult.IsValid || pitchResult.Frequency <= 0)
            {
                ResetDisplays();
                return;
            }
            
            // Get musical note from frequency
            var note = PitchDetector.GetNoteFromFrequency(pitchResult.Frequency);
            
            if (!note.IsValid)
            {
                ResetDisplays();
                return;
            }
            
            // Update displays
            UpdateFrequencyDisplay(pitchResult.Frequency);
            UpdateCentsDisplay(note.CentsOffset);
            UpdateTuningNeedle(note.CentsOffset);
            UpdateGuitarPegs(note);
        }

        /// <summary>
        /// Calculates RMS (Root Mean Square) of audio samples
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

        #region UI Update Methods

        /// <summary>
        /// Updates the frequency display
        /// </summary>
        private void UpdateFrequencyDisplay(float frequency)
        {
            FrequencyDisplay.Text = $"{frequency:F2} Hz";
        }

        /// <summary>
        /// Updates the cents offset display with color coding
        /// </summary>
        private void UpdateCentsDisplay(float cents)
        {
            // Format cents value
            string sign = cents >= 0 ? "+" : "";
            CentsDisplay.Text = $"{sign}{cents:F1}";
            
            // Color code based on tuning accuracy
            if (Math.Abs(cents) < 5)
            {
                // Green - in tune (within ±5 cents)
                CentsDisplay.Foreground = new SolidColorBrush(Color.FromRgb(39, 174, 96));
            }
            else if (Math.Abs(cents) < 15)
            {
                // Orange - close (within ±15 cents)
                CentsDisplay.Foreground = new SolidColorBrush(Color.FromRgb(230, 126, 34));
            }
            else
            {
                // Red - out of tune (more than ±15 cents)
                CentsDisplay.Foreground = new SolidColorBrush(Color.FromRgb(231, 76, 60));
            }
        }

        /// <summary>
        /// Updates the tuning needle position based on cents offset
        /// </summary>
        private void UpdateTuningNeedle(float cents)
        {
            // Clamp cents to -50 to +50 range
            cents = Math.Max(-50, Math.Min(50, cents));
            
            // Calculate needle position
            // Center = 0 cents
            // Full left = -50 cents
            // Full right = +50 cents
            double canvasWidth = TuningCanvas.ActualWidth;
            double centerX = canvasWidth / 2.0;
            double needleX = centerX + (cents / 50.0) * (canvasWidth / 2.0);
            
            // Update needle transform
            NeedleTransform.X = needleX;
        }

        /// <summary>
        /// Updates guitar pegs highlighting based on detected note
        /// </summary>
        private void UpdateGuitarPegs(MusicalNote note)
        {
            // Reset all pegs to default style
            ResetAllPegs();
            
            if (!note.IsValid)
                return;
            
            // Find closest guitar string
            string closestPeg = FindClosestGuitarString(note.ActualFrequency);
            
            if (string.IsNullOrEmpty(closestPeg))
                return;
            
            // Highlight the corresponding peg
            HighlightPeg(closestPeg);
        }

        /// <summary>
        /// Finds the closest guitar string based on frequency
        /// </summary>
        private string FindClosestGuitarString(float frequency)
        {
            string closestPeg = null;
            float minDifference = float.MaxValue;
            
            foreach (var tuning in _guitarTuning)
            {
                float difference = Math.Abs(frequency - tuning.Value);
                
                // Accept frequencies within 50 Hz of target (approximately 1 semitone)
                if (difference < minDifference && difference < 50)
                {
                    minDifference = difference;
                    closestPeg = tuning.Key;
                }
            }
            
            return closestPeg;
        }

        /// <summary>
        /// Resets all guitar pegs to default style
        /// </summary>
        private void ResetAllPegs()
        {
            var defaultStyle = (Style)FindResource("TuningPegStyle");
            
            PegE.Style = defaultStyle;
            PegA.Style = defaultStyle;
            PegD.Style = defaultStyle;
            PegG.Style = defaultStyle;
            PegB.Style = defaultStyle;
            Pege.Style = defaultStyle;
        }

        /// <summary>
        /// Highlights a specific guitar peg
        /// </summary>
        private void HighlightPeg(string pegName)
        {
            var activeStyle = (Style)FindResource("ActivePegStyle");
            
            switch (pegName)
            {
                case "E":
                    PegE.Style = activeStyle;
                    break;
                case "A":
                    PegA.Style = activeStyle;
                    break;
                case "D":
                    PegD.Style = activeStyle;
                    break;
                case "G":
                    PegG.Style = activeStyle;
                    break;
                case "B":
                    PegB.Style = activeStyle;
                    break;
                case "e":
                    Pege.Style = activeStyle;
                    break;
            }
        }

        /// <summary>
        /// Updates the level meter based on RMS value
        /// </summary>
        private void UpdateLevelMeter(float rms)
        {
            // Scale RMS to level meter width (0-1 range to 0-100% width)
            // Apply logarithmic scaling for better visual representation
            double level = Math.Min(1.0, rms * 10.0); // Scale factor
            
            // Get the parent border width
            if (LevelMeter.Parent is Grid grid && grid.Parent is Border border)
            {
                double maxWidth = border.ActualWidth - 10; // Account for padding
                LevelMeter.Width = level * maxWidth;
            }
        }

        /// <summary>
        /// Resets all displays to default state
        /// </summary>
        private void ResetDisplays()
        {
            FrequencyDisplay.Text = "-- Hz";
            CentsDisplay.Text = "0";
            CentsDisplay.Foreground = (Brush)FindResource("PrimaryHueMidBrush");
            
            // Reset needle to center
            if (TuningCanvas.ActualWidth > 0)
            {
                NeedleTransform.X = TuningCanvas.ActualWidth / 2.0;
            }
            
            // Reset all pegs
            ResetAllPegs();
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Stops the tuner and resets UI
        /// </summary>
        private void StopTuner()
        {
            try
            {
                // Stop recording and timer
                _audioEngine?.StopRecording();
                _uiUpdateTimer?.Stop();
                _isRunning = false;
                
                // Update button
                StartStopButton.Content = "AVVIA";
                StartStopButton.Background = (Brush)FindResource("PrimaryHueMidBrush");
                
                // Enable microphone selection
                MicrophoneComboBox.IsEnabled = true;
                
                // Reset displays
                ResetDisplays();
                
                // Reset level meter
                LevelMeter.Width = 0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error stopping tuner: {ex.Message}");
            }
        }

        #endregion
    }
}
