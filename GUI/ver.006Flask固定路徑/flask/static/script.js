let audioChunks = [];
let mediaRecorder;

// 函式：從伺服器獲取WAV檔案列表
function getWavFiles() {
    fetch('/wav_files')
        .then(function (response) {
            return response.json();
        })
        .then(function (data) {
            const fileList = document.getElementById('fileList');
            fileList.innerHTML = '';

            // 創建檔案連結並加上選擇功能
            data.forEach(function (file) {
                const link = document.createElement('a');
                link.href = '#';
                link.textContent = file.name;
                link.addEventListener('click', function (event) {
                    event.preventDefault();
                    selectWavFile(file.url);
                });
                fileList.appendChild(link);
                fileList.appendChild(document.createElement('br'));
            });

        })
        .catch(function (error) {
            console.error('無法獲取檔案列表:', error);
        });
}

function selectWavFile(url) {
    const summaryForm = document.getElementById('summaryForm');
    const selectedWavFileInput = document.getElementById('selectedWavFile');
    selectedWavFileInput.value = url;
    summaryForm.querySelector('button').disabled = false;
}

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

            fetch('/audio', { method: 'POST', body: formData })
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

// 網頁載入後獲取WAV檔案列表
document.addEventListener('DOMContentLoaded', function () {
    getWavFiles();
});

function startRecord() {
    fetch('/start_record')
        .then(response => response.text())
        .then(data => console.log(data))
        .catch(error => console.log(error));
}

function stopRecord() {
    fetch('/end_record')
        .then(response => response.text())
        .then(data => console.log(data))
        .catch(error => console.log(error));
}

function generate_summary_button_click() {
    fetch('/generate_summary')
        .then(response => response.text())
        .then(data => console.log(data))
        .catch(error => console.log(error));
}

function generate_summary_button_click() {
    // 獲取上傳的檔案
    const fileInput = document.getElementById('fileInput');
    const file = fileInput.files[0];

    // 建立 FormData 對象
    const formData = new FormData();
    formData.append('file', file);

    // 發送包含上傳檔案的請求
    fetch('/generate_summary', {
        method: 'POST',
        body: formData
    })
        .then(response => response.text())
        .then(data => console.log(data))
        .catch(error => console.log(error));
}

document.addEventListener('DOMContentLoaded', function () {
    const fileLinks = document.querySelectorAll('.file-link');
    const summaryForm = document.getElementById('summaryForm');
    const selectedWavFileInput = document.getElementById('selectedWavFile');

    fileLinks.forEach(function (link) {
        link.addEventListener('click', function (event) {
            event.preventDefault();
            const filePath = link.dataset.url;
            selectedWavFileInput.value = filePath;
            summaryForm.querySelector('button').disabled = false;
        });
    });
});