# BART
import docx2txt
from transformers import BartTokenizer, BartForConditionalGeneration

tokenizer = BartTokenizer.from_pretrained('facebook/bart-large-cnn')
model = BartForConditionalGeneration.from_pretrained('facebook/bart-large-cnn')

# 讀取文件
text = docx2txt.process('/content/OutputWord/output.docx')

# 生成摘要
sentences = text.split('\n')
input_text = ' '.join(sentences)
input_ids = tokenizer.encode(input_text, return_tensors='pt')

# 生成摘要
summary_ids = model.generate(input_ids, max_length=512, num_beams=5,
                             early_stopping=True, num_return_sequences=1, do_sample=True, temperature=0.7)

# 解碼生成的摘要
summary = tokenizer.decode(summary_ids[0], skip_special_tokens=True)
print('生成的簡短摘要：', summary)
