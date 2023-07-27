import os
from datetime import datetime
from werkzeug.utils import secure_filename
from flask import Flask, request, redirect, url_for, send_from_directory, render_template, jsonify
from transs import transcribe_audio
from summary import generate_summary

app = Flask(__name__)
UPLOAD_FOLDER = r'C:\users\user\Desktop\112\20230707-1\音檔轉文字\flask\upload'
app.config['UPLOAD_FOLDER'] = UPLOAD_FOLDER
ALLOWED_EXTENSIONS = set(['wav', 'm4a'])


# 副檔名檢查
def allowed_file(filename):
    return '.' in filename and \
           filename.rsplit('.', 1)[1] in ALLOWED_EXTENSIONS


@app.route('/', methods=['GET', 'POST'])
def upload_file():
    if request.method == 'POST':
        file = request.files['file']
        if file and allowed_file(file.filename):
            filename = secure_filename(file.filename)
            file.save(os.path.join(app.config['UPLOAD_FOLDER'], filename))
            # return redirect(url_for('uploaded_file', filename=filename))
    return render_template('index.html')


@app.route('/audio', methods=['POST'])
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


@app.route('/uploads/<filename>')
def uploaded_file(filename):
    return send_from_directory(app.config['UPLOAD_FOLDER'], filename)


@app.route('/generate_summary', methods=['POST'])
def generate_summary_click():
    wav_files = []
    for filename in os.listdir(app.config['UPLOAD_FOLDER']):
        if filename.endswith('.wav'):
            file_path = os.path.join(app.config['UPLOAD_FOLDER'], filename)
            wav_files.append({'name': filename, 'url': file_path})
    return render_template('summary.html', wav_files=wav_files)


@app.route('/select_summary_file', methods=['POST'])
def select_summary_file():
    selected_file = request.form.get('selected_wav_file')
    print(selected_file)
    wordfile = transcribe_audio(selected_file)
    output = r"C:\users\user\Desktop\112\20230707-1\音檔轉文字\flask\final"
    generate_summary(wordfile, output)
    wav_files = []
    for filename in os.listdir(app.config['UPLOAD_FOLDER']):
        if filename.endswith('.wav'):
            file_path = os.path.join(app.config['UPLOAD_FOLDER'], filename)
            wav_files.append({'name': filename, 'url': file_path})

    return render_template('summary.html', wav_files=wav_files, selected_file=selected_file)


if __name__ == '__main__':
    app.run(debug=True)
