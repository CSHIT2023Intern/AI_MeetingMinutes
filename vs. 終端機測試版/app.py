#python server 不能執行
from flask import Flask,render_template
import speech_recognition as spec
import time
import os
import pyaudio



app = Flask(__name__)

@app.route('/')
def hello_world():
    return 'Hello World!'

@app.route('/home')
def index():
    return render_template('home.html')
    # return render_template('index.html',title = 'Welcomw',username = name)

@app.route('/text')
def text():
    return '<html><body><h1>Hello</h1></body></html>'

@app.route('/micro')
def micro():
    return render_template('home.html')
    
if __name__ == '__main__':
    app.run()
