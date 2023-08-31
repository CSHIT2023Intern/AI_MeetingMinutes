from nltk.tokenize import sent_tokenize
import nltk
nltk.download('punkt')


def segment_article(article, max_sentences_per_segment):
    sentences = sent_tokenize(article)
    segmented_article = []
    current_segment = []

    for sentence in sentences:
        current_segment.append(sentence)
        if len(current_segment) >= max_sentences_per_segment:
            segmented_article.append(" ".join(current_segment))
            current_segment = []

    if current_segment:
        segmented_article.append(" ".join(current_segment))

    return segmented_article


input_article = "這是一個範例文章。這個範例演示了如何使用NLP進行文章分段。您可以根據句子數量來進行分段，這有助於更好地組織文章。"

max_sentences_per_segment = 2  # 每個分段的最大句子數量
segmented_articles = segment_article(input_article, max_sentences_per_segment)

for i, segment in enumerate(segmented_articles):
    print(f"段落 {i + 1}:\n{segment}\n")
