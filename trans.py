import os

import docx

import whisper

output_docx_path = ""


def transcribe_audio(input_file, Wordfile):
    global output_docx_path
    # 載入模型
    # large會標點符號但很慢 #medium最少要用這個
    model = whisper.load_model("tiny")

    # 語音轉文字
    result = model.transcribe(input_file)
    transcription = result["text"]

    # 將結果存儲為 Word 文件，每 20 個字元換行
    formatted_transcription = "\n".join(
        [transcription[i:i+20] for i in range(0, len(transcription), 20)])

    output_word_folder = os.path.join(Wordfile, "逐字稿")
    # # 檢查轉換檔案資料夾是否存在，若不存在則建立資料夾
    if not os.path.exists(output_word_folder):
        os.makedirs(output_word_folder)

    # 儲存 Word 文件至 逐字稿 資料夾
    output_docx_path = os.path.join(output_word_folder, "output.docx")
    doc = docx.Document()
    doc.add_paragraph(formatted_transcription)
    doc.save(output_docx_path)
    # print(f"Transcription saved as {output_docx_path}")
