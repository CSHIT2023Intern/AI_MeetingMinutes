# 語音轉文字speech_recognition.recognize_whisper
import os
import openai
import speech_recognition
from datetime import datetime

# 存檔路徑
ffpath = 'txt'

# 將日期時間變成字串
todayStr = datetime.now().strftime("%Y-%m-%d__%H-%M-%S")


def summary(selected_file):

    # 建立 Recognizer 物件
    SpeechRecognition = speech_recognition.Recognizer()

    # 讀取音訊檔案數據
    with speech_recognition.AudioFile(selected_file) as source:
        audio = SpeechRecognition.record(source)

    # 這裡的資料型別為dict，因此先把他取出放到變數text
    text = SpeechRecognition.recognize_whisper(
        audio,
        model="tiny",
        language="zh"
    )

    # 將text從list/dict變成string 在放到txt變數中
    txt = ' '.join(text)

    # 判斷存檔資料夾是否已經存在沒有就建立
    if not os.path.exists(ffpath):
        os.makedirs(ffpath)

    # 輸出語音轉文字資料到資料夾中
    ffname = os.path.join(ffpath, '語音轉文字_'+todayStr+".txt")
    with open(ffname, "w", encoding='UTF-8') as file:
        file.write(txt)

    # API設定
    openai.api_type = "azure"
    openai.api_base = "https://cshitinternopenai.openai.azure.com/"
    openai.api_version = "2023-03-15-preview"
    openai.api_key = "0be4adcd512d4b09b7e44d50325f4bf9"

    response = openai.ChatCompletion.create(
        engine="CSHITIntern",
        messages=[
            {"role": "system", "content": "我是一個秘書要做會議紀錄"},  # 人物設定
            {"role": "assistant", "content": txt},  # 文章
            {"role": "user", "content": "可以幫我們統整重點"}  # 想要叫GPT做的事
        ])

    # 輸出摘要資料到資料夾中
    ffname = os.path.join(
        ffpath, '會議摘要_'+todayStr+'.txt')
    with open(ffname, "w", encoding='UTF-8') as file:  # 寫入檔案到資料夾中
        file.write(response['choices'][0]['message']['content'])

    # 回傳到主程式
    Response = response['choices'][0]['message']['content']
    return (Response)
