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

namespace WebApplication2.Controllers
{
    public class HomeController : Controller
    {
        // GET: Home
        public ActionResult Index()
        {
            return View();
        }

        [HttpPost]


        public async Task<ActionResult> RecognizeSpeech()
        {
            

            var config = SpeechConfig.FromSubscription("AzureKey", "AzureRegion");

            var mp3File = Server.MapPath(@"~\UploadFile\123.mp3");
            var audioFile = Server.MapPath(@"~\UploadFile\123.wav");

            //language
            config.SpeechRecognitionLanguage = "zh-cN";

            var recognizedText = "";

            var stopRecognition = new TaskCompletionSource<int>();

            using (var reader = new Mp3FileReader(mp3File))
            using (var writer = new WaveFileWriter(audioFile, reader.WaveFormat))
            {
                reader.CopyTo(writer);
            }

            using (var audioConfig = AudioConfig.FromWavFileInput(audioFile))
            using (var recognizer = new SpeechRecognizer(config, audioConfig))
            {
                //var phraseList = PhraseListGrammar.FromRecognizer(recognizer);
                //phraseList.AddPhrase("有一個");
                //phraseList.AddPhrase("夏日");
                //phraseList.AddPhrase("夕陽");

                recognizer.Recognized += (s, e) =>
                {
                    if (e.Result.Reason == ResultReason.RecognizedSpeech)
                    {
                        string recognizedSegment = e.Result.Text;
                        recognizedText += recognizedSegment + " ";
                    }
                };

                recognizer.SessionStopped += (s, e) =>
                {
                    stopRecognition.SetResult(0);
                };

                await recognizer.StartContinuousRecognitionAsync();
                await stopRecognition.Task; // 等待异步操作完成

                return Content(recognizedText); // 确保在异步操作完成后返回内容
            }
        }
    }
}