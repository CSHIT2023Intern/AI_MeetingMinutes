from flask import Flask,request,render_template,jsonify
import os
import openai
from pydub import AudioSegment
import speech_recognition as speechrec
from gensim.summarization import summarize
from datetime import date
import time
import pyaudio

# global wav_file

app = Flask(__name__)

@app.route('/')
def Hello():
    # return '<html><body><h1>Welcome</h1></body></html>'
    return render_template('home.html')


# 範例
@app.route('/para/<user>')
def index(user):
    return render_template('index.html',user_template=user)

@app.route('/upload', methods=['POST','GET']) #錄音檔語音摘要
# 範例參考的寫法
# def upload():
#     if request.method =='POST':
#         if request.values['send']=='提交':
#             return render_template('index.html',text=request.values['data'])       
#     return render_template('index.html',text="")

def upload():
    if request.method =='POST':
        if request.values['send']=='提交':
            data1=request.values['data']
            # 錄音轉文字 利用Speech Recognition recognize_whisper
            # 設置open api 端點
            openai.api_type = "azure"
            openai.api_base = "https://cshitinternopenai.openai.azure.com/"
            openai.api_version = "2023-03-15-preview"
            openai.api_key = "0be4adcd512d4b09b7e44d50325f4bf9"

            sperec = speechrec.Recognizer()

            #假設file的檔案
            # First_file = form['filename']
            First_file = data1
            First_Name= First_file.split(".")[0]

            file_path =  os.path.dirname(First_file)
            file_name = os.path.basename(First_file)


            # 如果檔案類型為mp3的，就要轉換類型為wav
            if First_file.endswith(".mp3"):
                
                # 輸入mp3的路徑
                mp3_file_path = 'D:\\中山醫實習\\專案\\Practice\\video\\'+First_file
                # mp3_file_path = os.path.join(file_path,file_name)


                # 輸出wav檔案路徑
                wav_file_path = 'D:\\中山醫實習\\專案\\Practice\\video\\'+First_Name+'.wav'
                # wav_file_path = +First_Name+'.wav'

                def mp3_to_wav(mp3_file, wav_file):
                    
                    # 讀取mp3檔案
                    audio = AudioSegment.from_mp3(mp3_file)

                    # 將mp3檔案轉換為wav格式
                    audio.export(wav_file, format='wav')

                 # 呼叫函式執行轉換
                mp3_to_wav(mp3_file_path, wav_file_path)

            file = speechrec.AudioFile('D:\\中山醫實習\\專案\\Practice\\video\\'+First_Name+'.wav')
            with file as wav_file:
                audio = sperec.record(wav_file)

            #這裡的資料型別為dict，因此先把他取出放到變數text 利用google api 讀取檔案
            text = sperec.recognize_whisper(
                audio_data=audio,
                model="medium",
                language="zh"
            )  #show_all從True改成False可以讓顯示只顯示一段，不需要一直重複
            print((text))

            #將text從list/dict變成string 在放到txt變數中
            txt = ' '.join(text)

            #將檔案放入的路徑為
            ffpath = "D:\\中山醫實習\\專案\\Practice\\txt\\"

            # 讀取今天的日期
            today = date.today()
            # 將日期變成字串
            todayStr = today.strftime("%Y-%m-%d")

            #判斷是否已經存在了
            if not os.path.exists(ffpath):
                os.makedirs(ffpath)
            ffname = os.path.join(ffpath, '文字檔'+First_Name+todayStr+".txt") #將輸出的資料命名為 "output.txt"

            with open(ffname,"w",encoding='UTF-8') as file: #寫入檔案到資料夾中
                file.write(txt)

            print("已儲存") #成功就會print

            response = openai.ChatCompletion.create(
            engine="CSHITIntern", # engine = "deployment_name".
            messages=[
                {"role": "system", "content": "我是一個秘書要做會議紀錄"}, #人物設定
                {"role": "assistant", "content": txt},#文章
                {"role": "user", "content": "可以幫我們統整重點" }#想要叫GPT做的事
                ])

            print(response)
            print(response['choices'][0]['message']['content'])

            ffname = os.path.join(ffpath,'摘要檔'+First_Name+todayStr+'.txt') #將輸出的資料命名
            with open(ffname,"w",encoding='UTF-8') as file: #寫入檔案到資料夾中
                file.write(response['choices'][0]['message']['content'])
        # return render_template('index.html',text=response['choices'][0]['message']['content'])
        return jsonify({'answer' : response['choices'][0]['message']['content']}) # 回傳json格式
    return render_template('index.html',text="")

@app.route('/micro',methods=['POST','GET']) #麥克風語音摘要
def micro():
    if request.method =='POST':
        if request.values['send']=='開始錄音':
            # 即時錄音轉文字
            # 新增物件
            r = speechrec.Recognizer()
            r.interimResults=True
            # 使用麥克風錄音
            with speechrec.Microphone(sample_rate=16000) as source:
                print("開始錄音")
                start = "開始錄音"
                # 調整噪音或環境的聲音
                r.adjust_for_ambient_noise(source)
                audio = r.listen(source)
            try:
                text = r.recognize_google(audio, language="zh-Tw")
            except speechrec.UnknownValueError:
                text = "無法翻譯"
            txt = text
            openai.api_type = "azure"
            openai.api_base = "https://cshitinternopenai.openai.azure.com/"
            openai.api_version = "2023-03-15-preview"
            openai.api_key = "0be4adcd512d4b09b7e44d50325f4bf9"


            # 讀取今天的日期
            today = date.today()
            # 將日期變成字串
            todaySpeech = today.strftime("%Y-%m-%d")

            response = openai.ChatCompletion.create(
            engine="CSHITIntern", # engine = "deployment_name".
            messages=[
                {"role": "system", "content": "我是一個秘書要做會議紀錄"},
                {"role": "assistant", "content": text},
                {"role": "user", "content": "可以幫我們統整重點" }
                ])

            #將檔案放入的路徑為
            ffpath = "D:\\中山醫實習\\專案\\Practice\\"

            #判斷是否已經存在了
            if not os.path.exists(ffpath):
                os.makedirs(ffpath)
            ffname = os.path.join(ffpath,'文字檔'+'speech1'+todaySpeech+'.txt') #將輸出的資料命名為 "output.txt"

            with open(ffname,"w",encoding='utf-8') as file: #寫入檔案到資料夾中
                file.write(response['choices'][0]['message']['content'])

            print(text)
            # return render_template('micro.html',text=response['choices'][0]['message']['content']) #用在第二種方法 回傳text值，他是對應前端的{{text}}屬性
            return jsonify({'answer' : response['choices'][0]['message']['content']}) #用在第一種方法 回傳json格式
    return render_template('micro.html',text="")

if __name__ == '__main__':
	app.run(host='0.0.0.0',port='4000',debug=True)
