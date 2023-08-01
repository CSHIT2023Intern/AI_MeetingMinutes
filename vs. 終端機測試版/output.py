#匯出檔案的程式
from gensim.summarization import summarize

# 讀取txt檔案
filename = 'apple.txt'
with open(filename, 'r',encoding='utf-8') as f:
    text = f.read()

# print(text)
# 摘要統整
summary = summarize(text)
print(summary)

# # 將摘要文字輸出到檔案
# with open('summary.txt', 'w') as f:
#     f.write(summary)