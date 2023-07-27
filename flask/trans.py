import os
import docx
import whisper


def transcribe_audio(input_file):
    # 載入模型
    # large會標點符號但很慢 #medium最少要用這個
    model = whisper.load_model("base")

    # 進行轉錄
    result = model.transcribe(input_file)
    transcription = result["text"]

    # 檢查並建立 OutputWord 資料夾
    output_word_folder = "OutputWord"
    if not os.path.exists(output_word_folder):
        os.makedirs(output_word_folder)

    # 儲存 Word 文件至 OutputWord 資料夾
    output_docx_path = os.path.join(output_word_folder, "output.docx")
    doc = docx.Document()
    doc.add_paragraph(transcription)
    doc.save(output_docx_path)

    # 返回 Word 文件的路徑
    return output_docx_path
