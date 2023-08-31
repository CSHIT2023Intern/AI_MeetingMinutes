using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.IO;
using System.Web.Mvc;
using System.Threading.Tasks;
using System.Text;
using System.Net.Http;
using System.Runtime.Serialization;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.CognitiveServices.Speech.Transcription;
using AzureMVC.Models;
using Azure.AI.OpenAI;
using Azure;
using Task = System.Threading.Tasks.Task;
using Newtonsoft.Json;
using helloworld;
using NAudio.Wave;

namespace AzureMVC.Controllers
{
    public class UploadController : Controller
    {

        //static string speechKey = Environment.GetEnvironmentVariable("SPEECH_KEY");//這裡因為我在cmd裡把SPEECH_KEY設為c9d3e6d440214af3bc175d4c31809a44
        //static string speechRegion = Environment.GetEnvironmentVariable("SPEECH_REGION");//SPEECH_REGION為eastasia
        static string speechKey = "c9d3e6d440214af3bc175d4c31809a44";
        static string speechRegion = "eastasia";
        static string subscriptionKey = "2581928f7d5042e190f8fb24c94540a1";
        static string serviceRegion = "eastus";

        //speech to text
        private SpeechRecognizer speechRecognizers;
        ////語音檔轉文字
        static StringBuilder TextResult = new StringBuilder(); //轉完的文字
        ////麥克風即時轉文字
        static StringBuilder MicroResult = new StringBuilder();
        private WaveInEvent waveInEvent;
        private WaveFileWriter waveFileWriter;


        //coversation speaker recognize
        private ConversationTranscriber conversationTranscriber;

        static StringBuilder all_result = new StringBuilder();

        //TranscribeSpeeching()、TranscribeSpeechFinal()
        static StringBuilder TrascribeTxt = new StringBuilder(); //摘要(包涵 切割文字檔後跑的、直接轉完文字跑的)
        static StringBuilder CognizeText = new StringBuilder();
        static string MicroText;
        private bool isRecognizing = false;
        private SpeechConfig speechConfig, config;
        private AudioConfig audioConfig;
        private Task recTask;
        private static FileStream audioOutputStream;


        //將變數初始化
        public UploadController()
        {
            //單人辨識的初始化宣告(用於麥克風)
            speechConfig = SpeechConfig.FromSubscription(speechKey, speechRegion);
            speechConfig.SpeechRecognitionLanguage = "zh-tw";
            audioConfig = AudioConfig.FromDefaultMicrophoneInput();
            speechRecognizers = new SpeechRecognizer(speechConfig, audioConfig);
            //多人辨識的初始化宣告(用於麥克風)
            config = SpeechConfig.FromSubscription(subscriptionKey, serviceRegion);
            config.SpeechRecognitionLanguage = "zh-tw";
            config.SetProperty("ConversationTranscriptionInRoomAndOnline", "true");
            config.SetProperty("DifferentiateGuestSpeakers", "true"); //變這行執行時會出現錯誤
            //conversationTranscriber = new ConversationTranscriber(audioConfig);
        }

        // GET: Upload
        public ActionResult Index()
        {
            return View();
        }

////------------------------------------------------------------------------------------------------------------        

