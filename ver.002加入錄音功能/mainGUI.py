import tkinter as tk
from tkinter import filedialog
import shutil
from pydub import AudioSegment
import os
from trans import transcribe_audio
from Summary import generate_summary
import pyaudio
import wave
import threading
from datetime import datetime
import sys

ALLOWED_EXTENSIONS = ['.wav', '.m4a', '.mp3']

chunk = 1024  # 记录声音的样本区块大小
sample_format = pyaudio.paInt16  # 樣本格式
channels = 2  # 声道数量
fs = 44100  # 取样频率
seconds = 5  # 录音秒数
frames = []
run = False
name = ''
ok = False


def recording():
    global run, name, ok, frames
    while True:
        event.wait()  # 等待事件被触发
        event.clear()  # 触发后将事件回归原本状态
        run = True  # 设置 run 为 True 表示开始录音
        print('开始录音...')
        p = pyaudio.PyAudio()  # 建立 pyaudio 对象
        stream = p.open(format=sample_format, channels=channels,
                        rate=fs, frames_per_buffer=chunk, input=True)
        frames = []
        while run:
            data = stream.read(chunk)
            frames.append(data)  # 将声音记录到列表中
        print('停止录音')
        stream.stop_stream()  # 停止录音
        stream.close()  # 关闭串流
        p.terminate()
        event2.wait()  # 等待事件被触发
        event2.clear()  # 触发后将事件回归原本状态
        # 如果存档按下确定，表示要保存
        if ok:
            current_datetime = datetime.now().strftime("%Y%m%d_%H%M%S")
            file_name = f"會議記錄RC_{current_datetime}.wav"
            folder_name = "Final"
            if not os.path.exists(folder_name):
                os.makedirs(folder_name)
            file_path = os.path.join(folder_name, file_name)
            wf = wave.open(file_path, 'wb')  # 打开声音记录文件
            wf.setnchannels(channels)  # 设置声道
            wf.setsampwidth(p.get_sample_size(sample_format))  # 设置格式
            wf.setframerate(fs)  # 设置取样频率
            wf.writeframes(b''.join(frames))  # 存档
            wf.close()
        else:
            pass


event = threading.Event()  # 注册录音事件
event2 = threading.Event()  # 注册停止录音事件
record = threading.Thread(target=recording)  # 将录音的部分放入 threading 里执行
record.start()


def start_record_click():
    log_label.config(text="錄音中")
    # 禁用"開始錄音"按钮
    startRC_button.config(state=tk.DISABLED)
    endRC_button.config(state=tk.NORMAL)
    upload_button.config(state=tk.DISABLED)
    event.set()  # 触发录音开始事件


def end_record_click():
    global run, name, ok
    log_label.config(text="錄音結束已存檔")
    startRC_button.config(state=tk.NORMAL)  # 重新启用"開始錄音"按钮
    endRC_button.config(state=tk.DISABLED)
    upload_button.config(state=tk.NORMAL)
    run = False  # 设置 run 为 False 停止录音循环
    ok = True
    event2.set()  # 触发录音停止事件


def convert_to_wav(input_file, output_dir):
    # 讀取上傳的檔案
    audio = AudioSegment.from_file(input_file)

    # 轉換成WAV格式
    output_filename = os.path.basename(input_file)  # 取得檔案名稱
    output_filename = os.path.splitext(output_filename)[
        0] + ".wav"  # 修改副檔名為.wav
    output_path = os.path.join(output_dir, output_filename)  # 組合輸出檔案路徑

    audio.export(output_path, format="wav")

    return output_path


def is_allowed_file(filename):
    ext = os.path.splitext(filename)[1]
    return ext.lower() in ALLOWED_EXTENSIONS


