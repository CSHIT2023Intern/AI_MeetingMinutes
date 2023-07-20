#openai的 尚未完成 沒有API金鑰

# import os
# import openai
# # openai.organization = "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"
# openai.api_key = os.getenv("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")
# openai.Model.list()

# import os
# import openai
# openai.organization = "org-bWTePyiJSiaokFMvqUaQNGkT"
# openai.organization = "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"
# openai.Model.list()

# import os
# import openai
# import whisper
# openai.api_key = "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"
# audio_file = open("test.wav", "rb")
# id = "whisper-1"
# response = openai.Audio.transcribe(
#     api_key=openai.api_key,
#     model=id,
#     file=audio_file
# )
# print(response.data['text'])

import os
import openai
import whisper
API_KEY = "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"
audio_file = open("test.wav", "rb")
id = "whisper-1"
response = openai.Audio.transcribe(
    file=audio_file,
    model=id,
    api_key=API_KEY

    # model: Any, file: Any, api_key: Any | None = None, api_base: Any | None = None, api_type: Any | None = None, api_version: Any | None = None, organization: Any | None = None, **params: Any
)
print(response.data['text'])

# import os
# fi = form['filename']
# if fi.filename:
# 	# This code will strip the leading absolute path from your file-name
# 	fil = os.path.basename(fi.filename)
# 	# open for reading & writing the file into the server
# 	open(fil, 'wb').write(fi.file.read())
