mergeInto(LibraryManager.library, {
    RequestMicrophonePermission: function () {
        if (navigator.mediaDevices && navigator.mediaDevices.getUserMedia) {
            navigator.mediaDevices.getUserMedia({ audio: true })
            .then(function(stream) {
                console.log("Micrófono activado en WebGL");
            })
            .catch(function(err) {
                console.error("Error al acceder al micrófono: " + err);
            });
        }
    }
});