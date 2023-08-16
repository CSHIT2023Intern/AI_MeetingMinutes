using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.IO;
using System.Web.Mvc;
using System.Threading.Tasks;
using System.Text;
using System.Runtime.Serialization;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.CognitiveServices.Speech.Transcription;
using AzureMVC.Models;
using Azure.AI.OpenAI;
using Azure;
using Task = System.Threading.Tasks.Task;
using System.Net.Http;
using Newtonsoft.Json;
using helloworld;
using NAudio.Wave;
using System.Diagnostics;

namespace AzureMVC.Controllers
{
    public class UploadController : Controller
    {

        static string speechKey = Environment.GetEnvironmentVariable("SPEECH_KEY");//這裡因為我在cmd裡把SPEECH_KEY設為c9d3e6d440214af3bc175d4c31809a44
        static string speechRegion = Environment.GetEnvironmentVariable("SPEECH_REGION");//SPEECH_REGION為eastasia
        static string subscriptionKey = "";
        static string serviceRegion = "";
        static StringBuilder all_result = new StringBuilder();
        static StringBuilder TextResult = new StringBuilder();
        static StringBuilder MicroResult = new StringBuilder();
        static StringBuilder TrascribeTxt = new StringBuilder();
        static StringBuilder CognizeText = new StringBuilder();
        static string value, MicroText,fpath8;
        private bool isRecognizing = false;
        private SpeechRecognizer speechRecognizers;
        private SpeechConfig speechConfig, cognizeConfig;
        private AudioConfig audioConfig;
        private Task recTask;


        //將變數初始化
        public UploadController()
        {
            ////cognizeConfig = SpeechConfig.FromSubscription(speechKey, speechRegion);
            //speechConfig = SpeechConfig.FromSubscription(speechKey, speechRegion);
            //speechConfig.SpeechRecognitionLanguage = "zh-tw";
            //audioConfig = AudioConfig.FromDefaultMicrophoneInput();
            //speechRecognizers = new SpeechRecognizer(speechConfig, audioConfig);
        }

        // GET: Upload
        public ActionResult Index()
        {
            return View();
        }

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
                ViewBag.mess = "Success!";
                return RedirectToAction("UploadFile"); //導向 UploadFile() function
            }
            catch
            {
                ViewBag.mess = "error";
                return View();
            }
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

            int chunkSizeInSeconds = 300; // 每個小檔案的秒數

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
        

