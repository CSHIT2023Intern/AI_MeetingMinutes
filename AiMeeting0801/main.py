import os
from datetime import datetime
from werkzeug.utils import secure_filename
from flask import Flask, request, render_template, redirect, url_for
from UploadAllow import allowed_file
from ListLoad import ListLoad
from trans import trans
from summary import summary
import wave
import pyaudio
import unicodedata


app = Flask(__name__)

# 上傳檔案路徑
UPLOAD_FOLDER = 'upload'
app.config['UPLOAD_FOLDER'] = UPLOAD_FOLDER
datafile = app.config['UPLOAD_FOLDER']

# 錄音設定
# CHUNK: 這是用於錄製音訊時每個音訊緩衝區的大小。這裡設置為 1024，表示每次從音訊設備讀取 1024 個音訊樣本
CHUNK = 512
# FORMAT: 這是錄製音訊時使用的樣本格式。在這裡，pyaudio.paInt16 表示每個樣本是 16 位整數，常見的音訊樣本格式。
FORMAT = pyaudio.paInt16
# CHANNELS: 這是錄製音訊時使用的聲道數。在這裡，CHANNELS = 1 表示使用單聲道進行錄製。
CHANNELS = 1
# RATE: 這是錄製音訊時的採樣率，表示每秒錄製的樣本數。在這裡，RATE = 44100 表示錄製時每秒錄製 44100 個音訊樣本，這是標準的 CD 音質採樣率。
RATE = 44100
# audio: 這是用於錄製音訊的 PyAudio 對象。PyAudio 是一個用於處理音訊的 Python 庫，它允許您錄製和播放音訊。初始化為None，這意味著audio目前並沒有被賦值為任何值。
audio = None
# frames: 這是用於存儲錄製的音訊數據的列表。每次從音訊設備讀取音訊時，它會將音訊數據存儲在這個列表中，從而形成完整的音訊數據流。
frames = []
# 用於控制是否錄音
recording = False


# 首頁
@app.route('/')
def Index():
    return render_template('index.html')


# 錄音頁
@app.route('/recording', methods=['GET', 'POST'])
def recording():
    # 使用者是否有選擇檔案
    selected_file = request.form.get('selected_wav_file')
    # 是
    if selected_file is not None:
        # 進入語音轉文字
        txt = trans(selected_file)
        # 進入摘要
        response = summary(txt)
        # 更新下拉選單
        wav_files = ListLoad(datafile)
        # 回傳選單和摘要資料到前端
        return render_template('recording.html', wav_files=wav_files, text=response)
    # 否
    else:
        # 更新下拉選單
        wav_files = ListLoad(datafile)
        # 傳更新選單到前端
        return render_template('recording.html', wav_files=wav_files)


# 開始錄音
@app.route('/start', methods=['GET', 'POST'])
def start():
    global audio, frames, record

    # 建立audio
    audio = pyaudio.PyAudio()

    stream = audio.open(format=FORMAT, channels=CHANNELS,
                        rate=RATE, input=True,
                        frames_per_buffer=CHUNK)

    frames = []

    # 開始錄音
    record = True

    while record:
        data = stream.read(CHUNK)
        frames.append(data)

    stream.stop_stream()
    stream.close()
    audio.terminate()
    return redirect(url_for('recording'))


# 停止錄音
@app.route('/stop', methods=['GET', 'POST'])
def stop():
    global recording

    # 停止錄音
    recording = False

    # 取得當前的日期時間
    current_datetime = datetime.now().strftime("%Y-%m-%d__%H-%M-%S")

    # 檔案名稱為 Record_日期時間.wav
    filename = "紀錄"+current_datetime+".wav"

    # 檔案路徑
    filepath = os.path.join(app.config['UPLOAD_FOLDER'], filename)

    # 使用Wave把檔案存入
    with wave.open(filepath, 'wb') as wf:
        wf.setnchannels(CHANNELS)
        wf.setsampwidth(audio.get_sample_size(FORMAT))
        wf.setframerate(RATE)
        wf.writeframes(b''.join(frames))

    # 回到錄音頁重新開始
    return redirect(url_for('recording'))


# 上傳頁-上傳檔案並更新下拉選單可以選擇進摘要
@app.route('/upload_select', methods=['POST', 'GET'])
def upload_select():
    # 使用者是否有選擇檔案
    selected_file = request.form.get('selected_wav_file')
    # 是
    if selected_file is not None:
        # 進入語音轉文字
        txt = trans(selected_file)
        # 進入摘要
        response = summary(txt)
        # 更新下拉選單
        wav_files = ListLoad(datafile)
        # 回傳選單和摘要資料到前端
        return render_template('UploadResult.html', wav_files=wav_files, text=response)
    # 否
    else:
        # 上傳檔案
        if request.method == 'POST':
            file = request.files['file']
            if file and allowed_file(file.filename):
                filename = secure_filename(file.filename)
                file.save(os.path.join(app.config['UPLOAD_FOLDER'], filename))
        # 更新下拉選單
        wav_files = ListLoad(datafile)
        # 回傳選單到前端
        return render_template('UploadResult.html', wav_files=wav_files)


if __name__ == '__main__':
    app.run(debug=True)
