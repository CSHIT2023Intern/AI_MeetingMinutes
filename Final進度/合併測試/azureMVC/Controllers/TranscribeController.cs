using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.IO;
using System.Net.Http;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using Newtonsoft.Json;
using helloworld;
using Microsoft.CognitiveServices.Speech.Transcription;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using NAudio.Wave;
using System.Diagnostics;
using System.Text;
using Azure.AI.OpenAI;
using Azure;
using azureMVC.Models;

namespace azureMVC.Controllers
{
    //建立語音簽章
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

    //語音簽章樣本檔案
    public class Participant
    {
        public string WavFilePath { get; set; }
        public string SignatureJson { get; set; }
        public string Language { get; set; }
    }


    public class TranscribeController : Controller
    {
        // GET: Transcribe
        public ActionResult Transcribe()
        {
            //讀取資料夾
            string[] fpaths = Directory.GetFiles(Server.MapPath("~/UploadFile"));
            List<FileModel> files = new List<FileModel>();
            foreach (string fpath in fpaths)
            {
                files.Add(new FileModel { Filename = Path.GetFileName(fpath) });
            }
            return View(files);
        }

        //上傳檔案到資料夾
        [HttpPost]
        public ActionResult Transcribe(HttpPostedFileBase audioFile)
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
                string FilePath = Path.Combine(Server.MapPath("~/UploadFile"), FileName);
                audioFile.SaveAs(FilePath);
                string[] fpaths = Directory.GetFiles(Server.MapPath("~/UploadFile"));
                List<FileModel> files = new List<FileModel>();
                foreach (string fpath in fpaths)
                {
                    files.Add(new FileModel { Filename = Path.GetFileName(fpath) });
                }
            }
            return RedirectToAction("Transcribe"); 
        }

        //ApiKey
        static string subscriptionKey = "2581928f7d5042e190f8fb24c94540a1";
        static string serviceRegion = "eastus";

        //建立語音簽章
        [HttpPost]
        private static async Task<VoiceSignature> CreateVoiceSignatureFromVoiceSample(string voiceSample, string subscriptionKey, string region)
        {

            byte[] fileBytes = System.IO.File.ReadAllBytes(voiceSample);
            var content = new ByteArrayContent(fileBytes);
            var client = new HttpClient();  //讀取位於 voiceSample 路徑的檔案的所有位元組，將其讀取到 fileBytes 陣列中
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", subscriptionKey); //指定的 URL 發送 POST 請求，將 fileBytes 作為內容傳送。該請求將位元組內容提交給指定的 URL 以生成語音簽名。
            var response = await client.PostAsync($"https://signature.{region}.cts.speech.microsoft.com/api/v1/Signature/GenerateVoiceSignatureFromByteArray", content);
            var jsonData = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<VoiceSignature>(jsonData);
            return result;
        }       
        //轉檔( covert .mp3,.m4a file to .wav)
        public static string CoverTo_Wav(string fpath, string folderpath)
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

        //切割wav檔案
        public string CutWavFile(string fpath)
        {
            string outputDirectory = Server.MapPath(@"~/CutWav");

            // 删除文件夹及其内容
            Directory.Delete(outputDirectory, true);

            //重建資料夾
            Directory.CreateDirectory(outputDirectory);

            // 每個小檔案的秒數
            int chunkSizeInSeconds = 2700;

            //切割成小檔案
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
        // 排序切割音檔
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
        //將人聲到加入到對話
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
                                string cognizetxt = $"{e.Result.UserId}:Text ={e.Result.Text}\n";
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
                        await Task.Delay(TimeSpan.FromSeconds(30)); //不太理解，為什麼用這個就可以跑得出來

                        //// Stop transcribing the conversation.
                        await conversationTranscriber.StopTranscribingAsync().ConfigureAwait(false);
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

        static StringBuilder CognizeText = new StringBuilder();
        //語譯轉換
        [HttpPost]        
        public async Task<ActionResult> TranscribeConversationsAsync(string filename)
        {
            string folderPath = Server.MapPath("~/UploadFile/");
            string filePath = Path.Combine(folderPath, filename);

            var config = SpeechConfig.FromSubscription(subscriptionKey, serviceRegion);
            config.SpeechRecognitionLanguage = "zh-TW";
            config.SetProperty("ConversationTranscriptionInRoomAndOnline", "true");
            //config.SetProperty("DifferentiateGuestSpeakers", "true"); //變這行執行時會出現錯誤

            

            //讀語音簽章
            List<Participant> participantInfos = new List<Participant>();
            string wavFolderPath = Server.MapPath(@"~/VoiceSignature/"); ; // 資料夾路徑

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
            WriteStringToWordAndSave(CognizeText.ToString());

            await TranscribeSpeechFinal(CognizeText.ToString());
            return Content(CognizeText.ToString());
        }
        static StringBuilder TrascribeTxt = new StringBuilder(); 
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
                string folderPath = Server.MapPath("~/TextWord/");
                string filename = "摘要結果" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".docx";
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