def browse_file():
    filename = filedialog.askopenfilename()
    if filename:
        # 將上傳檔案資料夾的路徑替換為你想要存儲上傳檔案的位置
        upload_dir = 'C:\\users\\user\\Desktop\\112\\20230707-1\\音檔轉文字\\UpLoadFile'
        # 將轉換檔案資料夾的路徑替換為你想要存儲轉換檔案的位置
        converted_dir = 'C:\\users\\user\\Desktop\\112\\20230707-1\\音檔轉文字\\TransFile'

        # 檢查上傳檔案資料夾是否存在，若不存在則建立資料夾
        if not os.path.exists(upload_dir):
            os.makedirs(upload_dir)

        # 檢查轉換檔案資料夾是否存在，若不存在則建立資料夾
        if not os.path.exists(converted_dir):
            os.makedirs(converted_dir)

        # 取得上傳檔案的路徑和檔名
        file_path, file_name = os.path.split(filename)

        # 檢查檔案是否已存在
        if os.path.exists(os.path.join(upload_dir, file_name)):
            log_label.config(text="檔案已存在，請重新上傳")

        elif is_allowed_file(filename):
            # 將上傳的檔案複製到上傳檔案資料夾
            shutil.copy(filename, upload_dir)

            # 將檔案路徑組合為轉換後的檔案的儲存路徑
            output_file = convert_to_wav(filename, converted_dir)

            # # 在 GUI 介面上顯示訊息
            # log_text.delete(1.0, tk.END)  # 清空原有內容
            # log_text.insert(tk.END, f"上傳檔案已成功儲存至：{upload_dir}/{file_name}\n")
            # log_text.insert(tk.END, f"轉換後的檔案已成功儲存至：{output_file}\n")

            # 在 GUI 介面上顯示訊息2
            # log_label.config(text=f"上傳檔案已成功儲存至：{upload_dir}/{file_name}\n"
            #                  f"轉換後的檔案已成功儲存至：{output_file}\n")
            log_label.config(text=f"上傳成功")
        else:
            # # 在 GUI 介面上顯示錯誤訊息
            # log_text.delete(1.0, tk.END)  # 清空原有內容
            # log_text.insert(tk.END, "上傳檔案不符合格式，請重新上傳。\n")

            # 在 GUI 介面上顯示錯誤訊息2
            log_label.config(text="上傳檔案不符合格式，請重新上傳")


# def transcribe_button_click():
#     # 轉錄上傳的檔案
#     uploaded_file = 'C:\\users\\user\\Desktop\\112\\20230707-1\\音檔轉文字\\TransFile\\test.wav'
#     transcribe_audio(uploaded_file)

#     log_label.config(text="轉錄完成")
def transcribe_button_click():
    # 選擇上傳的檔案
    file_path = filedialog.askopenfilename()
    if file_path:
        # 轉錄上傳的檔案
        transcribe_audio(file_path)

        log_label.config(text="轉錄完成")
    else:
        log_label.config(text="未選擇檔案")


def exit_program():
    # 删除存放上传文件的文件夹
    upload_dir = 'C:\\users\\user\\Desktop\\112\\20230707-1\\音檔轉文字\\UpLoadFile'
    if os.path.exists(upload_dir):
        shutil.rmtree(upload_dir)

    # 删除存放转换后文件的文件夹
    converted_dir = 'C:\\users\\user\\Desktop\\112\\20230707-1\\音檔轉文字\\TransFile'
    if os.path.exists(converted_dir):
        shutil.rmtree(converted_dir)

    # 删除存放转换后文件的TXT文件夹
    converted_dir = 'C:\\users\\user\\Desktop\\112\\20230707-1\\音檔轉文字\\OutputTxt'
    if os.path.exists(converted_dir):
        shutil.rmtree(converted_dir)

    # 删除存放转换后文件的doc文件夹
    converted_dir = 'C:\\users\\user\\Desktop\\112\\20230707-1\\音檔轉文字\\OutputWord'
    if os.path.exists(converted_dir):
        shutil.rmtree(converted_dir)

    log_label.config(text="程式已關閉")
    # threading.Thread.join()
    # 關閉視窗
    # root.destroy()
    # # 安全退出程式
    # sys.exit()
    # 強制退出
    os._exit(0)

    # # 清空存放上傳檔案的資料夾
    # upload_dir = 'C:\\users\\user\\Desktop\\112\\20230707-1\\音檔轉文字\\UpLoadFile'
    # for file_name in os.listdir(upload_dir):
    #     file_path = os.path.join(upload_dir, file_name)
    #     if os.path.isfile(file_path):
    #         os.remove(file_path)

    # # 清空存放轉換後檔案的資料夾
    # converted_dir = 'C:\\users\\user\\Desktop\\112\\20230707-1\\音檔轉文字\\TransFile'
    # for file_name in os.listdir(converted_dir):
    #     file_path = os.path.join(converted_dir, file_name)
    #     if os.path.isfile(file_path):
    #         os.remove(file_path)

    # log_label.config(text="程式已關閉")
    # root.destroy()


