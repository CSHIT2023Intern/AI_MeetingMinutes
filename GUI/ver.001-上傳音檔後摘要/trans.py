import os
import docx
import whisper


def transcribe_audio(input_file):
    # 載入模型
    # large會標點符號但很慢 #medium最少要用這個
    model = whisper.load_model("tiny")

    # 進行轉錄
    result = model.transcribe(input_file)
    transcription = result["text"]

    # 將結果存儲為 Word 文件，每 20 個字元換行
    formatted_transcription = "\n".join(
        [transcription[i:i+20] for i in range(0, len(transcription), 20)])

    # 檢查並建立資料夾
    output_txt_folder = "OutputTxt"
    output_word_folder = "OutputWord"
    if not os.path.exists(output_txt_folder):
        os.makedirs(output_txt_folder)
    if not os.path.exists(output_word_folder):
        os.makedirs(output_word_folder)

    # 儲存 Word 文件至 OutputWord 資料夾
    output_docx_path = os.path.join(output_word_folder, "output.docx")
    doc = docx.Document()
    doc.add_paragraph(formatted_transcription)
    doc.save(output_docx_path)
    # print(f"Transcription saved as {output_docx_path}")

    # 儲存純文字格式的轉錄結果至 OutputTxt 資料夾
    output_txt_path = os.path.join(output_txt_folder, "output.txt")
    with open(output_txt_path, "w") as file:
        file.write(transcription)
    # print(f"Transcription saved as {output_txt_path}")
