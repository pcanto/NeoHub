// Exit delay beep audio functionality
window.exitDelayBeep = {
    audioContext: null,
    beepInterval: null,

    // Start beeping at 1-second intervals
    startBeeping: function() {
        if (this.beepInterval) {
            return; // Already beeping
        }

        // Create audio context on first use
        if (!this.audioContext) {
            this.audioContext = new (window.AudioContext || window.webkitAudioContext)();
        }

        // Resume if suspended (mobile browsers require user interaction)
        if (this.audioContext.state === 'suspended') {
            this.audioContext.resume();
        }

        // Play immediately
        this.playBeep();

        // Then every second
        this.beepInterval = setInterval(() => {
            this.playBeep();
        }, 1000);
    },

    // Stop beeping
    stopBeeping: function() {
        if (this.beepInterval) {
            clearInterval(this.beepInterval);
            this.beepInterval = null;
        }
    },

    // Play a single beep (800Hz for 100ms)
    playBeep: function() {
        if (!this.audioContext) {
            this.audioContext = new (window.AudioContext || window.webkitAudioContext)();
        }

        const oscillator = this.audioContext.createOscillator();
        const gainNode = this.audioContext.createGain();

        oscillator.connect(gainNode);
        gainNode.connect(this.audioContext.destination);

        oscillator.frequency.value = 800; // 800Hz beep
        oscillator.type = 'sine';

        gainNode.gain.setValueAtTime(0.3, this.audioContext.currentTime);
        gainNode.gain.exponentialRampToValueAtTime(0.01, this.audioContext.currentTime + 0.1);

        oscillator.start(this.audioContext.currentTime);
        oscillator.stop(this.audioContext.currentTime + 0.1);
    }
};
