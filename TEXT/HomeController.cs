using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.CognitiveServices.Speech;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using System.IO;


namespace WebApplication2.Controllers
{
    public class HomeController : Controller
    {
        // GET: Home
        public ActionResult Index()
        {
            return View();
        }

        // 數字排序比較器
        public class NumericComparer : IComparer<string>
        {
            public int Compare(string x, string y)
            {
                int xNum = int.Parse(Path.GetFileNameWithoutExtension(x).Split('_')[1]);
                int yNum = int.Parse(Path.GetFileNameWithoutExtension(y).Split('_')[1]);
                return xNum.CompareTo(yNum);
            }
        }
             
        [HttpPost]
        public async Task<ActionResult> RecognizeSpeech()
        {
            //設定
            var config = SpeechConfig.FromSubscription("", "");
            config.SpeechRecognitionLanguage = "zh-TW";

            //檔案輸入          
            //var m4aFile = Server.MapPath(@"~\UploadFile\123.m4a");
            //var mp3File = Server.MapPath(@"~\UploadFile\read.mp3");
            //var audioFile = Server.MapPath(@"~\UploadFile\TaipeiMeet.wav");
            var audioFile = Server.MapPath(@"~\UploadFile\123.wav");

            // 在這裡獲取副檔名
            string audioExtension = Path.GetExtension(audioFile).ToLower();
            // 檔案類型判斷 
            if (audioExtension == ".mp3" || audioExtension == ".m4a")
            {
                // wav
                //Mp3FileReader專門mp3 MediaFoundationReader常見音訊
                using (var reader = new MediaFoundationReader(audioFile))

                //會留原本音檔
                using (var writer = new WaveFileWriter(audioFile, reader.WaveFormat))
                {
                    reader.CopyTo(writer);
                }
                //直接覆蓋原本音檔             
                //using (var reader = new MediaFoundationReader(m4aFile))
                //{
                //    WaveFileWriter.CreateWaveFile(m4aFile, reader);
                //}
            }
            
            //切割後檔案存放位置
            string outputDirectory = Server.MapPath(@"~\cutWav\");
            //每個小檔案的秒數
            int chunkSizeInSeconds = 5;
            //切歌成小檔案
            using (var reader = new WaveFileReader(audioFile))
            {
                int bytesPerSecond = reader.WaveFormat.AverageBytesPerSecond;
                int bytesPerChunk = chunkSizeInSeconds * bytesPerSecond;

                int chunkNumber = 1;
                int bytesRead;
                byte[] buffer = new byte[bytesPerChunk];

                while ((bytesRead = reader.Read(buffer, 0, buffer.Length)) > 0)
                {
                    string outputFilePath = Path.Combine(outputDirectory, $"chunk_{chunkNumber}.wav");
                    using (var writer = new WaveFileWriter(outputFilePath, reader.WaveFormat))
                    {
                        writer.Write(buffer, 0, bytesRead);
                    }
                    chunkNumber++;
                }
            }
            //最後組合用的
            var recognizedText = "";
            //把檔案存到陣列
            string[] files = Directory.GetFiles(outputDirectory, "chunk_*.wav");         
            //對檔案名稱進行自定義的數字排序
            Array.Sort(files, new NumericComparer());
            //辨識
            foreach (var file in files)
            {              
                using(var audioConfig = AudioConfig.FromWavFileInput(file))
                using(var speechRecognizer = new SpeechRecognizer(config, audioConfig))
                {
                    speechRecognizer.Recognized += (s, e) =>
                    {
                        if (e.Result.Reason == ResultReason.RecognizedSpeech)
                        {
                            string recognizedSegment = e.Result.Text;
                            recognizedText += recognizedSegment + " ";
                        }
                    };

                await speechRecognizer.StartContinuousRecognitionAsync();
                await Task.Delay(TimeSpan.FromSeconds(10));
                //await speechRecognizer.StopContinuousRecognitionAsync();
                }
                //刪除切割後的檔案
                try
                {
                    await Task.WhenAll(Task.Delay(1000));
                    System.IO.File.Delete(file);
                }
                catch (System.IO.IOException copyError)
                {
                    Console.WriteLine(copyError.Message);
                }
            } 
            return Content(recognizedText);
        }
    }
}
