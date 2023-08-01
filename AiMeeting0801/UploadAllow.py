# 副檔名檢查
ALLOWED_EXTENSIONS = set(['wav', 'm4a'])


def allowed_file(filename):
    return '.' in filename and \
           filename.rsplit('.', 1)[1] in ALLOWED_EXTENSIONS
