using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.IO;
using System.Web.Mvc;
using System.Threading.Tasks;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using System.Text;
using AzureMVC.Models;
using Azure.AI.OpenAI;
using Azure;
using Task = System.Threading.Tasks.Task;

namespace AzureMVC.Controllers
{
    public class UploadController : Controller
    {

        static string speechKey = Environment.GetEnvironmentVariable("SPEECH_KEY");//這裡因為我在cmd裡把SPEECH_KEY設為c9d3e6d440214af3bc175d4c31809a44
        static string speechRegion = Environment.GetEnvironmentVariable("SPEECH_REGION");//SPEECH_REGION為eastasia

        static StringBuilder all_result = new StringBuilder();
        static StringBuilder TextResult = new StringBuilder();
        static StringBuilder MicroResult = new StringBuilder();
        static StringBuilder TrascribeTxt = new StringBuilder();
        static string value,MicroText;
        private bool isRecognizing = false;
        private SpeechRecognizer speechRecognizers;
        private SpeechConfig speechConfig;
        private AudioConfig audioConfig;
        private Task recTask;
        
        //將變數初始化
        public UploadController()
        {
            speechConfig = SpeechConfig.FromSubscription(speechKey, speechRegion);
            speechConfig.SpeechRecognitionLanguage = "zh-tw";
            audioConfig = AudioConfig.FromDefaultMicrophoneInput();
            speechRecognizers = new SpeechRecognizer(speechConfig, audioConfig);
        }

        // GET: Upload
        public ActionResult Index()
        {
            return View();
        }

        ////語音檔辨識
        [HttpGet]
        public ActionResult UploadFile()
        {
            //刪除資料夾檔案
            //DirectoryInfo FP = new DirectoryInfo(Server.MapPath("~/UploadedFiles/"));//抓出檔案路徑
            //FileInfo[] files = FP.GetFiles();//抓取檔案
            //foreach (FileInfo file in files)
            //{
            //    file.Delete();
            //}
            //讀取資料夾
            string[] fpaths = Directory.GetFiles(Server.MapPath("~/UploadedFiles/"));
            List<FileModel> files = new List<FileModel>();
            foreach (string fpath in fpaths)
            {
                files.Add(new FileModel { Filename = Path.GetFileName(fpath) });
            }
            return View(files);
        }

        [HttpPost]
        //[ValidateAntiForgeryToken]
        public async Task<ActionResult> Translate(string filename)
        {
            TextResult.Clear();
            string folderPath = Server.MapPath("~/UploadedFiles/");
            string filePath = Path.Combine(folderPath, filename);
            if (System.IO.File.Exists(filePath)) //找出檔案 是存在的
            {
                var speechConfig = SpeechConfig.FromSubscription(speechKey, speechRegion);
                speechConfig.SpeechRecognitionLanguage = "zh-tw";
                var audioConfig = AudioConfig.FromWavFileInput(filePath);
                var speechRecognizer = new SpeechRecognizer(speechConfig, audioConfig);
                //為只做一次
                //var result = speechRecognizer.RecognizeOnceAsync().Result;
                //ViewBag.word = result.Text;
                //可以連續讀取
                speechRecognizer.Recognized += (s, e) =>
                {
                    if (e.Result.Reason == ResultReason.RecognizedSpeech)
                    {
                        string text = e.Result.Text;
                        TextResult.Append(text);
                    }
                    else if (e.Result.Reason == ResultReason.NoMatch)
                    {
                        Console.WriteLine($"NOMATCH: Speech could not be recognized.");
                    }
                };
                //開始語音變識
                Task recTask = speechRecognizer.StartContinuousRecognitionAsync();
                //await Task.Delay(TimeSpan.FromSeconds(30));
                //等待變識結束
                //await speechRecognizer.StopContinuousRecognitionAsync();
                //等待完全讀取
                await recTask;
                ViewBag.transalte = TextResult.ToString();
                await TranscribeSpeech(TextResult.ToString());
                string content = TextResult.ToString();
                //string savePath = @"~\UploadedFiles"; // 替换为您希望保存的路径和文件名
                string savePath = @"~/TextWord/"; // 替换为您希望保存的路径和文件名
                //WriteStringToWordAndSave(content, savePath);
            }
            return Content(TextResult.ToString(), "text/plain");
        }


