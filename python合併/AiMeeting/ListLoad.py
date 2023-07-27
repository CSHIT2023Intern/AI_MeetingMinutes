import os


def ListLoad(datafile):
    wav_files = []
    for filename in os.listdir(datafile):
        if filename.endswith('.wav'):
            file_path = os.path.join(datafile, filename)
            wav_files.append({'name': filename, 'url': file_path})
    return wav_files
