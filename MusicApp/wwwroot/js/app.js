// Create a file named wwwroot/js/app.js

// Helper function to get all keys from localStorage that start with a prefix
window.localStorage.getKeys = function (prefix) {
    let keys = [];
    for (let i = 0; i < localStorage.length; i++) {
        const key = localStorage.key(i);
        if (key.startsWith(prefix)) {
            keys.push(key);
        }
    }
    return keys;
};

// Add audio player functionality
window.initAudioPlayer = function () {
    if (!document.getElementById('audioPreview')) {
        const audioElement = document.createElement('audio');
        audioElement.id = 'audioPreview';
        audioElement.addEventListener('ended', function() {
            window.dispatchEvent(new CustomEvent('previewEnded'));
        });
        document.body.appendChild(audioElement);
    }
};

window.playAudioPreview = function (url) {
    const audio = document.getElementById('audioPreview');
    if (audio) {
        audio.src = url;
        audio.play();
    }
};

window.stopAudioPreview = function () {
    const audio = document.getElementById('audioPreview');
    if (audio) {
        audio.pause();
        audio.currentTime = 0;
    }
};