        [HttpPost]
        public ActionResult UploadFile(HttpPostedFileBase audioFile)
        {
            try
            {
                if(audioFile == null)
                {
                    return View();
                }
                else if (audioFile.ContentLength > 0)
                {
                    //檢查檔案類型是否為wav
                    if(Path.GetExtension(audioFile.FileName).ToLower()!= ".wav")
                    {
                        ViewBag.mess = "只能上傳wav檔";
                        return View();
                    }
                    string FileName = Path.GetFileName(audioFile.FileName);
                    string FilePath = Path.Combine(Server.MapPath("~/UploadedFiles"), FileName);
                    audioFile.SaveAs(FilePath);
                    string[] fpaths = Directory.GetFiles(Server.MapPath("~/UploadedFiles/"));
                    List<FileModel> files = new List<FileModel>();
                    foreach (string fpath in fpaths)
                    {
                        files.Add(new FileModel { Filename = Path.GetFileName(fpath) });
                    }
                }
                ViewBag.mess = "Success!";
                return RedirectToAction("UploadFile"); //導向 UploadFile() function
            }
            catch
            {
                ViewBag.mess = "error";
                return View();
            }
        }


        ////即時錄音辨識
        [HttpGet]
        public ActionResult RecordFile()
        {
            return View();
        }

        [HttpPost]
        public ActionResult StartRecording()
        {
            // 確保 speechRecognizers 不為 null
            if (speechRecognizers == null)
            {
                // 初始化 speechRecognizers，使用相應的 speechConfig 和 audioConfig
                speechRecognizers = new SpeechRecognizer(speechConfig, audioConfig);
            }
            isRecognizing = true;
            speechRecognizers.Recognized += (s, e) =>
            {
                if (e.Result.Reason == ResultReason.RecognizedSpeech)
                {
                    string text = e.Result.Text;
                    MicroResult.Append(text);
                    MicroText = MicroResult.ToString();
                    ViewBag.text = MicroText;
                    //MicroText = "Nothing";
                }
                else if (e.Result.Reason == ResultReason.NoMatch)
                {
                    ViewBag.text1 = "NOMATCH: Speech could not be recognized.";
                    Console.WriteLine($"NOMATCH: Speech could not be recognized.");
                }
            };
            //開始語音變識
            recTask = speechRecognizers.StartContinuousRecognitionAsync();
            return Json(new { message = "錄音開始" });
        }

        [HttpPost]
        public async  Task<ActionResult> StopRecording()
        {
            isRecognizing = false;
            // 停止語音變識
            await speechRecognizers.StopContinuousRecognitionAsync();
            //MicroResult.Append("Nothing");
            var val = await TranscribeSpeech(MicroResult.ToString());
            MicroResult.Append(val);
            //return Json(new { message = MicroText });
            return Content(MicroResult.ToString(), "text/plain");
        }
        
