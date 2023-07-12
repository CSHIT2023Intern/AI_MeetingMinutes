# 錄音轉文字 利用Speech Recognition recognize_whisper
import speech_recognition as speechrec
import os
from pydub import AudioSegment

sperec = speechrec.Recognizer()

#假設file的檔案
# First_file = form['filename']
First_file = "S.mp3"
First_file.split('')

# 如果檔案類型為mp3的，就要轉換類型為wav
if First_file.endswith(".mp3"):
    
    # 輸入mp3的路徑
    mp3_file_path = 'D:\\中山醫實習\\專案\\Practice\\video\\'+First_file

    # 輸出wav檔案路徑
    wav_file_path = 'D:\\中山醫實習\\專案\\Practice\\txt\\'+"S.wav"


    def mp3_to_wav(mp3_file, wav_file):
    
      # 讀取mp3檔案
      audio = AudioSegment.from_mp3(mp3_file)

      # 將mp3檔案轉換為wav格式
      audio.export(wav_file, format='wav')

 # 呼叫函式執行轉換
mp3_to_wav(mp3_file_path, wav_file_path)

file = speechrec.AudioFile('D:\\中山醫實習\\專案\\Practice\\video\\S.wav')
with file as wav_file:
    audio = sperec.record(wav_file)

#這裡的資料型別為dict，因此先把他取出放到變數text 利用google api 讀取檔案
text = sperec.recognize_whisper(
    audio_data=audio,
    model="medium",
    # None = None
    # api_key= "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"
   # language="chinese"
   language="zh"
)  #show_all從True改成False可以讓顯示只顯示一段，不需要一直重複
print((text))

#將text從list/dict變成string 在放到txt變數中
txt = ' '.join(text)

#將檔案放入的路徑為
ffpath = "D:\\中山醫實習\\專案\\Practice\\txt\\"

#判斷是否已經存在了
if not os.path.exists(ffpath):
   os.makedirs(ffpath)
ffname = os.path.join(ffpath,"output.txt") #將輸出的資料命名為 "output.txt"

with open(ffname,"w") as file: #寫入檔案到資料夾中
   file.write(txt)

print("已儲存") #成功就會print
