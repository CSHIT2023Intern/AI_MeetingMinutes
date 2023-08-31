using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.CognitiveServices.Speech;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Text;
using Azure.AI.OpenAI;
using Azure;
using NAudio.Wave;

namespace azureMVC.Controllers
{
    public class RecordController : Controller
    {
        private static FileStream audioOutputStream;
        //speech to text
        private SpeechRecognizer speechRecognizers;
        private Task recTask;
        private bool isRecognizing = false;
        ////麥克風即時轉文字
        static StringBuilder MicroResult = new StringBuilder();
        private WaveInEvent waveInEvent;
        private WaveFileWriter waveFileWriter;

        private SpeechConfig speechConfig, config;
        static string value, MicroText, fpath8;
        private AudioConfig audioConfig;
        static StringBuilder TrascribeTxt = new StringBuilder();
        

        //將變數初始化
        public RecordController()
        {
            //單人辨識的初始化宣告(用於麥克風)
            speechConfig = SpeechConfig.FromSubscription("c9d3e6d440214af3bc175d4c31809a44", "eastasia");
            speechConfig.SpeechRecognitionLanguage = "zh-tw";
            audioConfig = AudioConfig.FromDefaultMicrophoneInput();
            speechRecognizers = new SpeechRecognizer(speechConfig, audioConfig);
        }

        // GET: Record
        public ActionResult Record()
        {
            return View();
        }
        
        [HttpPost]
        public ActionResult StartRecording()
        {
            // 指定要保存的語音檔路徑
            string folderPath = Server.MapPath("~/RecordFile/");
            string outputPath = Path.Combine(folderPath, "紀錄" + DateTime.Now.ToString("yyyyMMdd") + ".wav");
            // 初始化录音设备
            waveInEvent = new WaveInEvent();
            waveInEvent.WaveFormat = new WaveFormat(16000, 1);

            // 设置录音事件处理程序
            waveInEvent.DataAvailable += (sender, args) =>
            {
                if (waveFileWriter != null)
                {
                    waveFileWriter.Write(args.Buffer, 0, args.BytesRecorded);
                    waveFileWriter.Flush();
                }
            };

            // 初始化文件写入器
            waveFileWriter = new WaveFileWriter(outputPath, waveInEvent.WaveFormat);

            if (speechRecognizers == null)
            {
                // 初始化 speechRecognizers，使用相應的 speechConfig 和 audioConfig
                speechRecognizers = new SpeechRecognizer(speechConfig, audioConfig);
            }
            speechRecognizers.Recognized += (s, e) =>
            {
                if (e.Result.Reason == ResultReason.RecognizedSpeech)
                {
                    string text = e.Result.Text;
                    MicroResult.Append(text);
                    MicroText = MicroResult.ToString();
                    ViewBag.text = MicroText;
                    //MicroText = "Nothing";
                    MicroResult.Append("沒有內容");
                }
                else if (e.Result.Reason == ResultReason.NoMatch)
                {
                    ViewBag.text1 = "NOMATCH: Speech could not be recognized.";
                    Console.WriteLine($"NOMATCH: Speech could not be recognized.");
                }
            };
            // 开始录音
            waveInEvent.StartRecording();
            isRecognizing = true;
            //開始語音變識
            speechRecognizers.StartContinuousRecognitionAsync().Wait();
            return Json(new { message = "錄音開始" });
        }

        ////即時錄音辨識 停止錄音
        [HttpPost]
        public async Task<ActionResult> StopRecording()
        {
            if (isRecognizing)
            {
                // 停止語音變識
                waveInEvent.StopRecording();
                waveFileWriter?.Dispose();
                // 停止語音變識
                await speechRecognizers.StopContinuousRecognitionAsync();
                // 清理资源
                speechRecognizers.Dispose();
                isRecognizing = false;
            }
            TrascribeTxt.Clear();
            await TranscribeSpeechFinal(MicroResult.ToString());
            string content = MicroResult.ToString() + TrascribeTxt.ToString();
            WriteStringToWordAndSave(content);
            //return Json(new { message = MicroText });
            return Content(content, "text/plain");
        }

