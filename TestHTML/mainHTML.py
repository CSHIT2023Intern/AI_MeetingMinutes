import os
import webbrowser
from pydub import AudioSegment


def mp3_to_wav(mp3_file, wav_file):
    # 讀取 MP3 檔案
    audio = AudioSegment.from_mp3(mp3_file)

    # 從原 MP3 檔案名稱中取得檔名（不含副檔名）
    filename = os.path.splitext(mp3_file)[0]

    # 設定與原檔案同名的 WAV 檔案路徑
    wav_file = f'{filename}.wav'

    # 將 MP3 檔案轉換為 WAV 格式
    audio.export(wav_file, format='wav')

    return wav_file


# 打開瀏覽器視窗並顯示 upload.html 檔案
webbrowser.open('file://' + os.path.abspath('upload.html'))

# 提示使用者輸入 MP3 檔案路徑
mp3_file_path = input("請輸入 MP3 檔案路徑：")

# 檢查使用者輸入的檔案路徑是否存在
if not os.path.isfile(mp3_file_path):
    print("找不到檔案。")
    exit()

# 呼叫函式進行轉換
wav_file_path = mp3_to_wav(mp3_file_path, None)

print("檔案轉換完成：", wav_file_path)
