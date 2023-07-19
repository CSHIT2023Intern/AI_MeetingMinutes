# 在colab可以執行 本機還沒有串接測試
import os
import azure.cognitiveservices.speech as speechsdk
from docx import Document

SPEECH_KEY = "c9d3e6d440214af3bc175d4c31809a44"
SPEECH_REGION = "eastasia"


def transcribe_audio(input_file, Wordfile):
    # KEY
    speech_config = speechsdk.SpeechConfig(
        subscription=SPEECH_KEY, region=SPEECH_REGION)
    # 語言
    speech_config.speech_recognition_language = "zh-TW"
    # 音檔
    audio_config = speechsdk.audio.AudioConfig(filename=input_file)

    speech_recognizer = speechsdk.SpeechRecognizer(
        speech_config=speech_config, audio_config=audio_config)

    speech_recognition_result = speech_recognizer.recognize_once_async().get()
    recognized_text = speech_recognition_result.text

    # 儲存 Word 文件至 逐字稿 資料夾
    doc = Document()
    doc.add_paragraph(recognized_text)
    doc.save(Wordfile)
