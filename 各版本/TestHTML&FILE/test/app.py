import os
from datetime import datetime
from flask import Flask, request, render_template, jsonify

app = Flask(__name__)

UPLOAD_FOLDER = r'C:\users\user\Desktop\112\20230707-1\音檔轉文字\test'
app.config['UPLOAD_FOLDER'] = UPLOAD_FOLDER


@app.route('/')
def index():
    return render_template('index.html')


@app.route('/upload', methods=['POST'])
def upload():
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
    else:
        return '請選擇檔案'


@app.route('/wav_files')
def get_wav_files():
    wav_files = []
    for filename in os.listdir(app.config['UPLOAD_FOLDER']):
        if filename.endswith('.wav'):
            file_path = os.path.join(app.config['UPLOAD_FOLDER'], filename)
            wav_files.append({'name': filename, 'url': file_path})
    return jsonify(wav_files)


if __name__ == '__main__':
    app.run(debug=True)
