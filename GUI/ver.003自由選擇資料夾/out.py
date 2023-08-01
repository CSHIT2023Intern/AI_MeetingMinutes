import os
import shutil


def clear(upload_dir, converted_dir):
    # 删除複製上傳檔案的文件夹
    if os.path.exists(upload_dir):
        shutil.rmtree(upload_dir)

    # 删除軟檔後檔案的文件夹
    if os.path.exists(converted_dir):
        shutil.rmtree(converted_dir)

    # # 删除Word檔案的文件夹
    # if os.path.exists(OutputWordFile):
    #     shutil.rmtree(OutputWordFile)
