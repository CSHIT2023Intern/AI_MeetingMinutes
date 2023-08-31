using Azure.AI.OpenAI;
using Azure;
using azureMVC.Models;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace azureMVC.Controllers
{   
    public class UploadController : Controller
    {        
        //錄音檔列表
        [HttpGet]
        public ActionResult Upload()
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
        public ActionResult Upload(HttpPostedFileBase audioFile)
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
            return RedirectToAction("Upload"); //導向 Upload() function           
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

        //切割wav檔案
        public string CutWavFile(string fpath)
        {
            string outputDirectory = Server.MapPath(@"~/CutWav");

            // 删除文件夹及其内容
            Directory.Delete(outputDirectory, true);

            //重建資料夾
            Directory.CreateDirectory(outputDirectory);

            // 每個小檔案的秒數
            int chunkSizeInSeconds = 10; 

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

        // 將切割後的檔案按照順序排序
        public class NumericComparer : IComparer<string>
        {
            public int Compare(string x, string y)
            {
                int xNum = int.Parse(Path.GetFileNameWithoutExtension(x).Split('_')[1]);
                int yNum = int.Parse(Path.GetFileNameWithoutExtension(y).Split('_')[1]);
                return xNum.CompareTo(yNum);
            }
        }

        //語音檔轉文字
        static StringBuilder TextResult = new StringBuilder(); //轉完的文字

        //語音辨識
        [HttpPost]
        public async Task<ActionResult> Translate(string filename)
        {
            var stopRecogintion = new TaskCompletionSource<int>();
            string content = "";
            TextResult.Clear();
            //前端傳過來的名稱
            string folderPath = Server.MapPath("~/UploadFile/");
            string filePath = Path.Combine(folderPath, filename);

            var speechConfig = SpeechConfig.FromSubscription("c9d3e6d440214af3bc175d4c31809a44", "eastasia");
            speechConfig.SpeechRecognitionLanguage = "zh-tw";

            // 在這裡獲取副檔名
            string audioExtension = Path.GetExtension(filePath).ToLower();
            // 檔案類型判斷 
            if (audioExtension == ".mp3" || audioExtension == ".m4a")
            {
                filePath = CoverTo_Wav(filePath, folderPath);
            }

            TimeSpan maxDuration = TimeSpan.FromMinutes(45); // 設定最大允許的時間長度

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


                    // 對檔案名稱進行排序
                    Array.Sort(files, new NumericComparer());
                    foreach (var file in files)
                    {
                        //進行語音轉文字
                        await SpeackToText(file, speechConfig);
                    }
                    await CutStringFile_CovertText(TextResult.ToString());
                    await TranscribeSpeechFinal(TrascribeTxt.ToString());
                }
                else
                {
                    await SpeackToText(filePath, speechConfig);
                    await TranscribeSpeechFinal(TextResult.ToString());
                }

                ViewBag.transalte = TextResult.ToString();
                //await TranscribeSpeech(TextResult.ToString());
                content = TextResult.ToString() + TrascribeTxt.ToString();
                WriteStringToWordAndSave(content);
            }

            return Content(content, "text/plain");
        }

        [HttpPost]
        //語音轉文字 (單純不加人聲)
        public async Task SpeackToText(string FilePath, SpeechConfig config)
        {
            using (var reader = new AudioFileReader(FilePath))
            {
                TimeSpan duration = reader.TotalTime;
                int durationInSeconds = (int)duration.TotalSeconds; // 將 TimeSpan 轉換為總秒數的整數

                var audioConfig = AudioConfig.FromWavFileInput(FilePath);
                var speechRecognizer = new SpeechRecognizer(config, audioConfig);


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
                await Task.Delay(TimeSpan.FromSeconds(durationInSeconds + 20));//這裡設定在執行時，會跑到60分鐘才會結束
                                                                               //await Task.WhenAny(stopRecogintion.Task);
                                                                               //等待變識結束
                await speechRecognizer.StopContinuousRecognitionAsync();
                TextResult.Append("@");
            }
        }

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
                var res = await TranscribeSpeeching(segment);
                Console.WriteLine(new string('-', 20));
            }
        }

        //產生會議記錄(切割的
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
                TrascribeTxt.Clear();
                //MicroResult.Clear();
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
                //string savePath = @"~\UploadFiles"; // 替换为您希望保存的路径和文件名
                //WriteStringToWordAndSave(content);
                ViewBag.txtRes = "摘要完成";
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occurred: " + ex.Message);
            }
            return View();
        }

        static StringBuilder TrascribeTxt = new StringBuilder(); //摘要(包涵 切割文字檔後跑的、直接轉完文字跑的)

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
                //string savePath = @"~\UploadFiles"; // 替换为您希望保存的路径和文件名
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