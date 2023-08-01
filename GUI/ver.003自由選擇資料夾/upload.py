import os

from pydub import AudioSegment

# 允許上傳的檔案格式
AlllowFileType = ['.wav', '.m4a', '.mp3']


# 檢查上傳的檔案格式
def is_allowed_file(filename):
    ext = os.path.splitext(filename)[1]
    return ext.lower() in AlllowFileType


# 讀取上傳檔案轉成WAV檔
def convert_to_wav(input_file, output_dir):
    # 讀取上傳的檔案
    audio = AudioSegment.from_file(input_file)

    # 轉換成WAV格式
    output_filename = os.path.basename(input_file)  # 取得檔案名稱
    output_filename = os.path.splitext(output_filename)[
        0] + ".wav"  # 修改副檔名為.wav
    output_path = os.path.join(output_dir, output_filename)  # 組合輸出檔案路徑

    audio.export(output_path, format="wav")

    return output_path