        //產生會議記錄
        [HttpPost]
        public async Task<ActionResult> TranscribeSpeechFinal(string text)
        {
            OpenAIClient client = new(new Uri("https://cshitinterngpt4.openai.azure.com/"), new AzureKeyCredential("71e9950da8c34bc7805520df08984c21"));
            var chatCompletionsOptions = new ChatCompletionsOptions()
            {
                Messages =
                {
                    new ChatMessage(ChatRole.System, "紀錄會議紀錄的人"),
                    new ChatMessage(ChatRole.User, text),
                    //new ChatMessage(ChatRole.User, ,
                    new ChatMessage(ChatRole.User, "統整重點並以會議紀錄的型式書寫"),
                },
                //文本的「創造性」程度。這個參數控制了生成文本時模型選擇詞語的隨機程度
                //Temperature = 1,
                //在生成下一個詞語時，只考慮詞語的累積機率分佈中最高的部分，例如:1、0.1
                NucleusSamplingFactor = (float)0.1,
                //存在懲罰，控制包含特定詞語的文本 較小的值會傾向於生成包含輸入提示中提到的詞語或主題的文本，而較大的值會減少這種傾向，0或是1比較適合。               
                PresencePenalty = 1,
                //頻率懲罰，控制重複詞語的頻率，調整生成文本中各詞語的重複頻率，以產生更多多樣性的輸出，會有?產生 
                //FrequencyPenalty = 1,
                // 最大標記數，限制文本長度
                MaxTokens = 3000 //MaxTokens = 32768 這樣在返回的結果中，生成的文本將會有最多32768個標記，這會跟gpt的上限為準
            };
            try
            {
                Response<StreamingChatCompletions> response = await client.GetChatCompletionsStreamingAsync(
                             deploymentOrModelName: "CSHInternGPT4-32K", chatCompletionsOptions);

                StreamingChatCompletions streamingChatCompletions = response.Value;

                TrascribeTxt.Append("會議重點整理");

                await foreach (StreamingChatChoice choice in streamingChatCompletions.GetChoicesStreaming())
                {
                    await foreach (ChatMessage message in choice.GetMessageStreaming())
                    {
                        TrascribeTxt.Append(message.Content);
                        //TextResult.Append(message.Content);
                        Console.Write(message.Content);
                    }
                    Console.WriteLine();
                }
                //ViewBag.text = TextResult.ToString();
                string content = TrascribeTxt.ToString();
                //string savePath = @"~\UploadedFiles"; // 替换为您希望保存的路径和文件名
                //WriteStringToWordAndSave(content);
                ViewBag.txtRes = "摘要完成";
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occurred: " + ex.Message);
            }
            return View();
        }

        //輸出word
        public void WriteStringToWordAndSave(string content)
        {
            // 創建Word應用程式
            var wordApp = new Microsoft.Office.Interop.Word.Application();

            // 創建新的Word檔
            var doc = wordApp.Documents.Add();

            try
            {
                //將內容寫入word
                doc.Content.Text = content;
                string folderPath = Server.MapPath("~/OutputTextFile/");
                string filename = "OutPutStringResult" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".docx";
                string outputPath = Path.Combine(folderPath, filename);
                //保存指定路徑
                doc.SaveAs(outputPath);
            }
            catch (Exception ex)
            {
                Console.WriteLine("ERROR" + ex.Message);
            }
            finally
            {
                // 釋放資源
                // 關閉文件和 Word 應用程式，並釋放相關資源
                doc.Close();
                wordApp.Quit();
                System.Runtime.InteropServices.Marshal.ReleaseComObject(doc);
                System.Runtime.InteropServices.Marshal.ReleaseComObject(wordApp);
                // 注意：在應用程式結束時，也應該釋放 wordApp 資源
            }
        }
    }
}