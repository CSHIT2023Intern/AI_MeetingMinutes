import os
import speech_recognition
from datetime import datetime

# 存檔路徑
OutPutPath = 'txt'

# 將日期時間變成字串
todayStr = datetime.now().strftime("%Y-%m-%d__%H-%M-%S")


def trans(selected_file):
    # 建立 Recognizer 物件
    SpeechRecognition = speech_recognition.Recognizer()

    # 讀取音訊檔案數據
    with speech_recognition.AudioFile(selected_file) as source:
        audio = SpeechRecognition.record(source)
    try:
       # 這裡的資料型別為dict，因此先把他取出放到變數text
        text = SpeechRecognition.recognize_whisper(
            audio,
            model="tiny",
            language="zh"
        )
    except speech_recognition.UnknownValueError:
        text = "無法翻譯"
    # 將text從list/dict變成string 在放到txt變數中
    txt = ' '.join(text)

    # 判斷存檔資料夾是否已經存在沒有就建立
    if not os.path.exists(OutPutPath):
        os.makedirs(OutPutPath)

    # 輸出語音轉文字資料到資料夾中
    ffname = os.path.join(OutPutPath, '語音轉文字_'+todayStr+".txt")
    with open(ffname, "w", encoding='UTF-8') as file:
        file.write(txt)

    return txt
