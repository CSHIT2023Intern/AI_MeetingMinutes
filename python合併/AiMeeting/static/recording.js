navigator.mediaDevices.getUserMedia({ audio: true })
    .then(function (stream) {
        mediaRecorder = new MediaRecorder(stream);

        mediaRecorder.addEventListener('dataavailable', function (event) {
            audioChunks.push(event.data);
        });

        document.getElementById('startButton').addEventListener('click', function () {
            audioChunks = [];
            mediaRecorder.start();
            document.getElementById('startButton').disabled = true;
            document.getElementById('stopButton').disabled = false;
            document.getElementById('recordingStatus').textContent = '錄音中...';
        });

        document.getElementById('stopButton').addEventListener('click', function () {
            mediaRecorder.stop();
            document.getElementById('startButton').disabled = false;
            document.getElementById('stopButton').disabled = true;
            document.getElementById('recordingStatus').textContent = '';
        });

        mediaRecorder.addEventListener('stop', function () {
            const audioBlob = new Blob(audioChunks, { type: 'audio/wav' });
            const audioUrl = URL.createObjectURL(audioBlob);

            // 上傳錄音檔案到Flask伺服器
            const formData = new FormData();
            formData.append('audio', audioBlob);

            fetch('/recording', { method: 'POST', body: formData })
                .then(function (response) {
                    if (response.ok) {
                        console.log('檔案上傳成功');
                        // 更新檔案列表
                        getWavFiles();
                    } else {
                        console.error('檔案上傳失敗');
                    }
                })
                .catch(function (error) {
                    console.error('檔案上傳失敗:', error);
                });
        });
    })
    .catch(function (error) {
        console.error('取得錄音權限失敗:', error);
    });