import os
import azure.cognitiveservices.speech as speechsdk
from datetime import datetime
import time

# 存檔路徑
OutPutPath = 'txt'

# 將日期時間變成字串
todayStr = datetime.now().strftime("%Y-%m-%d__%H-%M-%S")

SPEECH_KEY = "c9d3e6d440214af3bc175d4c31809a44"
SPEECH_REGION = "eastasia"


def trans():

    # KEY
    speech_config = speechsdk.SpeechConfig(
        subscription=SPEECH_KEY, region=SPEECH_REGION)
    # 語言
    speech_config.speech_recognition_language = "zh-TW"

    speech_recognizer = speechsdk.SpeechRecognizer(
        speech_config=speech_config)

    done = False

    def stop_cb(evt):
        speech_recognizer.stop_continuous_recognition()
        nonlocal done
        done = True

    results = []

    def handle_final_result(evt):
        results.append(evt.result.text)  # 讀出evt text裡的字串

    speech_recognizer.recognized.connect(handle_final_result)

    speech_recognizer.session_stopped.connect(stop_cb)
    speech_recognizer.canceled.connect(stop_cb)

    speech_recognizer.start_continuous_recognition()
    while not done:
        time.sleep(.5)

    txt = "".join(results)

    # 判斷存檔資料夾是否已經存在沒有就建立
    if not os.path.exists(OutPutPath):
        os.makedirs(OutPutPath)

    # 輸出語音轉文字資料到資料夾中
    ffname = os.path.join(OutPutPath, '語音轉文字_'+todayStr+".txt")
    with open(ffname, "w", encoding='UTF-8') as file:
        file.write(txt)

    return txt
