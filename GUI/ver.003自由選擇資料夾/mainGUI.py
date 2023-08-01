import os
import sys
import shutil
import threading


from pydub import AudioSegment
import pyaudio
import wave

import tkinter as tk
from tkinter import filedialog
from tkinter import messagebox
from datetime import datetime

import trans
from upload import is_allowed_file, convert_to_wav
from summary import generate_summary
from out import clear

# 上傳檔案按鈕所存放上傳檔案與轉換WAV檔案的資料夾路徑
upload_dir = ""
converted_dir = ""

output_file = ""

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
    upload_button.config(state=tk.DISABLED)
    endRC_button.config(state=tk.NORMAL)
    # 觸發錄音開始事件
    event.set()


# 停止錄音按鈕
def end_record_click():
    global run, name, ok
    log_label.config(text="錄音結束，已存檔")
    # 啟用開始錄音按钮
    startRC_button.config(state=tk.NORMAL)
    upload_button.config(state=tk.NORMAL)
    endRC_button.config(state=tk.DISABLED)
    # run = False是停止錄音循環
    run = False
    ok = True
    # 觸發錄音停止事件
    event2.set()


# 上傳檔案按鈕
def upload_file():
    global upload_dir, converted_dir, output_file
    log_label.config(text="請選擇上傳文件")
    upload_file_path = filedialog.askopenfilename()
    if upload_file_path:
        # 取得上傳檔案所在資料夾路徑
        uploaddir = os.path.dirname(upload_file_path)

        # 取得上傳檔案的檔名
        file_name = os.path.basename(upload_file_path)

        # 檢查檔案是否已存在
        if os.path.exists(os.path.join(upload_dir, file_name)):
            answer = messagebox.askyesno("覆蓋確認", "檔案已存在，是否覆蓋？")
            if answer:
                # 若使用者選擇覆蓋，則刪除原檔案並複製新檔案
                os.remove(os.path.join(upload_dir, file_name))
                shutil.copy(upload_file_path, upload_dir)

                # 將儲存上傳檔案的資料夾名稱設為 "UpLoadFile"，並建立資料夾
                upload_dir = os.path.join(uploaddir, "UpLoadFile")
                # 檢查上傳檔案資料夾是否存在，若不存在則建立資料夾
                if not os.path.exists(upload_dir):
                    os.makedirs(upload_dir)

                # 將儲存轉換檔案的資料夾名稱設為 "TransFile"，並建立資料夾
                converted_dir = os.path.join(uploaddir, "TransFile")
                # 檢查轉換檔案資料夾是否存在，若不存在則建立資料夾
                if not os.path.exists(converted_dir):
                    os.makedirs(converted_dir)

                output_file = convert_to_wav(upload_file_path, converted_dir)
                log_label.config(text="上傳成功（已覆蓋）")
            else:
                log_label.config(text="上傳取消")
        elif is_allowed_file(upload_file_path):

            # 將儲存上傳檔案的資料夾名稱設為 "UpLoadFile"，並建立資料夾
            upload_dir = os.path.join(uploaddir, "UpLoadFile")
            # 檢查上傳檔案資料夾是否存在，若不存在則建立資料夾
            if not os.path.exists(upload_dir):
                os.makedirs(upload_dir)

            # 將儲存轉換檔案的資料夾名稱設為 "TransFile"，並建立資料夾
            converted_dir = os.path.join(uploaddir, "TransFile")
            # 檢查轉換檔案資料夾是否存在，若不存在則建立資料夾
            if not os.path.exists(converted_dir):
                os.makedirs(converted_dir)

            # 將上傳的檔案複製到上傳檔案資料夾
            shutil.copy(upload_file_path, upload_dir)
            # 將檔案路徑組合為轉換後的檔案的儲存路徑
            output_file = convert_to_wav(upload_file_path, converted_dir)
            log_label.config(text="上傳成功")
        else:
            log_label.config(text="上傳檔案不符合格式，請重新上傳")
    else:
        log_label.config(text="未選擇檔案")


# 輸出逐字稿按鈕
def transcribe_button_click():
    global output_file, OutputWordFile
    # 選擇 OutputWord 資料夾位置
    log_label.config(text="請選擇逐字稿資料夾位置")
    OutputWordFile = filedialog.askdirectory()
    # if有問題前兩個其中一個執行過第三個就會掛掉
    # 有錄音
    if record_folder:
        log_label.config(text="請選擇需轉換錄音")
        selected_file_path = filedialog.askopenfilename(
            initialdir=record_folder)
        log_label.config(text="正在轉換中...")
        root.update()  # 不立刻更新上面那行text來不及顯示
        trans.transcribe_audio(selected_file_path, OutputWordFile)
        log_label.config(text="語音轉文字完成!")
    # 上傳需轉檔檔案
    elif os.path.exists(output_file):
        # 執行轉錄
        log_label.config(text="正在轉換中...")
        root.update()  # 不立刻更新上面那行text來不及顯示
        trans.transcribe_audio(output_file, OutputWordFile)
        log_label.config(text="語音轉文字完成!")
    # WAV檔案
    else:
        # 選擇上傳的檔案
        log_label.config(text="請選擇需轉換文件")
        selected_file_path = filedialog.askopenfilename()
        if selected_file_path:
            log_label.config(text="正在轉換中...")
            root.update()  # 不立刻更新上面那行text來不及顯示
            # 轉錄上傳的檔案
            trans.transcribe_audio(selected_file_path, OutputWordFile)
            log_label.config(text="語音轉文字完成!")
        else:
            log_label.config(text="未選擇檔案")


# 摘要總結按鈕
def generate_summary_button_click():
    # 讀取逐字稿路徑
    OutputWord = trans.output_docx_path
    # 選擇輸出位置
    log_label.config(text="請選擇輸出資料夾")
    output_dir = filedialog.askdirectory()
    if output_dir:
        log_label.config(text="進行中.. ")
        root.update()  # 不立刻更新上面那行text來不及顯示
        generate_summary(OutputWord, output_dir)
        log_label.config(text="摘要成功")
    else:
        log_label.config(text="未選擇輸出資料夾")


# 離開按鈕
def exit_program():
    clear(upload_dir, converted_dir)
    os._exit(0)


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
                     text="歡迎\n \n 如果是wav檔名可以直接進行逐字稿轉換",
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


# 上傳檔案按鈕
upload_button = tk.Button(button_frame1, text="上傳檔案", command=upload_file)
upload_button.pack(side=tk.LEFT, padx=5, pady=5)


# 輸出逐字稿按鈕
transcribe_button = tk.Button(
    button_frame2, text="輸出逐字稿", command=transcribe_button_click)
transcribe_button.pack(side=tk.LEFT, padx=5)


# 摘要總結按鈕
summary_button = tk.Button(button_frame2, text="摘要總結",
                           command=generate_summary_button_click)
summary_button.pack(side=tk.LEFT, padx=5)

# 摘要總結按鈕
exit_button = tk.Button(root, text="離開", command=exit_program, bd=5)
exit_button.pack(side=tk.RIGHT)

root.mainloop()
