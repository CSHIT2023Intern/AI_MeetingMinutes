using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.CognitiveServices.Speech;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Net.Mime;
using System.IO;
using static System.Web.Razor.Parser.SyntaxConstants;

namespace WebApplication2.Controllers
{
    public class HomeController : Controller
    {
        // GET: Home
        public ActionResult Index()
        {
            return View();
        }

        // 自定義的數字排序比較器
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
            var config = SpeechConfig.FromSubscription("c9d3e6d440214af3bc175d4c31809a44", "eastasia");
            config.SpeechRecognitionLanguage = "zh-TW";

            //var m4aFile = Server.MapPath(@"~\UploadFile\123.m4a");
            //var mp3File = Server.MapPath(@"~\UploadFile\read.mp3");
            var audioFile = Server.MapPath(@"~\UploadFile\TaipeiMeet.wav");

            //mp3 to wav
            //using (var reader = new Mp3FileReader(mp3File))
            //using (var writer = new WaveFileWriter(audioFile, reader.WaveFormat))
            //{
            //    reader.CopyTo(writer);
            //}

            //m4a to wav
            //using (var reader = new MediaFoundationReader(m4aFile))
            //{
            //    WaveFileWriter.CreateWaveFile(m4aFile, reader);
            //}

            string outputDirectory = Server.MapPath(@"~\cutWav\");

            int chunkSizeInSeconds = 300; // 每個小檔案的秒數

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

            var recognizedText = "";
            string[] files = Directory.GetFiles(outputDirectory, "chunk_*.wav");

       
            // 對檔案名稱進行自定義的數字排序
            Array.Sort(files, new NumericComparer());
            foreach (var file in files)
            {
                var audioConfig = AudioConfig.FromWavFileInput(file);
                var speechRecognizer = new SpeechRecognizer(config, audioConfig);

                var stopRecognition = new TaskCompletionSource<int>();

                speechRecognizer.Recognized += (s, e) =>
                {
                    if (e.Result.Reason == ResultReason.RecognizedSpeech)
                    {
                        string recognizedSegment = e.Result.Text;
                        recognizedText += recognizedSegment + " ";
                    }
                };


                await speechRecognizer.StartContinuousRecognitionAsync();
                await Task.Delay(TimeSpan.FromSeconds(40));
                //await Task.WhenAll(Task.Delay(1000));

                //await speechRecognizer.StopContinuousRecognitionAsync();
            }
           ;
            return Content(recognizedText);

        }
    }
}