def generate_summary_button_click():
    # 转录上传的文件
    uploaded_file = 'C:\\users\\user\\Desktop\\112\\20230707-1\\音檔轉文字\\OutputWord\\output.docx'
    output_dir = 'C:\\users\\user\\Desktop\\112\\20230707-1\\音檔轉文字'
    generate_summary(uploaded_file, output_dir)

    log_label.config(text="摘要生成并保存成功。")


root = tk.Tk()
root.title("會議記錄檔案上傳")

# 設定視窗大小像素
# root.geometry("200x100")

# 獲取螢幕寬度和高度
screen_width = root.winfo_screenwidth()
screen_height = root.winfo_screenheight()

# 計算視窗的 x 和 y 座標以置中
x = (screen_width // 2) - (200 // 2)
y = (screen_height // 2) - (200 // 2)

# 設定視窗位置
root.geometry(f"400x100+{x}+{y}")

# 建立一個框架
# frame = tk.Frame(root)
# frame.pack()

# # 建立一個 Text 元件用於顯示訊息1
# log_text = tk.Text(root, height=10, width=50, state=tk.DISABLED)
# log_text.insert(tk.END, "歡迎")  # 設置預設內容為「歡迎」
# log_text.pack()

# 建立一個 Label 元件用於顯示訊息
log_label = tk.Label(root, height=1, wraplength=180)
log_label.config(text="歡迎")  # 設置預設內容為「歡迎」
log_label.pack(pady=20)

# 建立一個框架用於放置按鈕
button_frame = tk.Frame(root)
button_frame.pack()

# 建立一個按鈕，當點擊時觸發 browse_file 函式
startRC_button = tk.Button(button_frame, text="開始錄音",
                           command=start_record_click)
startRC_button.pack(side=tk.LEFT)

# 建立一個框架用於留白
spacer_frame = tk.Frame(button_frame, width=10)
spacer_frame.pack(side=tk.LEFT)

# 建立一個按鈕，當點擊時觸發 browse_file 函式
endRC_button = tk.Button(button_frame, text="停止錄音", command=end_record_click)
endRC_button.pack(side=tk.LEFT)

# 建立一個框架用於留白
spacer_frame = tk.Frame(button_frame, width=10)
spacer_frame.pack(side=tk.LEFT)

# 建立一個按鈕，當點擊時觸發 browse_file 函式
upload_button = tk.Button(button_frame, text="上傳檔案", command=browse_file)
upload_button.pack(side=tk.LEFT)

# 建立一個框架用於留白
spacer_frame = tk.Frame(button_frame, width=10)
spacer_frame.pack(side=tk.LEFT)

# 建立一個按鈕，當點擊時觸發 transcribe_button_click 函式
transcribe_button = tk.Button(
    button_frame, text="輸出逐字稿", command=transcribe_button_click)
transcribe_button.pack(side=tk.LEFT)

# 建立一個框架用於留白
spacer_frame2 = tk.Frame(button_frame, width=10)
spacer_frame2.pack(side=tk.LEFT)

# 建立一個按鈕，當點擊時觸發 generate_summary 函式
summary_button = tk.Button(button_frame, text="摘要總結",
                           command=generate_summary_button_click)
summary_button.pack(side=tk.LEFT)

# 建立一個按鈕，當點擊時觸發 exit_program 函式
exit_button = tk.Button(root, text="離開", command=exit_program)
exit_button.pack(side=tk.BOTTOM)

root.mainloop()
