# 匯入套件
import jieba

# 宣告字串
sentence = '今天是一個晴朗的星期天'
sentence2 = '今天是一個晴朗的星期天。我要去西門町看電影，騎完之後要去東吳大學後面爬山'

# 載入自定義詞典
file_path = 'custom_dict.txt'
jieba.load_userdict(file_path)

# 斷詞

# 全模式
# s1_list = jieba.cut(sentence2, cut_all=True)
# print('模式- 全  ： ', ' | '.join(s1_list))

# 精確模式
s2_list = jieba.cut(sentence2, cut_all=False)
print('模式- 精確： ', ' | '.join(s2_list))

# 預設模式
# s3_list = jieba.cut(sentence2)
# print('模式- 預設： ', ' | '.join(s3_list))

# 搜尋引擎模式
# s4_list = jieba.cut_for_search(sentence2)
# print('模式- 搜尋： ', ' | '.join(s4_list))
