import os
import sys
import shutil
import threading
import tkinter as tk
from tkinter import filedialog
from datetime import datetime


from pydub import AudioSegment
import pyaudio
import wave


import trans
import transs
# import transss
from summary import generate_summary

record_folder = ""
record_file_name = ""

OutputWordFile = ""

# 錄音相關
chunk = 1024    # 記錄聲音的樣本區塊大小
sample_format = pyaudio.paInt16  # 樣本格式
channels = 2    # 聲道數量
fs = 48000  # 取樣頻率
frames = []  # 建立聲音串列
run = False
name = ''
ok = False


def recording():
    global run, name, ok, frames, record_folder, record_file_name
    while True:
        event.wait()  # 等待事件觸發
        event.clear()  # 初始化事件
        run = True  # run = True表示開始錄音
        p = pyaudio.PyAudio()  # 建立 pyaudio
        stream = p.open(format=sample_format, channels=channels,
                        rate=fs, frames_per_buffer=chunk, input=True)
        frames = []
        while run:
            data = stream.read(chunk)
            frames.append(data)  # 把聲音紀錄到列表中
        stream.stop_stream()  # 停止錄音
        stream.close()  # 關閉串流
        p.terminate()
        event2.wait()  # 等待事件觸發
        event2.clear()  # 初始化事件
        if ok:
            current_datetime = datetime.now().strftime("%Y%m%d_%H%M%S")
            file_name = f"會議記錄RC_{current_datetime}.wav"
            # 讓使用者選擇存檔的資料夾
            record_folder = filedialog.askdirectory()
            if record_folder:
                record_file_name = file_name  # 將檔案名稱存儲為全域變數
                file_path = os.path.join(record_folder, record_file_name)
                wf = wave.open(file_path, 'wb')  # 打開聲音紀錄文件
                wf.setnchannels(channels)  # 設定聲道
                wf.setsampwidth(p.get_sample_size(sample_format))  # 設定格式
                wf.setframerate(fs)  # 設定取樣頻率
                wf.writeframes(b''.join(frames))  # 存檔
                wf.close()
        else:
            pass


event = threading.Event()  # 設定開始錄音事件
event2 = threading.Event()  # 設定停止錄音事件
record = threading.Thread(target=recording)  # 將錄音的部分放到 threading 裡執行
record.start()


# 開始錄音按鈕
def start_record_click():
    log_label.config(text="錄音中")
    # 禁用開始錄音、上傳檔案按钮
    startRC_button.config(state=tk.DISABLED)
    endRC_button.config(state=tk.NORMAL)
    # 觸發錄音開始事件
    event.set()


# 停止錄音按鈕
def end_record_click():
    global run, name, ok
    log_label.config(text="錄音結束，已存檔")
    # 啟用開始錄音按钮
    startRC_button.config(state=tk.NORMAL)
    endRC_button.config(state=tk.DISABLED)
    # run = False是停止錄音循環
    run = False
    ok = True
    # 觸發錄音停止事件
    event2.set()


# 摘要總結按鈕
def generate_summary_button_click():
    global OutputWordFile
    # 選擇 OutputWord 資料夾位置
    log_label.config(text="請選擇逐字稿資料夾位置")
    OutputWordFile = filedialog.askdirectory(title="選擇逐字稿資料夾位置")
    # if有問題前兩個其中一個執行過第三個就會掛掉
    # 有錄音
    if record_folder:
        log_label.config(text="請選擇需轉換錄音")
        selected_file_path = filedialog.askopenfilename(
            title="請選擇需轉換錄音")
        log_label.config(text="正在轉換中...")
        root.update()  # 不立刻更新上面那行text來不及顯示
        # Whisper
        # trans.transcribe_audio(selected_file_path, OutputWordFile)

        # speech_recognition_google
        transs.transcribe_audio(selected_file_path, OutputWordFile)

        # speech_recognition_whisper
        # transs1.transcribe_audio(selected_file_path, OutputWordFile)

        # azure speech_recognition
        # transss.transcribe_audio(selected_file_path, OutputWordFile)
        log_label.config(text="語音轉文字完成!")
    # 音檔
    else:
        # 選擇音檔
        log_label.config(text="請選擇需轉換文件")
        selected_file_path = filedialog.askopenfilename(title="請選擇需轉換文件")
        if selected_file_path:
            log_label.config(text="正在轉換中...")
            root.update()  # 不立刻更新上面那行text來不及顯示
            # 轉錄音檔
            # Whisper
            # trans.transcribe_audio(selected_file_path, OutputWordFile)

            # speech_recognition_google
            transs.transcribe_audio(selected_file_path, OutputWordFile)

            # speech_recognition_whisper
            # transs1.transcribe_audio(selected_file_path, OutputWordFile)

            # azure speech_recognition
            # transss.transcribe_audio(selected_file_path, OutputWordFile)

            log_label.config(text="語音轉文字完成!")
        else:
            log_label.config(text="未選擇檔案")

    # 讀取Whisper逐字稿路徑
    # OutputWord = trans.output_docx_path

    # 讀取speech_recognition_google逐字稿路徑
    OutputWord = transs.output_docx_path

    # 讀取speech_recognition_whisper逐字稿路徑
    # OutputWord = transs1.output_docx_path

    # 讀取azure speech_recognition逐字稿路徑
    # OutputWord = transss.output_docx_path

    # 選擇輸出位置
    log_label.config(text="請選擇摘要輸出資料夾")
    output_dir = filedialog.askdirectory(title="請選擇摘要輸出資料夾")
    if output_dir:
        log_label.config(text="摘要進行中.. ")
        root.update()  # 不立刻更新上面那行text來不及顯示
        generate_summary(OutputWord, output_dir)
        log_label.config(text="摘要成功")
    else:
        log_label.config(text="未選擇輸出資料夾")


# 宣告視窗和標題
root = tk.Tk()
root.title("會議記錄檔案上傳")

# 讀取螢幕寬度和高度
screen_width = root.winfo_screenwidth()
screen_height = root.winfo_screenheight()

# 宣告視窗寬高與位置
tkW, tkH, x, y = 300, 150, 0, 0

# 計算視窗的 x 和 y 座標以置中
x = (screen_width // 2) - (200 // 2)
y = (screen_height // 2) - (200 // 2)

# 設定視窗大小和位置
root.geometry(f"{tkW}x{tkH}+{x}+{y}")

# 訊息框
label_frame = tk.Frame(root)
label_frame.pack(padx=1, pady=1)

# 訊息
log_label = tk.Label(root, height=4, width=180,
                     text="歡迎\n \n 如果是wav&m4a可以直接進行逐字稿轉換",
                     font=('Arial', 10, 'bold'))
log_label.pack()

# 按鈕框
button_frame1 = tk.Frame(root)
button_frame1.pack()

button_frame2 = tk.Frame(root)
button_frame2.pack()

# 開始錄音按鈕
startRC_button = tk.Button(button_frame1, text="開始錄音",
                           command=start_record_click,)
startRC_button.pack(side=tk.LEFT, padx=5, pady=5)


# 停止錄音按鈕
endRC_button = tk.Button(button_frame1, text="停止錄音", command=end_record_click)
endRC_button.pack(side=tk.LEFT, padx=5, pady=5)


# 輸出逐字稿&摘要總結按鈕
transsummary_button = tk.Button(
    button_frame2, text="摘要總結", command=generate_summary_button_click)
transsummary_button.pack(side=tk.LEFT, padx=5)


def on_close():
    os._exit(0)


root.protocol("WM_DELETE_WINDOW", on_close)

root.mainloop()
