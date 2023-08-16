#openai的 尚未完成 沒有API金鑰

# import os
# import openai
# # openai.organization = ""
# openai.api_key = os.getenv("") #要自己加
# openai.Model.list()

# import os
# import openai
# openai.organization = ""  #要自己加
# openai.organization = ""  #要自己加
# openai.Model.list()

# import os
# import openai
# import whisper
# openai.api_key = ""  #要自己加
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
API_KEY = "" #要自己加
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
