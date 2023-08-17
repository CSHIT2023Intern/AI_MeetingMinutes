import openai
import os
import docx
from datetime import datetime

# 存檔路徑
ffpath = 'txt'

# 將日期時間變成字串
todayStr = datetime.now().strftime("%Y-%m-%d__%H-%M-%S")


def summary(txt):
    # API設定
    openai.api_type = "azure"
    openai.api_base = "https://cshitinternopenai.openai.azure.com/"
    openai.api_version = "2023-03-15-preview"
    openai.api_key = ""

    response = openai.ChatCompletion.create(
        engine="CSHITIntern",
        messages=[
            {"role": "system", "content": "我是一個秘書要做會議紀錄"},  # 人物設定
            {"role": "assistant", "content": txt},  # 文章
            {"role": "user", "content": "可以幫我們統整重點"}  # 想要叫GPT做的事
        ])

    # 輸出摘要資料到資料夾中
    ffname = os.path.join(
        ffpath, '會議摘要_'+todayStr+'.txt')
    with open(ffname, "w", encoding='UTF-8') as file:  # 寫入檔案到資料夾中
        file.write(response['choices'][0]['message']['content'])

    # 將摘要內容存成 Word 檔案
    output_doc_path = os.path.join(ffpath, '會議摘要_'+todayStr+'.docx')
    output_doc = docx.Document()
    output_doc.add_paragraph(response['choices'][0]['message']['content'])
    output_doc.save(output_doc_path)

    # 回傳到主程式
    Response = response['choices'][0]['message']['content']
    return (Response)