        //產生會議記錄
        [HttpPost]
        public async Task<ActionResult> TranscribeSpeech(string text)
        {

            OpenAIClient client = new(new Uri("https://cshitinternopenai.openai.azure.com/"), new AzureKeyCredential("0be4adcd512d4b09b7e44d50325f4bf9"));
            text = "各位評審老師。各位同學。大家好。今天。我所要朗讀的篇目是。第三套題。崇武。作者,馮輝岳。我家四周都氣有小水溝。靠近後面的這一段水溝比較寬。溝底的水泥均列了。形成幾個坑洞。經常接著水。這一段水溝少有乾枯的時候。因為我家的澡堂就在後門邊。洗澡用過的水全都排進來這裡。天氣晴朗的日子。陽光照進小水溝。走出後門。我喜歡蹲在水溝旁。看那一窪一窪的積水。這些原來混濁的積水。沉澱以後。只見厚厚的黑泥,噗滿高的。上層的水卻是清澈透明的。陽光穿過水面。印在溝底。涼涼的。暖暖的。我看見許多吉祥的紅蟲。這裡一隻,那裡一隻。紛紛鑽出黑龍江。在清澈的水中蠕動一下。應該說是扭動。一窪積水裡大約有兩三百隻呢。比紅絲線更小的身子。不停的扭著。五針。鳥頭扭腰扭屁股。他們興奮地扭啊扭啊。陽光越強,他們扭得越起勁。一窪窪的積水,彷彿一座座的舞臺。精力充沛的小紅蟲,是快樂的武者。幾百隻異禽,牛五。靜悄悄,沒有音樂,沒有鼓聲。但那是多麼壯觀的表演呢？我聚精會神地觀賞。連水溝飄起來的味道也不覺得臭了。觀賞小紅蟲的牛武城了,我的秘密。好幾次。我夢見成千上萬的小紅蟲。在波光鱗鱗的大池扭動呢？雖然每隔不久。母親都要抽一次水溝。不過小紅蟲好像永遠除不進。後來有人告訴我。那些小紅蟲是蚊子的幼蟲。我嚇了一跳。從此我再也不喜歡看水溝中的崇武了。我的朗讀就到這裡結束了,謝謝大家。";
            //string txt = "詞 曲   李 宗 盛 詞 曲   李 宗 盛 故 事 情 裡 的 世 界 越 來 越 遠 的 道 別 雨 傘 上 下 了 雨 殘 影 還 是 黑 夜 我 用 眼 光 去 追 緊 聽 著 你 的 雷 在 車 窗 外 面 徘 徊 是 我 錯 失 的 機 會 雨 傘 的 方 位 跟 我 從 前 的 爭 執 接 近 你 腳 踏 後 腿 你 的 崩 潰 在 窗 外 淋 水 我 一 日 閒 眠 離 開 有 你 的 季 節 你 說 你 後 面 也 無 法 再 相 信 風 在 山 流 去 過 往 的 畫 面 卻 都 是 我 不 配 心 是 藏 匪 我 想 你 幾 位 你 為 我 故 事 情 裡 的 世 界 越 來 越 遠 的 道 別 雨 轉 身 向 北 成 年 還 是 很 美 我 用 眼 光 去 追 緊 聽 著 你 的 雷 在 車 窗 外 面 徘 徊 我 後 面 是 我 錯 失 的 機 會 雨 傘 的 方 位 跟 我 從 前 的 爭 執 接 近 你 腳 踏 後 腿 你 的 崩 潰 在 窗 外 淋 水 我 一 日 閒 眠 離 開 有 你 的 季 節 你 說 你 後 面 也 無 法 再 相 信 風 在 山 流 去 過 往 的 畫 面 卻 都 是 我 不 配 心 是 藏 匪 我 想 你 幾 位 我 一 日 閒 眠 離 開 有 你 的 季 節 風 向 旁 走 位 揮 轉 著 我 的 後 悔 我 這 束 長 眼 卻 甩 不 掉 靜 靜 的 睡 在 山 邊 心 是 藏 匪 我 想 你 幾 位 前 進 龍 壁 就 讓 初 次 安 靜 Z i t h e r   H a r p";
            var chatCompletionsOptions = new ChatCompletionsOptions()
            {
                Messages =
                {
                new ChatMessage(ChatRole.System,"我是一個秘書要做會議紀錄"),
                new ChatMessage(ChatRole.Assistant,text),
                new ChatMessage(ChatRole.User,"可以幫統整重點嗎?"),
                new ChatMessage(ChatRole.User,"可以做成會議摘要嗎?"),
                },
                MaxTokens = 1000 //MaxTokens = 500000 這樣在返回的結果中，生成的文本將會有最多500000個標記

                //new ChatMessage(ChatRole.System,"You are a helpful assistant. You will tealk like a private."),
                //new ChatMessage(ChatRole.User,"Does Azure OpenAI support customer managed key?"),
                //new ChatMessage(ChatRole.Assistant,"Yes, customer managed keys are support by Azure OpenAI."),
                //new ChatMessage(ChatRole.User,"Do other Azure AI services support this too?")
            };
            //string deployment = "CSHITIntern"; //engine
            try
            {
                TrascribeTxt.Clear();
                Response<StreamingChatCompletions> response = await client.GetChatCompletionsStreamingAsync(
                             deploymentOrModelName: "CSHITIntern", chatCompletionsOptions);

                StreamingChatCompletions streamingChatCompletions = response.Value;

                await foreach (StreamingChatChoice choice in streamingChatCompletions.GetChoicesStreaming())
                {
                    await foreach (ChatMessage message in choice.GetMessageStreaming())
                    {
                        TrascribeTxt.Append(message.Content);
                        MicroResult.Append(message.Content);
                        //TextResult.Append(message.Content);
                        Console.Write(message.Content);
                    }
                    Console.WriteLine();
                }
                //ViewBag.text = TextResult.ToString();
                string tt = TrascribeTxt.ToString();
                string content = tt;
                //string savePath = @"~\UploadedFiles"; // 替换为您希望保存的路径和文件名
                string savePath = @"~/TextWord"; // 替换为您希望保存的路径和文件名
                WriteStringToWordAndSave(content, savePath);
                ViewBag.txtRes = "摘要完成";
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occurred: " + ex.Message);
            }
            return View();
        }

        //輸出word
        public void WriteStringToWordAndSave(string content, string savePath)
        {
            // 創建Word應用程式
            var wordApp = new Microsoft.Office.Interop.Word.Application();

            // 創建新的Word檔
            var doc = wordApp.Documents.Add();

            try
            {
                //將內容寫入word
                doc.Content.Text = content;

                //保存指定路徑
                doc.Save();
            }
            catch (Exception ex)
            {
                Console.WriteLine("ERROR" + ex.Message);
            }
            finally
            {
                //關閉Word檔和Word程式
                doc.Close();
                wordApp.Quit();
            }
        }

    }
}