import os
import docx
import speech_recognition as sr

output_docx_path = ""


def transcribe_audio(input_file):
    global output_docx_path

    # 建立 Recognizer 物件
    recognizer = sr.Recognizer()
    # 開啟音檔
    with sr.AudioFile(input_file) as source:

        # 讀取音訊檔案數據
        audio_data = recognizer.record(source)

        # 使用 Google 語音辨識進行轉換
        transcription = recognizer.recognize_google(
            audio_data, language="zh-TW")

        output_word_folder = "OutputWord"
        # 檢查轉換檔案資料夾是否存在，若不存在則建立資料夾
        if not os.path.exists(output_word_folder):
            os.makedirs(output_word_folder)

        # 儲存 Word 文件至 逐字稿 資料夾
        output_docx_path = os.path.join(output_word_folder, "output.docx")
        doc = docx.Document()
        doc.add_paragraph(transcription)
        doc.save(output_docx_path)
