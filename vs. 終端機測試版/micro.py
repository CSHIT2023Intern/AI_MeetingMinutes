# 即時錄音轉文字
import speech_recognition as spec
import time
import os
import pyaudio

def Voice_to_text(rate=16000):
    # 新增物件
    r = spec.Recognizer()
    r.interimResults=True
    # 使用麥克風錄音
    with spec.Microphone(sample_rate=rate) as source:
        print("開始錄音")
        # 調整噪音或環境的聲音
        r.adjust_for_ambient_noise(source)
        audio = r.listen(source)
    try:
        text = r.recognize_google(audio, language="zh-Tw")
    except spec.UnknownValueError:
        text = "無法翻譯"
    except spec.UnknownValueError as e:
        text = "無法翻譯{0}".format(e)
    return text
text = Voice_to_text()

#將檔案放入的路徑為
ffpath = "D:\\中山醫實習\\專案\\Practice\\"

#判斷是否已經存在了
if not os.path.exists(ffpath):
   os.makedirs(ffpath)
ffname = os.path.join(ffpath,"speech1.txt") #將輸出的資料命名為 "output.txt"

with open(ffname,"w",encoding='utf-8') as file: #寫入檔案到資料夾中
   file.write(text)

print(text)