        ////語音檔辨識 上傳錄音檔的介面
        [HttpGet]
        public async Task<ActionResult> UploadFile()
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
                //if(fpath == "")
                //{
                //    files.Add(new FileModel { Filename = "" });                    ;
                //}
                files.Add(new FileModel { Filename = Path.GetFileName(fpath) });
            }
            return View(files);
        }

        ////語音檔辨識 上傳錄音檔
        [HttpPost]
        public ActionResult UploadFile(HttpPostedFileBase audioFile)
        {
            try
            {
                if (audioFile == null)
                {
                    return View();
                }
                else if (audioFile.ContentLength > 0)
                {
                    //檢查檔案類型是否為wav, mp3 ,m4a
                    string[] allowedExtensions = { ".wav", ".mp3", ".m4a" };
                    string fileExtension = Path.GetExtension(audioFile.FileName).ToLower();
                    if (!allowedExtensions.Contains(fileExtension))
                    {
                        ViewBag.mess = "只能上傳wav、mp3、m4a檔";
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
                return RedirectToAction("UploadFile"); //導向 UploadFile() function
            }
            catch
            {
                return View();
            }
        }

////------------------------------------------------------------------------------------------------------------        

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

        //轉檔( covert .mp3,.m4a file to .wav)
        public static string CoverTo_Wav(string fpath,string folderpath)
        {
            string fileName = Path.GetFileNameWithoutExtension(fpath);
            //輸出的檔案
            string wav_file_path = Path.Combine(folderpath, fileName + ".wav");
            // wav
            //Mp3FileReader專門mp3 MediaFoundationReader常見音訊
            using (var reader = new MediaFoundationReader(fpath))

            //會留原本音檔
            using (var writer = new WaveFileWriter(wav_file_path, reader.WaveFormat))
            {
                reader.CopyTo(writer);
            }
            fpath = wav_file_path;
            return fpath;
        }

        //切割wav檔案
        public string CutWavFile(string fpath)
        {
            string outputDirectory = Server.MapPath(@"~\cutWav\");

            // 删除文件夹及其内容
            Directory.Delete(outputDirectory, true);

            // 重新创建文件夹
            Directory.CreateDirectory(outputDirectory);

            int chunkSizeInSeconds = 2700; // 每個小檔案的秒數

            //切歌成小檔案
            using (var reader = new WaveFileReader(fpath))
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
            return outputDirectory;
        }

////------------------------------------------------------------------------------------------------------------        

        ////語音檔辨識 語音檔辨識
        [HttpPost]
        //[ValidateAntiForgeryToken]
        public async Task<ActionResult> Translate(string filename)
        {
            var stopRecogintion = new TaskCompletionSource<int>();
            string content = "";
            TextResult.Clear();
            //前端傳過來的名稱
            string folderPath = Server.MapPath("~/UploadedFiles/");
            string filePath = Path.Combine(folderPath, filename);
            if (System.IO.File.Exists(filePath)) //找出檔案 是存在的
            {
                var speechConfig = SpeechConfig.FromSubscription(speechKey, speechRegion);
                speechConfig.SpeechRecognitionLanguage = "zh-tw";

                // 在這裡獲取副檔名
                string audioExtension = Path.GetExtension(filePath).ToLower();
                // 檔案類型判斷 
                if (audioExtension == ".mp3" || audioExtension == ".m4a")
                {
                    filePath = CoverTo_Wav(filePath, folderPath);
                }

                TimeSpan maxDuration = TimeSpan.FromMinutes(40); // 設定最大允許的時間長度

                using (var reader = new AudioFileReader(filePath))
                {
                    TimeSpan duration = reader.TotalTime;
                    Console.WriteLine($"音訊檔案長度: {duration}");

                    //判斷當檔案大於多少時就要分割音檔
                    if (duration > maxDuration)
                    {

                        Console.WriteLine("音訊檔案過長，進行切割...");
                        //檔案分割
                        string outputDirectory = CutWavFile(filePath);
                        //讀取分割後的全部檔案 在cutWav資料夾
                        string[] files = Directory.GetFiles(outputDirectory, "chunk_*.wav");


                        // 對檔案名稱進行自定義的數字排序
                        Array.Sort(files, new NumericComparer());
                        foreach (var file in files)
                        {
                            //進行語音轉文字
                            await SpeackToText(file,speechConfig);
                        }
                        TrascribeTxt.Clear();
                        await CutStringFile_CovertText(TextResult.ToString());
                        await TranscribeSpeechFinal(TrascribeTxt.ToString());
                    }
                    else
                    {
                        TrascribeTxt.Clear();
                        await SpeackToText(filePath,speechConfig);
                        await TranscribeSpeechFinal(TextResult.ToString());
                    }

                    ViewBag.transalte = TextResult.ToString();
                    //await TranscribeSpeech(TextResult.ToString());
                    content = TextResult.ToString()+TrascribeTxt.ToString();
                    WriteStringToWordAndSave(content);
                }
            }
            return Content(content, "text/plain");
        }

        [HttpPost]
        //單純語音轉文字
        public async Task SpeackToText(string FilePath,SpeechConfig config)
        {
            using (var reader = new AudioFileReader(FilePath))
            {
                TimeSpan duration = reader.TotalTime;
                int durationInSeconds = (int)duration.TotalSeconds; // 將 TimeSpan 轉換為總秒數的整數

                var audioConfig = AudioConfig.FromWavFileInput(FilePath);
                var speechRecognizer = new SpeechRecognizer(config, audioConfig);

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
                await speechRecognizer.StartContinuousRecognitionAsync();
                //await stopRecogintion.Task;
                //每個檔案變識幾秒或是分鐘.....
                await Task.Delay(TimeSpan.FromSeconds(durationInSeconds+20));//這裡設定在執行時，會跑到60分鐘才會結束
                                                          //await Task.WhenAny(stopRecogintion.Task);
                                                          //等待變識結束
                await speechRecognizer.StopContinuousRecognitionAsync();
                TextResult.Append("@");
            }
        }

        ////------------------------------------------------------------------------------------------------------------        

        ////即時錄音辨識 即時錄音介面
        [HttpGet]
        public ActionResult RecordFile()
        {
            return View();
        }

        ////即時錄音辨識 開始錄音
        [HttpPost]
        public ActionResult StartRecording()
        {
            MicroResult.Clear();
            // 指定要保存的語音檔路徑
            string folderPath = Server.MapPath("~/OutPutWavFile/");
            string outputPath = Path.Combine(folderPath, "output" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".wav");

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
            speechRecognizers.Recognized +=  (s, e) =>
            {
                if (e.Result.Reason == ResultReason.RecognizedSpeech)
                {
                    string text = e.Result.Text;
                    MicroResult.Append(text);
                    MicroText = MicroResult.ToString();
                    ViewBag.text = MicroText;
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
            if(isRecognizing)
            {
                // 停止录音和语音识别
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


////------------------------------------------------------------------------------------------------------------        
       
        //將讀出來的文字合併後在利用@符號分段落
        [HttpPost]
        public async Task CutStringFile_CovertText(string Alltxt)
        {
            // Find the nearest period for each "@" symbol
            List<string> segments = new List<string>();
            int startIndex = 0;
            string segment2 = "";
            while (startIndex < Alltxt.Length)
            {
                int atIndex = Alltxt.IndexOf('@', startIndex);
                int periodIndex = Alltxt.LastIndexOf('。', atIndex);

                if (periodIndex != -1)
                {

                    string segment1 = Alltxt.Substring(startIndex, periodIndex - startIndex + 1);
                    segment2 += segment1;
                    segment1 = segment2;
                    //string segment = Alltxt.Substring(atIndex + 1, periodIndex - atIndex);
                    segments.Add(segment1);
                    startIndex = atIndex + 1;
                }
                segment2 = Alltxt.Substring(periodIndex + 1, atIndex - periodIndex - 1);

                //string segment = Alltxt.Substring(atIndex + 1, periodIndex - atIndex);
            }

            // Print the segments
            foreach (string segment in segments)
            {
                Console.WriteLine(segment);
                TrascribeTxt.AppendLine();
                TrascribeTxt.AppendLine("段落重點:");
                var res =await TranscribeSpeeching(segment);
                Console.WriteLine(new string('-', 20));
            }
        }

        //產生會議記錄(切割檔的在cutWav資料夾
        [HttpPost]
        public async Task<ActionResult> TranscribeSpeeching(string text)
        {

            OpenAIClient client = new(new Uri("https://cshitinterngpt4.openai.azure.com/"), new AzureKeyCredential("71e9950da8c34bc7805520df08984c21"));
            var chatCompletionsOptions = new ChatCompletionsOptions()
            {
                Messages =
                {
                    new ChatMessage(ChatRole.System,"用繁體中文回答"),
                    new ChatMessage(ChatRole.User, text),
                    new ChatMessage(ChatRole.User,"做重點整理，並刪除'謝謝'、'呃'的字、且修正錯字"),
                    new ChatMessage(ChatRole.Assistant, "以下是已修改後的結果"),
                },
                //文本的「創造性」程度。這個參數控制了生成文本時模型選擇詞語的隨機程度
                //Temperature = 1,
                //在生成下一個詞語時，只考慮詞語的累積機率分佈中最高的部分，例如:1、0.1
                NucleusSamplingFactor = (float)0.1,
                // 存在懲罰，控制包含特定詞語的文本 較小的值會傾向於生成包含輸入提示中提到的詞語或主題的文本，而較大的值會減少這種傾向，0或是1比較適合。               
                PresencePenalty = 1,
                // 頻率懲罰，控制重複詞語的頻率，調整生成文本中各詞語的重複頻率，以產生更多多樣性的輸出，會有?產生 
                //FrequencyPenalty = 1,
                // 最大標記數，限制文本長度
                MaxTokens = 3000 //MaxTokens = 32768 這樣在返回的結果中，生成的文本將會有最多32768個標記，這會跟gpt的上限為準
            };
            try
            {
                Response<StreamingChatCompletions> response = await client.GetChatCompletionsStreamingAsync(
                             deploymentOrModelName: "CSHInternGPT4-32K", chatCompletionsOptions);

                StreamingChatCompletions streamingChatCompletions = response.Value;

                await foreach (StreamingChatChoice choice in streamingChatCompletions.GetChoicesStreaming())
                {
                    await foreach (ChatMessage message in choice.GetMessageStreaming())
                    {
                        TrascribeTxt.Append(message.Content);
                        //MicroResult.Append(message.Content);
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


        //產生會議記錄(將每個段落的摘要合併後，在跑一次摘要)
        [HttpPost]
        //[WebMethod(Description = "這是一個範例方法")]
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

                TrascribeTxt.AppendLine();
                TrascribeTxt.AppendLine("最終會議重點:");

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


        //產生會議記錄
        //[HttpPost]
        //public async Task<ActionResult> TranscribeSpeechFinal(string text)
        //{

        //    OpenAIClient client = new(new Uri("https://cshitinterngpt4.openai.azure.com/"), new AzureKeyCredential("71e9950da8c34bc7805520df08984c21"));
        //    var chatCompletionsOptions = new ChatCompletionsOptions()
        //    {
        //        Messages =
        //        {
        //            new ChatMessage(ChatRole.System, "紀錄會議紀錄的人"),
        //            new ChatMessage(ChatRole.User, text),
        //            //new ChatMessage(ChatRole.User, ,
        //            new ChatMessage(ChatRole.User, "統整重點並以會議紀錄的型式書寫"),
        //        },
        //        //文本的「創造性」程度。這個參數控制了生成文本時模型選擇詞語的隨機程度
        //        //Temperature = 1,
        //        //在生成下一個詞語時，只考慮詞語的累積機率分佈中最高的部分，例如:1、0.1
        //        NucleusSamplingFactor = (float)0.1,
        //        //存在懲罰，控制包含特定詞語的文本 較小的值會傾向於生成包含輸入提示中提到的詞語或主題的文本，而較大的值會減少這種傾向，0或是1比較適合。               
        //        PresencePenalty = 1,
        //        //頻率懲罰，控制重複詞語的頻率，調整生成文本中各詞語的重複頻率，以產生更多多樣性的輸出，會有?產生 
        //        //FrequencyPenalty = 1,
        //        // 最大標記數，限制文本長度
        //        MaxTokens = 3000 //MaxTokens = 32768 這樣在返回的結果中，生成的文本將會有最多32768個標記，這會跟gpt的上限為準
        //    };
        //    try
        //    {
        //        Response<StreamingChatCompletions> response = await client.GetChatCompletionsStreamingAsync(
        //                     deploymentOrModelName: "CSHInternGPT4-32K", chatCompletionsOptions);

        //        StreamingChatCompletions streamingChatCompletions = response.Value;

        //        TrascribeTxt.Append("會議重點整理");

        //        await foreach (StreamingChatChoice choice in streamingChatCompletions.GetChoicesStreaming())
        //        {
        //            await foreach (ChatMessage message in choice.GetMessageStreaming())
        //            {
        //                TrascribeTxt.Append(message.Content);
        //                //TextResult.Append(message.Content);
        //                Console.Write(message.Content);
        //            }
        //            Console.WriteLine();
        //        }
        //        //ViewBag.text = TextResult.ToString();
        //        string content = TrascribeTxt.ToString();
        //        //string savePath = @"~\UploadedFiles"; // 替换为您希望保存的路径和文件名
        //        //WriteStringToWordAndSave(content);
        //        ViewBag.txtRes = "摘要完成";
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine("An error occurred: " + ex.Message);
        //    }
        //    return View();
        //}



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

//////多人辨識------------------------------------------------------------------------------------------------------------        

        //以下為加入人聲
        //即時錄音轉譯(錄音檔轉譯)
        [DataContract]
        internal class VoiceSignature
        {
            [DataMember]
            public string Status { get; private set; }

            [DataMember]
            public VoiceSignatureData Signature { get; private set; }

            [DataMember]
            public string Transcription { get; private set; }
        }

        [DataContract]
        internal class VoiceSignatureData
        {
            internal VoiceSignatureData()
            { }

            internal VoiceSignatureData(int version, string tag, string data)
            {
                this.Version = version;
                this.Tag = tag;
                this.Data = data;
            }

            [DataMember]
            public int Version { get; private set; }

            [DataMember]
            public string Tag { get; private set; }

            [DataMember]
            public string Data { get; private set; }
        }

        public class Participant
        {
            public string WavFilePath { get; set; }
            public string SignatureJson { get; set; }
            public string Language { get; set; }
        }

        ////語音檔辨識 上傳錄音檔的介面
        [HttpGet]
        public async Task<ActionResult> SpeakerTranscribe()
        {
            //讀取資料夾
            string[] fpaths = Directory.GetFiles(Server.MapPath("~/UploadedFiles/"));
            List<FileModel> files = new List<FileModel>();
            foreach (string fpath in fpaths)
            {
                //if(fpath == "")
                //{
                //    files.Add(new FileModel { Filename = "" });                    ;
                //}
                files.Add(new FileModel { Filename = Path.GetFileName(fpath) });
            }
            return View(files);
        }

        ////語音檔辨識 上傳錄音檔
        [HttpPost]
        public ActionResult SpeakerTranscribe(HttpPostedFileBase audioFile)
        {
            try
            {
                if (audioFile == null)
                {
                    return View();
                }
                else if (audioFile.ContentLength > 0)
                {
                    //檢查檔案類型是否為wav, mp3 ,m4a
                    string[] allowedExtensions = { ".wav", ".mp3", ".m4a" };
                    string fileExtension = Path.GetExtension(audioFile.FileName).ToLower();
                    if (!allowedExtensions.Contains(fileExtension))
                    {
                        ViewBag.mess = "只能上傳wav、mp3、m4a檔";
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
                return RedirectToAction("SpeakerTranscribe"); //導向 UploadFile() function
            }
            catch
            {
                ViewBag.mess = "error";
                return View();
            }
        }


        [HttpPost]
        //建立設定檔
        private static async Task<VoiceSignature> CreateVoiceSignatureFromVoiceSample(string voiceSample, string subscriptionKey, string region)
        {
            byte[] fileBytes = System.IO.File.ReadAllBytes(voiceSample);
            var content = new ByteArrayContent(fileBytes);
            var client = new HttpClient();
            //client.Timeout = TimeSpan.FromSeconds(10); // 设置为您认为合适的超时时间
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", subscriptionKey);
            var response = await client.PostAsync($"https://signature.{region}.cts.speech.microsoft.com/api/v1/Signature/GenerateVoiceSignatureFromByteArray", content);
            var jsonData = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<VoiceSignature>(jsonData);
            // 处理响应
            return result;
        }

        [HttpPost]
        //將人聲到加入到對話當中
        private static async Task AddParticipantAsync(Participant participantInfo, string subscriptionKey, string serviceRegion, Conversation conversation)
        {
            try
            {
                var voiceSignature = await CreateVoiceSignatureFromVoiceSample(participantInfo.WavFilePath, subscriptionKey, serviceRegion);
                participantInfo.SignatureJson = JsonConvert.SerializeObject(voiceSignature.Signature);
                string Voicefilenm = Path.GetFileNameWithoutExtension(participantInfo.WavFilePath);
                var speaker = Microsoft.CognitiveServices.Speech.Transcription.Participant.From(Voicefilenm, participantInfo.Language, participantInfo.SignatureJson);
                await conversation.AddParticipantAsync(speaker);
            }
            catch (Exception ex)
            {
                // Handle the exception as needed
                Console.WriteLine($"Error adding participant: {ex.Message}");
            }
        }


        [HttpPost]
        //將單音道或雙聲道---> 八聲道(對話檔一定要轉檔)
        public static string CovertTo_Eight_Voice(string FilePath, string FolderPath)
        {
            string inputFile = FilePath; // 輸入的雙聲道or單聲道音訊檔案
            string fileName = Path.GetFileNameWithoutExtension(FilePath);
            //輸出的檔案
            string outputFile = Path.Combine(FolderPath, fileName + "_1" + ".wav");

            using (var reader = new AudioFileReader(inputFile))
            {
                var newFormat = new WaveFormat(reader.WaveFormat.SampleRate, 8); // 保留8个声道

                using (var writer = new WaveFileWriter(outputFile, newFormat))
                {
                    var buffer = new float[reader.WaveFormat.SampleRate * reader.WaveFormat.Channels]; // 一次读取一个样本帧

                    while (reader.Position < reader.Length)
                    {
                        int bytesRead = reader.Read(buffer, 0, buffer.Length);

                        // 保留第一个声道的数据，其他声道设置为静音
                        for (int sampleFrame = 0; sampleFrame < bytesRead / reader.WaveFormat.Channels; sampleFrame++)
                        {
                            for (int channel = 0; channel < 8; channel++)
                            {
                                if (channel == 0)
                                {
                                    writer.WriteSample(buffer[sampleFrame * reader.WaveFormat.Channels + channel]);
                                }
                                else
                                {
                                    writer.WriteSample(0); // 静音处理
                                }
                            }
                        }
                    }
                }
            }
            Console.WriteLine("轉換完成。");

            return outputFile;
        }


        //切割wav檔案，四分鐘切一次
        public string CutWavFile_speaker(string fpath)
        {
            string outputDirectory = Server.MapPath(@"~\cutWav\");

            // 删除文件夹及其内容
            Directory.Delete(outputDirectory, true);

            // 重新创建文件夹
            Directory.CreateDirectory(outputDirectory);

            int chunkSizeInSeconds = 240; // 每個小檔案的秒數

            //切歌成小檔案
            using (var reader = new WaveFileReader(fpath))
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
            return outputDirectory;
        }

        ////多人辨識  語音檔變識------------------------------------------------------------------------------------------------------------        

        [HttpPost]
        //語譯轉換
        public async Task<ActionResult> TranscribeConversationsAsync(string filename)
        {
            CognizeText.Clear();
            //await NameSet();
            //filename = @"D:\radio\katiesteve.wav";
            string folderPath = Server.MapPath("~/UploadedFiles/");
            string filePath = Path.Combine(folderPath, filename);

            var config = SpeechConfig.FromSubscription(subscriptionKey, serviceRegion);
            config.SpeechRecognitionLanguage = "zh-TW";
            config.SetProperty("ConversationTranscriptionInRoomAndOnline", "true");
            config.SetProperty("DifferentiateGuestSpeakers", "true"); //變這行執行時會出現錯誤
            //即時+非同步
            //config.SetServiceProperty("transcriptionMode", "RealTimeAndAsync", ServicePropertyChannel.UriQueryParameter);

            //第一種方式 直接宣告個人音檔
            //List<Participant> participantInfos = new List<Participant>
            //{ //簽名檔 單聲道
            //    new Participant{
            //        WavFilePath = @"D:\radio\家儀.wav",
            //        SignatureJson = "",
            //        Language = "zh-TW"
            //    },
            //    new Participant{
            //        WavFilePath = @"D:\radio\詠琪.wav",
            //        SignatureJson = "",
            //        Language = "zh-TW"
            //    },
            //    new Participant{
            //        WavFilePath = @"D:\radio\魏.wav",
            //        SignatureJson = "",
            //        Language = "zh-TW"
            //    },
            //    new Participant{
            //        WavFilePath = @"D:\radio\舒.wav",
            //        SignatureJson = "",
            //        Language = "zh-TW"
            //    },
            //    new Participant{
            //        WavFilePath = @"D:\radio\enrollment_audio_steve.wav",
            //        SignatureJson = "",
            //        Language = "zh-TW"
            //    },
            //    new Participant{
            //        WavFilePath = @"D:\radio\enrollment_audio_katie.wav",
            //        SignatureJson = "",
            //        Language = "zh-TW"
            //    },
            //    new Participant{
            //        WavFilePath = @"D:\radio\Taipei講者.wav",
            //        SignatureJson = "",
            //        Language = "zh-TW"
            //    },
            //    new Participant{
            //        WavFilePath = @"D:\radio\Taipei講者1.wav",
            //        SignatureJson = "",
            //        Language = "zh-TW"
            //    },
            //    new Participant{
            //        WavFilePath = @"D:\radio\Taipei講者2.wav",
            //        SignatureJson = "",
            //        Language = "zh-TW"
            //    },
            //     new Participant{
            //        WavFilePath = @"D:\radio\chinese2.wav",
            //        SignatureJson = "",
            //        Language = "zh-TW"
            //    },
            //};

            //第二種方式 透過呼叫Personal_AudioFile資料夾裡的檔案，宣告音檔
            List<Participant> participantInfos = new List<Participant>();
            string wavFolderPath = Server.MapPath("~/Personal_AudioFile/");// 資料夾路徑

            string[] wavFiles = Directory.GetFiles(wavFolderPath, "*.wav");

            foreach (string wavFilePath in wavFiles)
            {
                participantInfos.Add(new Participant
                {
                    WavFilePath = wavFilePath,
                    SignatureJson = "",
                    Language = "zh-TW"
                });
            }


            // 在這裡獲取副檔名
            string audioExtension = Path.GetExtension(filePath).ToLower();
            // 檔案類型判斷 
            if (audioExtension == ".mp3" || audioExtension == ".m4a")
            {
                filePath = CoverTo_Wav(filePath, folderPath);
            }

            TimeSpan maxDuration = TimeSpan.FromMinutes(4); // 設定最大允許的時間長度
                                                             //fpath8 = CovertTo_Eight_Voice(filePath, folderPath);

            using (var reader = new MediaFoundationReader(filePath)) //AudioFileReader
            {
                int channels = reader.WaveFormat.Channels;
                if (channels == 1)
                {
                    filePath = CovertTo_Eight_Voice(filePath, folderPath);
                }
                else if (channels == 2)
                {
                    filePath = CovertTo_Eight_Voice(filePath, folderPath);
                }
              
                //降躁
                //filePath = Denoiser(filePath, folderPath);
              
                
                // 如果音檔的位元數不是16位，則進行格式轉換
                TimeSpan duration = reader.TotalTime;
                Console.WriteLine($"音訊檔案長度: {duration}");


                if (duration > maxDuration)
                {

                    Console.WriteLine("音訊檔案過長，進行切割...");
                    //檔案分割
                    string outputDirectory = CutWavFile_speaker(filePath);
                    string[] files = Directory.GetFiles(outputDirectory, "chunk_*.wav");

                    // 對檔案名稱進行自定義的數字排序
                    Array.Sort(files, new NumericComparer());
                    foreach (var file in files)
                    {
                        await SpeakerDuration(file, config, participantInfos);
                    }
                    TrascribeTxt.Clear();
                    await MultipleCutStringFile_CovertText(CognizeText.ToString());
                    await TranscribeSpeechFinal(TrascribeTxt.ToString());
                }
                else
                {
                    //filePath = CovertTo_Eight_Voice(filePath, folderPath);
                    await SpeakerDuration(filePath, config, participantInfos);
                    TrascribeTxt.Clear();
                    await TranscribeSpeechFinal(CognizeText.ToString());
                }
            }
            var res = CognizeText.ToString() + TrascribeTxt.ToString();
            WriteStringToWordAndSave(res);

            return Content(res,"text/plain");
        }

        //[HttpPost]
        //將單音道或雙聲道---> 八聲道(對話檔一定要轉檔)
        //public static string CovertTo_Eight_Voice(string FilePath, string FolderPath)
        //{
        //    string inputFile = FilePath; // 輸入的雙聲道or單聲道音訊檔案
        //    string fileName = Path.GetFileNameWithoutExtension(FilePath);
        //    //輸出的檔案
        //    string outputFile = Path.Combine(FolderPath, fileName + "_1" + ".wav");

        //    //var outputFile = @"D:\radio\TaipeiMeet35min1.wav"; // 輸出的八聲道音訊檔案

        //    // 使用 FFmpeg 將雙聲道轉換成八聲道
        //    string ffmpegPath = @"C:\ffmpeg-6.0-full_build\bin\ffmpeg.exe"; // FFmpeg 執行檔路徑
        //    //string command = $"-i \"{inputFile}\" -ac 8 \"{outputFile}\""; // 轉換指令
        //    //string command = $"-i \"{inputFile}\" -filter_complex \"[0:a]pan=8c|c0=c0|c1=c0|c2=c0|c3=c0|c4=c0|c5=c0|c6=c0|c7=c0[aout]\" -map \"[aout]\" -ac 8 \"{outputFile}\"";
        //    //string command = $"-i \"{inputFile}\" -ac 8 -filter_complex \"[0:a]channelmap=channel_layout=octagonal:map=FL[aout]\" -c:a pcm_s16le \"{outputFile}\"";
        //    //string command = $"-i \"{inputFile}\" -filter_complex \"[0:a]channelmap=channel_layout=octagonal:map=FL|FL|FL|FL|FL|FL|FL|FL[aout]\" -c:a pcm_s16le \"{outputFile}\"";
        //    string command = $"-i \"{inputFile}\" -ac 8 -filter_complex \"[0:a]aevalsrc=0:d=0.0[s0];[0:a][s0]amerge=inputs=2[aout]\" -map \"[aout]\" -c:a pcm_s16le \"{outputFile}\"";

        //    ProcessStartInfo processInfo = new ProcessStartInfo(ffmpegPath)
        //    {
        //        Arguments = command,
        //        CreateNoWindow = true,
        //        RedirectStandardOutput = true,
        //        RedirectStandardError = true,
        //        UseShellExecute = false,
        //    };

        //    Process process = new Process { StartInfo = processInfo };
        //    process.Start();
        //    process.WaitForExit();

        //    if (process.ExitCode == 0)
        //    {
        //        Console.WriteLine("轉換成功。");
        //    }
        //    return outputFile;
        //}



        [HttpPost]
        public static string Denoiser(string fpath,string folderpath)
        {
            string fileName = Path.GetFileNameWithoutExtension(fpath);
            //輸出的檔案
            string outputFile = Path.Combine(folderpath, fileName + "_降躁" + ".wav");
            using (var reader = new AudioFileReader(fpath))
            {
                var fftSize = 4096; // FFT的大小，根據需要進行調整
                var noiseReductionFactor = 0.6; // 調整此因子以控制降噪程度

                using (var writer = new WaveFileWriter(outputFile, reader.WaveFormat))
                {
                    var buffer = new float[fftSize];
                    int bytesRead;

                    while ((bytesRead = reader.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        // 執行Spectral Subtraction降噪
                        for (int i = 0; i < buffer.Length; i++)
                        {
                            buffer[i] = (float)(buffer[i] * (1 - noiseReductionFactor));
                        }

                        // 將降噪後的音訊寫入輸出檔案
                        writer.WriteSamples(buffer, 0, bytesRead);
                    }

                }
            }
            return outputFile;
        }



        [HttpPost]
        //語音轉文字 (加人聲)
        public async Task SpeakerDuration(string FilePath, SpeechConfig config, List<Participant> participantInfos)
        {
            var stopRecognition = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
            var meetingID = Guid.NewGuid().ToString();

            using (var audioInput = AudioStreamReader.OpenWavFile(FilePath)) //AudioConfig.FromWavFileInput(filename)
            {
                using (var conversation = await Conversation.CreateConversationAsync(config, meetingID))
                {
                    // Create a conversation transcriber using audio stream input
                    using (var conversationTranscriber = new ConversationTranscriber(audioInput))
                    {
                        var reader = new MediaFoundationReader(FilePath);
                        TimeSpan duration = reader.TotalTime;
                        int durationInSeconds = (int)duration.TotalSeconds; // 將 TimeSpan 轉換為總秒數的整數

                        var tasks = new List<Task>();
                        //將人聲加入對話中
                        foreach (var participantInfo in participantInfos)
                        {
                            ////建立設定檔
                            //var voiceSignature = await GetVoiceSignatureString(participantInfo.WavFilePath, subscriptionKey, serviceRegion);
                            //participantInfo.SignatureJson = JsonConvert.SerializeObject(voiceSignature.Signature);
                            //string Voicefilenm = Path.GetFileNameWithoutExtension(participantInfo.WavFilePath);
                            //var speaker = Microsoft.CognitiveServices.Speech.Transcription.Participant.From(Voicefilenm, participantInfo.Language, participantInfo.SignatureJson);
                            //await conversation.AddParticipantAsync(speaker);
                            tasks.Add(AddParticipantAsync(participantInfo, subscriptionKey, serviceRegion, conversation));
                        }

                        await Task.WhenAll(tasks);
                        bool isAudioProcessingComplete = false;
                        var processingCompletionSource = new TaskCompletionSource<bool>();
                        // Join to the conversation.
                        await conversationTranscriber.JoinConversationAsync(conversation);
                        //CognizeText.Clear();
                        conversationTranscriber.Transcribed += (s, e) =>
                        {
                            if(!isAudioProcessingComplete)
                            {
                                if (e.Result.Text.Length > 0 && e.Result.Duration.TotalSeconds > 1.0 && e.Result.Reason == ResultReason.RecognizedSpeech) //e.Result.Reason == ResultReason.RecognizedSpeech
                                {
                                    //TransTxt = $"{ e.Result.UserId}:Text ={ e.Result.Text}";
                                    //coservationTxt.Append(TransTxt);
                                    //Console.WriteLine($"{e.Result.UserId}:Text={e.Result.Text}, Time={e.Result.Duration}");
                                    //Console.WriteLine($"TRANSCRIBED: Text={e.Result.Text} SpeakerId={e.Result.UserId}");
                                    //if (e.Result.UserId != "Unidentified")
                                    //{
                                    string cognizetxt = $"{ e.Result.UserId}:Text ={ e.Result.Text}\n";
                                    CognizeText.Append(cognizetxt);
                                    Console.WriteLine($"{e.Result.UserId}:Text={e.Result.Text}, Time={e.Result.Duration}");
                                    //}
                                }
                                else if (e.Result.Reason == ResultReason.NoMatch)
                                {
                                    Console.WriteLine($"NOMATCH: Speech could not be recognized.");
                                }
                            }
                        };
                        conversationTranscriber.SessionStopped += (s, e) =>
                        {
                            Console.WriteLine($"\n会话已停止事件。会话ID={e.SessionId}");
                            Console.WriteLine("\n停止识别。");
                            stopRecognition.TrySetResult(0);
                            isAudioProcessingComplete = true; // 设置标志以指示音频处理已完成。
                            processingCompletionSource.SetResult(true);
                        };
                        //var result = await GetRecognizerResult(conversationTranscriber, meetingID);

                        await conversationTranscriber.StartTranscribingAsync().ConfigureAwait(false);

                        //// Waits for completion.
                        //// Use Task.WaitAny to keep the task rooted.
                        ////await Task.WhenAny(stopRecognition.Task); //用這個會跑到當掉
                        //await Task.Delay(TimeSpan.FromSeconds(500)); //不太理解，為什麼用這個就可以跑得出來
                        await processingCompletionSource.Task; // 等待音頻處理完成
                        //// Stop transcribing the conversation.
                        await conversationTranscriber.StopTranscribingAsync().ConfigureAwait(false);
                        //CognizeText.Append("@");
                    }
                }
            }
        }


        [HttpPost]
        //多人辨識 分割段落
        public async Task MultipleCutStringFile_CovertText(string Alltxt)
        {
            // Find the nearest period for each "@" symbol
            int maxCharactersPerSegment = 20000;
            int currentCharacterCount = 0;
            StringBuilder currentSegment = new StringBuilder();
            List<string> segmentedArticles = new List<string>();

            foreach (char c in Alltxt)
            {
                currentSegment.Append(c);
                currentCharacterCount++;

                if (c == '。' && currentCharacterCount >= maxCharactersPerSegment)
                {
                    segmentedArticles.Add(currentSegment.ToString());
                    currentSegment.Clear();
                    currentCharacterCount = 0;
                }
            }

            // 處理最後一個段落
            if (currentSegment.Length > 0)
            {
                segmentedArticles.Add(currentSegment.ToString());
            }

            for (int i = 0; i < segmentedArticles.Count; i++)
            {
                Console.WriteLine($"段落 {i + 1}:\n{segmentedArticles[i]}\n");
                await TranscribeSpeeching(segmentedArticles[i]);
            }
        }

        ////多人辨識  即時語音變識------------------------------------------------------------------------------------------------------------        
        [HttpPost]
        public async Task<ActionResult> SpeakerStartRecord()
        {
            CognizeText.Clear();
            //audioConfig = AudioConfig.FromDefaultMicrophoneInput();
            var meetingID = Guid.NewGuid().ToString();

            List<Participant> participantInfos = new List<Participant>
            { //簽名檔 單聲道
                new Participant{
                    WavFilePath = @"D:\radio\me.wav",
                    SignatureJson = "",
                    Language = "zh-TW"
                },
                new Participant{
                    WavFilePath = @"D:\radio\詠.wav",
                    SignatureJson = "",
                    Language = "zh-TW"
                },
                new Participant{
                    WavFilePath = @"D:\radio\魏.wav",
                    SignatureJson = "",
                    Language = "zh-TW"
                },
                new Participant{
                    WavFilePath = @"D:\radio\舒.wav",
                    SignatureJson = "",
                    Language = "zh-TW"
                },
                new Participant{
                    WavFilePath = @"D:\radio\enrollment_audio_steve.wav",
                    SignatureJson = "",
                    Language = "zh-TW"
                },
                new Participant{
                    WavFilePath = @"D:\radio\enrollment_audio_katie.wav",
                    SignatureJson = "",
                    Language = "zh-TW"
                },
                new Participant{
                    WavFilePath = @"D:\radio\Taipei講者.wav",
                    SignatureJson = "",
                    Language = "zh-TW"
                },
                new Participant{
                    WavFilePath = @"D:\radio\Taipei講者1.wav",
                    SignatureJson = "",
                    Language = "zh-TW"
                },
                new Participant{
                    WavFilePath = @"D:\radio\Taipei講者2.wav",
                    SignatureJson = "",
                    Language = "zh-TW"
                },
                 new Participant{
                    WavFilePath = @"D:\radio\chinese2.wav",
                    SignatureJson = "",
                    Language = "zh-TW"
                },
            };


            // 指定要保存的語音檔路徑
            string folderPath = Server.MapPath("~/OutPutWavFile/");
            string outputPath = Path.Combine(folderPath, "output" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".wav");

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

            using (var conversation = await Conversation.CreateConversationAsync(config, meetingID))
            {
                // 確保 speechRecognizers 不為 null
                if (conversationTranscriber == null)
                {
                    // 初始化 speechRecognizers，使用相應的 speechConfig 和 audioConfig
                    conversationTranscriber = new ConversationTranscriber(audioConfig);
                }
                var tasks = new List<Task>();
                //將人聲加入對話中
                foreach (var participantInfo in participantInfos)
                {
                    ////建立設定檔
                    //var voiceSignature = await GetVoiceSignatureString(participantInfo.WavFilePath, subscriptionKey, serviceRegion);
                    //participantInfo.SignatureJson = JsonConvert.SerializeObject(voiceSignature.Signature);
                    //string Voicefilenm = Path.GetFileNameWithoutExtension(participantInfo.WavFilePath);
                    //var speaker = Microsoft.CognitiveServices.Speech.Transcription.Participant.From(Voicefilenm, participantInfo.Language, participantInfo.SignatureJson);
                    //await conversation.AddParticipantAsync(speaker);
                    tasks.Add(AddParticipantAsync(participantInfo, subscriptionKey, serviceRegion, conversation));
                }

                await Task.WhenAll(tasks);

                // Join to the conversation.
                await conversationTranscriber.JoinConversationAsync(conversation);

                conversationTranscriber.Transcribed += (s, e) =>
                {
                    if (e.Result.Text.Length > 0) //e.Result.Reason == ResultReason.RecognizedSpeech
                    {
                        //TransTxt = $"{ e.Result.UserId}:Text ={ e.Result.Text}";
                        //coservationTxt.Append(TransTxt);
                        //Console.WriteLine($"{e.Result.UserId}:Text={e.Result.Text}, Time={e.Result.Duration}");
                        //Console.WriteLine($"TRANSCRIBED: Text={e.Result.Text} SpeakerId={e.Result.UserId}");
                        //if (e.Result.UserId != "Unidentified")
                        //{
                        string cognizetxt = $"{ e.Result.UserId}:Text ={ e.Result.Text}\n";
                        CognizeText.Append(cognizetxt);
                        Console.WriteLine($"{e.Result.UserId}:Text={e.Result.Text}, Time={e.Result.Duration}");
                        //}
                    }
                    else if (e.Result.Reason == ResultReason.NoMatch)
                    {
                        Console.WriteLine($"NOMATCH: Speech could not be recognized.");
                    }
                };
                // 开始录音
                waveInEvent.StartRecording();
                isRecognizing = true;
                //var result = await GetRecognizerResult(conversationTranscriber, meetingID);
                //開始語音變識
                conversationTranscriber.StartTranscribingAsync().Wait();
            }
            return Json(new { message = "錄音開始" });

        }

        [HttpPost]
        public async Task<ActionResult> SpeakerStopRecord()
        {
            if (isRecognizing)
            {
                // 停止录音和语音识别
                waveInEvent.StopRecording();
                waveFileWriter?.Dispose();
                // 停止語音變識
                await conversationTranscriber.StopTranscribingAsync().ConfigureAwait(false);
                // 清理资源
                conversationTranscriber.Dispose();
                isRecognizing = false;
            }
            TrascribeTxt.Clear();
            await TranscribeSpeechFinal(CognizeText.ToString());
            var val = TrascribeTxt.ToString();
            WriteStringToWordAndSave(val);
            //return Json(new { message = MicroText });
            return Content(val, "text/plain");
        }


        //////上傳個人音檔-----------------------------------------------------------------------------------------------------------        

        ////語音檔辨識 上傳錄音檔的介面
        [HttpGet]
        public ActionResult UploadPersonal_AudioFile()
        {
            //讀取資料夾
            string[] fpaths = Directory.GetFiles(Server.MapPath("~/Personal_AudioFile"));
            List<FileModel> files = new List<FileModel>();
            foreach (string fpath in fpaths)
            {
                files.Add(new FileModel { Filename = Path.GetFileName(fpath) });
            }
            return View(files);
        }

        ////語音檔辨識 上傳錄音檔
        [HttpPost]
        public ActionResult UploadPersonal_AudioFile(HttpPostedFileBase audioFile)
        {
            try
            {
                if (audioFile == null)
                {
                    return View();
                }
                else if (audioFile.ContentLength > 0)
                {
                    //檢查檔案類型是否為wav, mp3 ,m4a
                    string[] allowedExtensions = { ".wav", ".mp3", ".m4a" };
                    string fileExtension = Path.GetExtension(audioFile.FileName).ToLower();
                    if (!allowedExtensions.Contains(fileExtension))
                    {
                        ViewBag.Message = "只能上傳wav、mp3、m4a檔";
                        return View();
                    }
                    string FileName = Path.GetFileName(audioFile.FileName);
                    string FilePath = Path.Combine(Server.MapPath("~/Personal_AudioFile"), FileName);
                    audioFile.SaveAs(FilePath);
                    string[] fpaths = Directory.GetFiles(Server.MapPath("~/Personal_AudioFile"));
                    List<FileModel> files = new List<FileModel>();
                    foreach (string fpath in fpaths)
                    {
                        files.Add(new FileModel { Filename = Path.GetFileName(fpath) });
                    }
                }
                //ViewBag.Message = "檔案上傳成功!";
                //導向 Upload() function 
                return RedirectToAction("UploadPersonal_AudioFile");
            }
            catch (Exception ex)
            {
                ViewBag.Message = "檔案上傳失敗: " + ex.Message;
                return View();
            }
        }

    }
}
//public async Task NameSet()
//{
//    var voiceSignatureWaveFileUser1 = @"D:\radio\舒.wav";
//    var voiceSignatureWaveFileUser2 = @"D:\radio\魏.wav";
//    //var conversationWaveFile = @"D:\radio\砂源對話3.wav";
//    // Create voice signature for the user1 and convert it to json string
//    var voiceSignature = await CreateVoiceSignatureFromVoiceSample(voiceSignatureWaveFileUser1, subscriptionKey, serviceRegion);
//    //var voiceSignature = CreateVoiceSignatureFromVoiceSample ("Guest1",subscriptionKey, serviceRegion);
//    voiceSignatureUser1 = JsonConvert.SerializeObject(voiceSignature.Signature);

//    // Create voice signature for the user2 and convert it to json string
//    voiceSignature = await CreateVoiceSignatureFromVoiceSample(voiceSignatureWaveFileUser2, subscriptionKey, serviceRegion);
//    //voiceSignature = CreateVoiceSignatureFromVoiceSample("Guest2", subscriptionKey, serviceRegion);
//    voiceSignatureUser2 = JsonConvert.SerializeObject(voiceSignature.Signature);

//}

//private static async Task<List<string>> GetRecognizerResult(ConversationTranscriber recognizer, string conversationId)
//{
//    List<string> recognizedText = new List<string>();
//    recognizer.Transcribed += (s, e) =>
//    {
//        if (e.Result.Text.Length > 0)
//        {
//            recognizedText.Add(e.Result.Text);
//            //Console.WriteLine($"TRANSCRIBED: {e.Result.Text}, {e.Result.ResultId}, {e.Result.Reason}, {e.Result.UserId}, {e.Result.OffsetInTicks}, {e.Result.Duration}");
//            Console.WriteLine($"{e.Result.UserId}: {e.Result.Text}, Time:{e.Result.Duration}");
//        }

//    };
//    await CompleteContinuousRecognition(recognizer, conversationId);
//    recognizer.Dispose();
//    return recognizedText;
//}

//private static async Task CompleteContinuousRecognition(ConversationTranscriber recognizer, string conversationId)
//{
//    var stopRecognition = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);

//    TaskCompletionSource<int> taskCompletionSource = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);

//    recognizer.SessionStopped += (s, e) =>
//    {
//        taskCompletionSource.TrySetResult(0);
//    };

//    recognizer.Canceled += (s, e) =>
//    {
//        Console.WriteLine($"CANCELED: Reason={e.Reason}");
//        if (e.Reason == CancellationReason.Error)
//        {
//            Console.WriteLine($"CANCELED: ErrorCode={e.ErrorCode}");
//            Console.WriteLine($"CANCELED: ErrorDetails={e.ErrorDetails}");
//            Console.WriteLine($"CANCELED: Did you update the subscription info?");
//            throw new System.ApplicationException("${e.ErrorDetails}");
//        }
//        taskCompletionSource.TrySetResult(0);
//    };
//    await recognizer.StartTranscribingAsync().ConfigureAwait(false);
//    //await Task.WhenAny(new[] { stopRecognition.Task });
//    await Task.Delay(TimeSpan.FromSeconds(300));

//    //await Task.WhenAny(taskCompletionSource.Task);
//    //await Task.WhenAny(taskCompletionSource.Task, Task.Delay(TimeSpan.FromSeconds(10)));
//    await recognizer.StopTranscribingAsync().ConfigureAwait(false);
//}
