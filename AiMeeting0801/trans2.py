import os
import whisper
from datetime import datetime

# 存檔路徑
OutPutPath = 'txt'

# 將日期時間變成字串
todayStr = datetime.now().strftime("%Y-%m-%d__%H-%M-%S")


def trans(selected_file):
    # 載入模型
    # large會標點符號但很慢 #medium最少要用這個
    model = whisper.load_model("tiny")

    # 進行轉錄
    result = model.transcribe(selected_file)
    txt = result["text"]

    # 檢查並建立 OutPutPath 資料夾
    if not os.path.exists(OutPutPath):
        os.makedirs(OutPutPath)

    # 輸出語音轉文字資料到資料夾中
    ffname = os.path.join(OutPutPath, '語音轉文字_'+todayStr+".txt")
    with open(ffname, "w", encoding='UTF-8') as file:
        file.write(txt)

    return txt
