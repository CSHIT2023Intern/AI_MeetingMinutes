import pyaudio
import wave
import os
from datetime import datetime

p = pyaudio.PyAudio()
frames = []


def start_record():
    global frames
    frames = []
    stream = p.open(format=pyaudio.paInt16, channels=2,
                    rate=44100, input=True, frames_per_buffer=1024)
    print("开始录音...")
    while True:
        data = stream.read(1024)
        frames.append(data)
        # 添加停止录音的条件，例如按下停止按钮或达到一定的时间限制


def end_record():
    global frames
    p.terminate()
    print("停止录音")
    save_recording(frames)


def save_recording(frames):
    folder_name = "MeetingRc"
    if not os.path.exists(folder_name):
        os.makedirs(folder_name)

    current_datetime = datetime.now().strftime("%Y%m%d_%H%M%S")
    file_name = f"會議記錄_{current_datetime}.wav"
    file_path = os.path.join(folder_name, file_name)

    wf = wave.open(file_path, 'wb')
    wf.setnchannels(2)
    wf.setsampwidth(pyaudio.PyAudio().get_sample_size(pyaudio.paInt16))
    wf.setframerate(44100)
    wf.writeframes(b''.join(frames))
    wf.close()
    print(f"錄音已保存為 {file_path}")


# 调用录音功能示例
start_record()
input("按Enter键停止录音...")
end_record()
