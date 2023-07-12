# GPT2-完全一樣
import docx2txt
import tensorflow as tf
from transformers import GPT2Tokenizer, TFGPT2LMHeadModel

tokenizer = GPT2Tokenizer.from_pretrained('gpt2')
model = TFGPT2LMHeadModel.from_pretrained('gpt2')

# 讀取文件
text = docx2txt.process('/content/OutputWord/output.docx')

# 生成摘要
sentences = text.split('\n')
input_text = ' '.join(sentences)
input_ids = tokenizer.encode(input_text, return_tensors='tf')
summary_ids = model.generate(
    input_ids, max_length=1024, num_return_sequences=1, early_stopping=True)
summary = tokenizer.decode(summary_ids[0])
print('生成摘要：', summary)
