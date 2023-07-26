import os
import openai
from datetime import datetime
import speech_recognition
from werkzeug.utils import secure_filename
from flask import Flask, request, render_template, jsonify
from UploadAllow import allowed_file
from summary import summary

app = Flask(__name__)
# 上傳檔案路徑
UPLOAD_FOLDER = 'upload'
app.config['UPLOAD_FOLDER'] = UPLOAD_FOLDER


# 首頁
@app.route('/')
def Index():
    return render_template('index.html')


# 錄音頁
@app.route('/recording', methods=['POST', 'GET'])
def recording():
    if 'audio' in request.files:
        audio_file = request.files['audio']
        # 取得當前的日期時間
        current_datetime = datetime.now().strftime("%Y%m%d_%H%M%S")
        # 檔案名稱為 Record_日期時間.wav
        filename = f"Record_{current_datetime}.wav"
        # 檔案路徑
        filepath = os.path.join(app.config['UPLOAD_FOLDER'], filename)
        # 將音訊檔案保存到指定位置
        audio_file.save(filepath)
        return '檔案上傳成功'
    return render_template('recording.html')


# 上傳頁-上傳檔案並更新下拉選單可以選擇進摘要
@app.route('/upload_select', methods=['POST', 'GET'])
def upload_select():
    # 使用者是否有選擇檔案
    selected_file = request.form.get('selected_wav_file')
    # 是
    if selected_file is not None:
        # 進入語音轉文字
        response = summary(selected_file)
        # 更新下拉選單
        wav_files = []
        for filename in os.listdir(app.config['UPLOAD_FOLDER']):
            if filename.endswith('.wav'):
                file_path = os.path.join(app.config['UPLOAD_FOLDER'], filename)
                wav_files.append({'name': filename, 'url': file_path})
        # 回傳選單和摘要資料到前端
        return render_template('UploadResult.html', wav_files=wav_files, text=response)
    # 否
    else:
        # 上傳檔案
        if request.method == 'POST':
            file = request.files['file']
            if file and allowed_file(file.filename):
                filename = secure_filename(file.filename)
                file.save(os.path.join(app.config['UPLOAD_FOLDER'], filename))
        # 更新下拉選單
        wav_files = []
        for filename in os.listdir(app.config['UPLOAD_FOLDER']):
            if filename.endswith('.wav'):
                file_path = os.path.join(app.config['UPLOAD_FOLDER'], filename)
                wav_files.append({'name': filename, 'url': file_path})
        # 回傳選單到前端
        return render_template('UploadResult.html', wav_files=wav_files)


if __name__ == '__main__':
    app.debug = True
    app.run()
