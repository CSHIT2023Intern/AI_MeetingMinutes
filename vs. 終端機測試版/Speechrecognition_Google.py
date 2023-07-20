# 錄音轉文字 利用Speech Recognition recognize_google

import speech_recognition as speechrec
import os
from pydub import AudioSegment
import requests
# import pyperclip
from gensim.summarization import summarize

# 設置chatgpt api 端點
# url = "https://api.chatgpt.com/chatgpt/summarization"

sperec = speechrec.Recognizer()

#假設file的檔案
First_file = "withoutyou.mp3"

# 如果檔案類型為mp3的，就要轉換類型為wav
if First_file.endswith(".mp3"):
    
    # 輸入mp3的路徑
    mp3_file_path = 'D:\\中山醫實習\\專案\\Practice\\video\\'+First_file

    # 輸出wav檔案路徑
    wav_file_path = 'D:\\中山醫實習\\專案\\Practice\\txt\\'+"withoutyou.wav"


    def mp3_to_wav(mp3_file, wav_file):
    
      # 讀取mp3檔案
      audio = AudioSegment.from_mp3(mp3_file)

      # 將mp3檔案轉換為wav格式
      audio.export(wav_file, format='wav')

 # 呼叫函式執行轉換
mp3_to_wav(mp3_file_path, wav_file_path)

file = speechrec.AudioFile('D:\\中山醫實習\\專案\\Practice\\video\\withoutyou.wav')
with file as wav_file:
    audio = sperec.record(wav_file)

#這裡的資料型別為dict，因此先把他取出放到變數text 利用google api 讀取檔案
text = sperec.recognize_google(audio, show_all=False, language="zh-Tw") #show_all從True改成False可以讓顯示只顯示一段，不需要一直重複
print((text))

#將text從list/dict變成string 在放到txt變數中
txt = ' '.join(text)

#將檔案放入的路徑為
ffpath = "D:\\中山醫實習\\專案\\Practice\\txt\\"

#判斷是否已經存在了
if not os.path.exists(ffpath):
   os.makedirs(ffpath)
ffname = os.path.join(ffpath,"output.txt") #將輸出的資料命名為 "output.txt"

with open(ffname,"w",encoding='utf-8') as file: #寫入檔案到資料夾中
   file.write(txt)

print("已儲存") #成功就會print

# summary = summarize(txt)
# # print(summary) 

# # 將摘要文字輸出到檔案
# with open('summary.txt', 'w') as f:
#     f.write(summary)


# # 構建payload
# payload = {"text":text}
# headers={"Content-Type":"application/json"}

# # 發送POST請求
# response = requests.post(url,json=payload,headers=headers)

# str = response.text
# print(str)
