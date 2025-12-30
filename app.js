/**
 * Main application logic for Guitar Tuner
 * Connects audio capture with pitch detection and UI updates
 */

// Global instances
let audioCapture = null;
let pitchDetector = null;
let isRunning = false;

// UI Elements
const startButton = document.getElementById('startButton');
const detectedNote = document.getElementById('detectedNote');
const frequency = document.getElementById('frequency');
const centsDisplay = document.getElementById('centsDisplay');
const tuningNeedle = document.getElementById('tuningNeedle');
const status = document.getElementById('status');
const stringItems = document.querySelectorAll('.string-item');

// Constants
const SAMPLE_RATE = 44100;
const BUFFER_SIZE = 4096;
const UPDATE_INTERVAL = 100; // ms

// Initialize on page load
window.addEventListener('DOMContentLoaded', () => {
    // Check browser support
    if (!AudioCapture.isSupported()) {
        showStatus('Il tuo browser non supporta l\'accesso al microfono. Usa Chrome, Firefox, Safari o Edge.', 'error');
        startButton.disabled = true;
        return;
    }

    // Initialize pitch detector
    pitchDetector = new PitchDetector(SAMPLE_RATE, 70, 1500, 0.05);

    // Setup button click handler
    startButton.addEventListener('click', toggleTuner);

    showStatus('Premi AVVIA per iniziare l\'accordatura', 'info');
});

/**
 * Toggles the tuner on/off
 */
async function toggleTuner() {
    if (!isRunning) {
        await startTuner();
    } else {
        stopTuner();
    }
}

/**
 * Starts the tuner
 */
async function startTuner() {
    try {
        showStatus('Inizializzazione microfono...', 'info');
        startButton.disabled = true;

        // Initialize audio capture if not already initialized
        if (!audioCapture) {
            audioCapture = new AudioCapture(SAMPLE_RATE, BUFFER_SIZE);
            const result = await audioCapture.initialize();

            if (!result.success) {
                showStatus(result.message, 'error');
                startButton.disabled = false;
                return;
            }
        }

        // Set up audio data callback
        audioCapture.onAudioData = (samples) => {
            if (isRunning) {
                processAudio(samples);
            }
        };

        // Start capture
        audioCapture.start();
        isRunning = true;

        // Update UI
        startButton.classList.remove('btn-primary');
        startButton.classList.add('btn-danger');
        startButton.querySelector('.btn-icon').textContent = '⏹';
        startButton.querySelector('.btn-text').textContent = 'FERMA';
        startButton.disabled = false;

        showStatus('Suona una corda della chitarra...', 'success');
        detectedNote.classList.add('detecting');
    } catch (error) {
        console.error('Error starting tuner:', error);
        showStatus('Errore nell\'avvio dell\'accordatore: ' + error.message, 'error');
        startButton.disabled = false;
        isRunning = false;
    }
}

/**
 * Stops the tuner
 */
function stopTuner() {
    if (audioCapture) {
        audioCapture.stop();
    }

    isRunning = false;

    // Update UI
    startButton.classList.remove('btn-danger');
    startButton.classList.add('btn-primary');
    startButton.querySelector('.btn-icon').textContent = '▶';
    startButton.querySelector('.btn-text').textContent = 'AVVIA';

    showStatus('Accordatore fermato. Premi AVVIA per ricominciare.', 'info');

    // Reset displays
    detectedNote.textContent = '--';
    detectedNote.classList.remove('detecting', 'flat', 'sharp');
    frequency.textContent = '--- Hz';
    centsDisplay.textContent = '0 cents';
    centsDisplay.className = 'cents-display';
    tuningNeedle.style.left = '50%';
    
    // Clear active strings
    stringItems.forEach(item => item.classList.remove('active'));
}

/**
 * Processes audio samples and updates UI
 * @param {Float32Array} samples
 */
function processAudio(samples) {
    // Detect pitch
    const result = pitchDetector.detectPitch(samples);

    if (result.isValid && result.frequency > 0) {
        // Get musical note
        const note = PitchDetector.getNoteFromFrequency(result.frequency);

        if (note.isValid) {
            updateUI(note);
        }
    } else {
        // No valid pitch detected - keep showing last note or show waiting state
        if (detectedNote.textContent === '--') {
            detectedNote.classList.add('detecting');
        }
    }
}

/**
 * Updates the UI with detected note information
 * @param {Object} note - Musical note object
 */
function updateUI(note) {
    // Update note display
    detectedNote.textContent = note.noteNameAnglo + note.octave;
    detectedNote.classList.remove('detecting');

    // Update frequency
    frequency.textContent = note.actualFrequency.toFixed(2) + ' Hz';

    // Update cents
    const cents = note.centsOffset;
    centsDisplay.textContent = (cents >= 0 ? '+' : '') + cents.toFixed(1) + ' cents';

    // Update color based on tuning
    detectedNote.classList.remove('flat', 'sharp');
    centsDisplay.className = 'cents-display';

    if (note.isInTune) {
        centsDisplay.classList.add('in-tune');
    } else if (cents < 0) {
        detectedNote.classList.add('flat');
        centsDisplay.classList.add('flat');
    } else {
        detectedNote.classList.add('sharp');
        centsDisplay.classList.add('sharp');
    }

    // Update tuning needle position
    // Map cents (-50 to +50) to percentage (0% to 100%)
    const needlePosition = 50 + (cents / 50) * 50;
    const clampedPosition = Math.max(0, Math.min(100, needlePosition));
    tuningNeedle.style.left = clampedPosition + '%';

    // Highlight matching guitar string
    highlightString(note);
}

/**
 * Highlights the matching guitar string
 * @param {Object} note
 */
function highlightString(note) {
    const noteKey = note.noteNameAnglo + note.octave;

    stringItems.forEach(item => {
        const stringNote = item.getAttribute('data-note');
        if (stringNote === noteKey) {
            item.classList.add('active');
        } else {
            item.classList.remove('active');
        }
    });
}

/**
 * Shows status message
 * @param {string} message
 * @param {string} type - 'info', 'success', 'error'
 */
function showStatus(message, type = 'info') {
    status.innerHTML = `<p>${message}</p>`;
    status.className = 'status';
    
    if (type === 'error') {
        status.classList.add('error');
    } else if (type === 'success') {
        status.classList.add('success');
    }
}

// Cleanup on page unload
window.addEventListener('beforeunload', () => {
    if (audioCapture) {
        audioCapture.dispose();
    }
});

// Handle visibility change (pause when tab is hidden)
document.addEventListener('visibilitychange', () => {
    if (document.hidden && isRunning) {
        // Optionally pause when tab is hidden to save resources
        // Uncomment if desired:
        // stopTuner();
    }
});
