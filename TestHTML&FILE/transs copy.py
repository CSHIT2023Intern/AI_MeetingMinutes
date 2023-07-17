import speech_recognition as sr


def transcribe_audio(audio_file):
    # 建立 Recognizer 物件
    recognizer = sr.Recognizer()
    # 開啟音檔
    with sr.AudioFile(audio_file) as source:
        # 讀取音訊檔案數據
        audio_data = recognizer.record(source)

        # 輸出轉換結果
        # print(recognizer.recognize_google(audio_data, show_all=True, language="zh-TW"))
        # 另一種輸出
        # # 使用 Google 語音辨識進行轉換（使用繁體中文）
        # text = recognizer.recognize_google(audio_data, language="zh-TW")
        # # 輸出轉換結果
        # print("音檔轉換結果: {}".format(text))


# 執行音檔轉文字
audio_file_path = r"C:\users\user\Desktop\112\20230707-1\音檔轉文字\Source\test.wav"
transcribe_audio(audio_file_path)
