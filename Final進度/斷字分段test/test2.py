import re
import jieba
from sklearn.feature_extraction.text import TfidfVectorizer
from sklearn.cluster import KMeans
import numpy as np


text = """
正式開始,首先請主席致詞。 謝謝,呃,很高興那個今天看到有一些是屬於老朋友,但是呢,還有一些是屬於就是呃,新認識的一個朋友,但是我想在性的一個業務的過程當中,那以自己個人的一個經歷啊。長期以來投入在性別暴力議題上。那或許在整個姓名的一個工作上來講的話,呃,有些的任時那也期待未來在臺北市。呃,我的新的職務上能夠。 跟大家一起學習,然後也跟大家在面對臺北市的一個性情的一個相關的工作,能夠有更好的發展。所以呢,也麻煩我們所有委員待會的話,你今天屬於給會前會,那會前會今天的一個功能,大概也讓大家彼此在任時,然後然後另外我們也針對我們,呃,未來的大會有什么樣子的議題跟想法？他大家可以細心地提出來,要做一些交流跟討論,好好,那我們接下來應該有一個時間是要給委員的字。 對接下來。 呃,本屆呃。專家學者及民間團體委員做自我介紹。
"""
# 使用jieba進行分詞
seg_list = jieba.cut(text, cut_all=False)
segmented_text = " ".join(seg_list)

# 將文本分成句子作為樣本
sentences = re.split(r'[，。！？]', text)
sentences = [s.strip() for s in sentences if s.strip()]

# 使用TF-IDF向量化文本
vectorizer = TfidfVectorizer()
X = vectorizer.fit_transform(sentences)

# 使用KMeans進行文本分段
num_clusters = 3  # 可調整分段的數量
kmeans = KMeans(n_clusters=num_clusters)
kmeans.fit(X)

# 將每句話分到相應的分段
sentence_clusters = kmeans.predict(X)

# 印出分段結果
grouped_sentences = [[] for _ in range(num_clusters)]

for idx, sentence in enumerate(sentences):
    cluster_idx = sentence_clusters[idx]
    grouped_sentences[cluster_idx].append(sentence)

for cluster_idx, group in enumerate(grouped_sentences):
    print(f"分段 {cluster_idx + 1}:\n")
    for sentence in group:
        print(sentence)
    print("\n---\n")
