# 可能會有中間斷氣太久而中斷
import os
import azure.cognitiveservices.speech as speechsdk
from datetime import datetime

# 存檔路徑
OutPutPath = 'txt'

# 將日期時間變成字串
todayStr = datetime.now().strftime("%Y-%m-%d__%H-%M-%S")

SPEECH_KEY = "c9d3e6d440214af3bc175d4c31809a44"
SPEECH_REGION = "eastasia"


def trans(selected_file):
    # KEY
    speech_config = speechsdk.SpeechConfig(
        subscription=SPEECH_KEY, region=SPEECH_REGION)
    # 語言
    speech_config.speech_recognition_language = "zh-TW"
    # 音檔
    audio_config = speechsdk.audio.AudioConfig(filename=selected_file)

    speech_recognizer = speechsdk.SpeechRecognizer(
        speech_config=speech_config, audio_config=audio_config)

    speech_recognition_result = speech_recognizer.recognize_once_async().get()
    txt = speech_recognition_result.text

    # 判斷存檔資料夾是否已經存在沒有就建立
    if not os.path.exists(OutPutPath):
        os.makedirs(OutPutPath)

    # 輸出語音轉文字資料到資料夾中
    ffname = os.path.join(OutPutPath, '語音轉文字_'+todayStr+".txt")
    with open(ffname, "w", encoding='UTF-8') as file:
        file.write(txt)

    return txt