        ////語音檔辨識 語音檔辨識
        [HttpPost]
        //[ValidateAntiForgeryToken]
        public async Task<ActionResult> Translate(string filename)
        {
            var stopRecogintion = new TaskCompletionSource<int>();

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

                TimeSpan maxDuration = TimeSpan.FromMinutes(30); // 設定最大允許的時間長度

                using (var reader = new AudioFileReader(filePath))
                {
                    TimeSpan duration = reader.TotalTime;
                    Console.WriteLine($"音訊檔案長度: {duration}");

                    if (duration > maxDuration)
                    {

                        Console.WriteLine("音訊檔案過長，進行切割...");
                        //檔案分割
                        string outputDirectory = CutWavFile(filePath);
                        string[] files = Directory.GetFiles(outputDirectory, "chunk_*.wav");


                        // 對檔案名稱進行自定義的數字排序
                        Array.Sort(files, new NumericComparer());
                        foreach (var file in files)
                        {
                            await SpeackToText(file,speechConfig);
                        }
                    }
                    else
                    {
                       await SpeackToText(filePath,speechConfig);
                    }

                    ViewBag.transalte = TextResult.ToString();
                    await TranscribeSpeech(TextResult.ToString());
                    string content = TextResult.ToString();
                    //string savePath = @"~\UploadedFiles"; // 替换为您希望保存的路径和文件名
                    string savePath = @"~/TextWord/"; // 替换为您希望保存的路径和文件名
                    //WriteStringToWordAndSave(content, savePath);
                }
            }
            return Content(TextResult.ToString(), "text/plain");
        }
        
        
        //語音轉文字 (單純不加人聲)
        public  async Task SpeackToText(string FilePath,SpeechConfig config)
        {
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
            await Task.Delay(TimeSpan.FromSeconds(40));
            //await Task.WhenAny(stopRecogintion.Task);
            //等待變識結束
            await speechRecognizer.StopContinuousRecognitionAsync();
        }


        ////即時錄音辨識 即時錄音介面
        [HttpGet]
        public ActionResult RecordFile()
        {
            return View();
        }

        ////即時錄音辨識 開始即時錄音
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


        ////即時錄音辨識 停止錄音
        [HttpPost]
        public async Task<ActionResult> StopRecording()
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

        //產生會議記錄(切割檔)
        [HttpPost]
        public async Task<ActionResult> TransSpeechCut(string text)
        {
            //這裡要自己加上 endpoint、key
            OpenAIClient client = new(new Uri(""), new AzureKeyCredential(""));
            var chatCompletionsOptions = new ChatCompletionsOptions()
            {
                Messages =
                {
                new ChatMessage(ChatRole.System,"我是一個秘書要做會議紀錄"),
                new ChatMessage(ChatRole.Assistant,text),
                new ChatMessage(ChatRole.User,"將文字檔統整成摘要，並做成會議記錄的型式帶入參與的人"),
                //new ChatMessage(ChatRole.User,"可以幫統整重點嗎?"),
                //new ChatMessage(ChatRole.User,"可以做成會議摘要嗎?"),
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
                             deploymentOrModelName: "", chatCompletionsOptions);       //這裡要自己加上engine在""裡

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
                string content = TrascribeTxt.ToString();
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

        //產生會議記錄
        [HttpPost]
        public async Task<ActionResult> TranscribeSpeech(string text)
        {
            //這裡要自己加上 endpoint、key
            OpenAIClient client = new(new Uri(""), new AzureKeyCredential(""));
            var chatCompletionsOptions = new ChatCompletionsOptions()
            {
                Messages =
                {
                new ChatMessage(ChatRole.System,"我是一個秘書要做會議紀錄"),
                new ChatMessage(ChatRole.Assistant,text),
                new ChatMessage(ChatRole.User,"將文字檔統整成摘要，並做成會議記錄的型式帶入參與的人"),
                //new ChatMessage(ChatRole.User,"可以幫統整重點嗎?"),
                //new ChatMessage(ChatRole.User,"可以做成會議摘要嗎?"),
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
                             deploymentOrModelName: "", chatCompletionsOptions); //這裡要自己加上engine在""裡

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
                string content = TrascribeTxt.ToString();
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
        }


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


        [HttpPost]
        //語譯轉換
        public async Task<ActionResult> TranscribeConversationsAsync(string filename)
        {
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
           

            // 在這裡獲取副檔名
            string audioExtension = Path.GetExtension(filePath).ToLower();
            // 檔案類型判斷 
            if (audioExtension == ".mp3" || audioExtension == ".m4a")
            {
                filePath = CoverTo_Wav(filePath, folderPath);
            }

            TimeSpan maxDuration = TimeSpan.FromMinutes(30); // 設定最大允許的時間長度
                                                             //fpath8 = CovertTo_Eight_Voice(filePath, folderPath);
            filePath = CovertTo_Eight_Voice(filePath, folderPath);


            using (var reader = new MediaFoundationReader(filePath)) //AudioFileReader
            {
                TimeSpan duration = reader.TotalTime;
                Console.WriteLine($"音訊檔案長度: {duration}");

                if (duration > maxDuration)
                {

                    Console.WriteLine("音訊檔案過長，進行切割...");
                    //檔案分割
                    string outputDirectory = CutWavFile(filePath);
                    string[] files = Directory.GetFiles(outputDirectory, "chunk_*.wav");


                    // 對檔案名稱進行自定義的數字排序
                    Array.Sort(files, new NumericComparer());
                    foreach (var file in files)
                    {
                        await SpeakerDuration(file,config, participantInfos);
                    }
                }
                else
                {
                    //filePath = CovertTo_Eight_Voice(filePath, folderPath);
                    await SpeakerDuration(filePath, config, participantInfos);
                }
            }
            string savePath = @"~/TextWord"; // 替换为您希望保存的路径和文件名
            WriteStringToWordAndSave(CognizeText.ToString(), savePath);

            await TranscribeSpeech(CognizeText.ToString());
            return Content(CognizeText.ToString());
        }
        
        public static string CovertTo_Eight_Voice(string FilePath, string FolderPath)
        {
            string inputFile = FilePath; // 輸入的雙聲道or單聲道音訊檔案
            string fileName = Path.GetFileNameWithoutExtension(FilePath);
            //輸出的檔案
            string outputFile = Path.Combine(FolderPath, fileName+"_1"+ ".wav");

            //var outputFile = @"D:\radio\TaipeiMeet35min1.wav"; // 輸出的八聲道音訊檔案

            // 使用 FFmpeg 將雙聲道轉換成八聲道
            string ffmpegPath = @"C:\ffmpeg-6.0-full_build\bin\ffmpeg.exe"; // FFmpeg 執行檔路徑
            string command = $"-i \"{inputFile}\" -ac 8 \"{outputFile}\""; // 轉換指令

            ProcessStartInfo processInfo = new ProcessStartInfo(ffmpegPath)
            {
                Arguments = command,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
            };

            Process process = new Process { StartInfo = processInfo };
            process.Start();
            process.WaitForExit();

            if (process.ExitCode == 0)
            {
                Console.WriteLine("轉換成功。");
            }
            return outputFile;
        }

        //語音轉文字 (單純不加人聲)
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

                        //var result = await GetRecognizerResult(conversationTranscriber, meetingID);

                        await conversationTranscriber.StartTranscribingAsync().ConfigureAwait(false);

                        //// Waits for completion.
                        //// Use Task.WaitAny to keep the task rooted.
                        ////await Task.WhenAny(stopRecognition.Task); //用這個會跑到當掉
                        await Task.Delay(TimeSpan.FromSeconds(300)); //不太理解，為什麼用這個就可以跑得出來

                        //// Stop transcribing the conversation.
                        await conversationTranscriber.StopTranscribingAsync().ConfigureAwait(false);
                    }
                }
            }
        }
    }
}