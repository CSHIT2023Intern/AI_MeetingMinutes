import os
import wave
import pyaudio
import uuid
from flask import Flask, render_template, request, redirect, url_for

app = Flask(__name__)

UPLOAD_FOLDER = 'upload'
app.config['UPLOAD_FOLDER'] = UPLOAD_FOLDER

CHUNK = 1024
FORMAT = pyaudio.paInt16
CHANNELS = 1
RATE = 44100
record = False
audio = None
frames = []

# 開始方法


def start_recording():
    global audio, frames
    audio = pyaudio.PyAudio()
    stream = audio.open(format=FORMAT, channels=CHANNELS,
                        rate=RATE, input=True,
                        frames_per_buffer=CHUNK)

    frames = []
    print("Recording started...")
    # # 計算需要錄製的音頻帧數
    # frames_to_record = int(RATE / CHUNK * RECORD_SECONDS)

    # for _ in range(frames_to_record):
    #     data = stream.read(CHUNK)
    #     frames.append(data)
    while True:
        data = stream.read(CHUNK)
        frames.append(data)

        # You can set a time-based limit or a different condition to stop recording
        if len(frames) > 500000000:
            break

    print("Recording stopped.")
    stream.stop_stream()
    stream.close()
    audio.terminate()

# 存檔方法


def save_recording():
    if not os.path.exists(app.config['UPLOAD_FOLDER']):
        os.makedirs(app.config['UPLOAD_FOLDER'])

    file_name = str(uuid.uuid4()) + ".wav"
    file_path = os.path.join(app.config['UPLOAD_FOLDER'], file_name)

    with wave.open(file_path, 'wb') as wf:
        wf.setnchannels(CHANNELS)
        wf.setsampwidth(audio.get_sample_size(FORMAT))
        wf.setframerate(RATE)
        wf.writeframes(b''.join(frames))

    print("Recording saved as", file_path)
    return file_name

# 首頁


@app.route('/')
def text():
    return render_template('text.html')

# 開始


@app.route('/start', methods=['GET', 'POST'])
def start():
    start_recording()
    return redirect(url_for('text'))

# 停止


@app.route('/stop', methods=['GET', 'POST'])
def stop():
    file_name = save_recording()
    return f"Recording saved as {file_name}"


if __name__ == '__main__':
    app.run(debug=True)
