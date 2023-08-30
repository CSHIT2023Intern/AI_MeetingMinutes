using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.IO;
using System.Threading.Tasks;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Azure.AI.OpenAI;
using static System.Environment;
using Azure;
using System.Text;
using Microsoft.Office.Interop.Word;
using Task = System.Threading.Tasks.Task;

namespace AzureMVC.Controllers
{
    public class HomeController : Controller
    {
        static StringBuilder TextResult = new StringBuilder();

        public async Task<ActionResult> Index()
        {
            var txt1="";
            

            OpenAIClient client = new(new Uri("https://cshitinternopenai.openai.azure.com/"), new AzureKeyCredential("0be4adcd512d4b09b7e44d50325f4bf9"));
            string txt = "詞 曲   李 宗 盛 詞 曲   李 宗 盛 故 事 情 裡 的 世 界 越 來 越 遠 的 道 別 雨 傘 上 下 了 雨 殘 影 還 是 黑 夜 我 用 眼 光 去 追 緊 聽 著 你 的 雷 在 車 窗 外 面 徘 徊 是 我 錯 失 的 機 會 雨 傘 的 方 位 跟 我 從 前 的 爭 執 接 近 你 腳 踏 後 腿 你 的 崩 潰 在 窗 外 淋 水 我 一 日 閒 眠 離 開 有 你 的 季 節 你 說 你 後 面 也 無 法 再 相 信 風 在 山 流 去 過 往 的 畫 面 卻 都 是 我 不 配 心 是 藏 匪 我 想 你 幾 位 你 為 我 故 事 情 裡 的 世 界 越 來 越 遠 的 道 別 雨 轉 身 向 北 成 年 還 是 很 美 我 用 眼 光 去 追 緊 聽 著 你 的 雷 在 車 窗 外 面 徘 徊 我 後 面 是 我 錯 失 的 機 會 雨 傘 的 方 位 跟 我 從 前 的 爭 執 接 近 你 腳 踏 後 腿 你 的 崩 潰 在 窗 外 淋 水 我 一 日 閒 眠 離 開 有 你 的 季 節 你 說 你 後 面 也 無 法 再 相 信 風 在 山 流 去 過 往 的 畫 面 卻 都 是 我 不 配 心 是 藏 匪 我 想 你 幾 位 我 一 日 閒 眠 離 開 有 你 的 季 節 風 向 旁 走 位 揮 轉 著 我 的 後 悔 我 這 束 長 眼 卻 甩 不 掉 靜 靜 的 睡 在 山 邊 心 是 藏 匪 我 想 你 幾 位 前 進 龍 壁 就 讓 初 次 安 靜 Z i t h e r   H a r p";
            var chatCompletionsOptions = new ChatCompletionsOptions()
            {
                Messages =
                {
                new ChatMessage(ChatRole.System,"我是一個秘書要做會議紀錄"),
                new ChatMessage(ChatRole.User,"可以幫我們統整重點"),
                new ChatMessage(ChatRole.Assistant,txt),
                new ChatMessage(ChatRole.User,"做成會議摘要"),
                //new ChatMessage(ChatRole.System,"You are a helpful assistant. You will tealk like a private."),
                //new ChatMessage(ChatRole.User,"Does Azure OpenAI support customer managed key?"),
                //new ChatMessage(ChatRole.Assistant,"Yes, customer managed keys are support by Azure OpenAI."),
                //new ChatMessage(ChatRole.User,"Do other Azure AI services support this too?")
                },
                MaxTokens = 700 //MaxTokens = 500000 這樣在返回的結果中，生成的文本將會有最多500000個標記
            };
            string deployment = "CSHITIntern"; //engine
            try
            {

                Response<StreamingChatCompletions> response = await client.GetChatCompletionsStreamingAsync(
                             deploymentOrModelName: "CSHITIntern", chatCompletionsOptions);

                StreamingChatCompletions streamingChatCompletions = response.Value;

                await foreach (StreamingChatChoice choice in streamingChatCompletions.GetChoicesStreaming())
                {
                    await foreach (ChatMessage message in choice.GetMessageStreaming())
                    {
                        TextResult.Append(message.Content);
                        Console.Write(message.Content);
                    }
                    Console.WriteLine();
                }
                ViewBag.text = TextResult.ToString();
                string tt = TextResult.ToString();
                string content = tt;
                //string savePath = @"~\UploadedFiles"; // 替换为您希望保存的路径和文件名
                string savePath = @"D:\中山醫實習\練習\AzureMVC\AzureMVC\UploadedFiles"; // 替换为您希望保存的路径和文件名
                WriteStringToWordAndSave(content, savePath);
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occurred: " + ex.Message);
            }
            return View();
        }

        public void WriteStringToWordAndSave(string content, string savePath)
        {
            // 创建一个新的 Word 应用程序对象
            var wordApp = new Microsoft.Office.Interop.Word.Application();

            // 创建一个新的 Word 文档
            var doc = wordApp.Documents.Add();

            try
            {
                // 将内容写入 Word 文档
                doc.Content.Text = content;

                // 保存 Word 文档到指定路径
                doc.SaveAs2(savePath);
            }
            catch (Exception ex)
            {
                // 处理异常（例如：文件保存失败等）
                // 记得在实际项目中添加适当的错误处理和日志记录
                Console.WriteLine("写入 Word 文档并保存失败：" + ex.Message);
            }
            finally
            {
                // 关闭 Word 文档和 Word 应用程序对象
                doc.Close();
                wordApp.Quit();
            }
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }

        public ActionResult Speech_to_text()
        {
            return View();
        }


        static string speechKey = Environment.GetEnvironmentVariable("c9d3e6d440214af3bc175d4c31809a44");
        static string speechRegion = Environment.GetEnvironmentVariable("eastasia");


        [HttpPost]
        public async Task<ActionResult> Transcribe(string audioFile)
        {
            var speechConfig = SpeechConfig.FromSubscription(speechKey, speechRegion);
            //speechConfig.SpeechRecognitionLanguage = "en-US";
            speechConfig.SpeechRecognitionLanguage = "zh-tw";
            //await FileToText(speechConfig);
            var audioConfig = AudioConfig.FromWavFileInput(audioFile);
            //var audioConfig = AudioConfig.FromWavFileInput("D:\\中山醫實習\\專案\\Practice\\video\\test.wav");
            var speechRecognizer = new SpeechRecognizer(speechConfig, audioConfig);
            var stopRecognition = new TaskCompletionSource<int>();
            var all_result = new List<string>();

            Console.WriteLine("Speech To Txt");


            speechRecognizer.Recognized += (s, e) =>
            {
                if (e.Result.Reason == ResultReason.RecognizedSpeech)
                {
                    string text = e.Result.Text;
                    all_result.Add(text);
                    ViewBag.text = text;
                    Console.WriteLine($"{text}");
                }
                else if (e.Result.Reason == ResultReason.NoMatch)
                {
                    Console.WriteLine($"NOMATCH: Speech could not be recognized.");
                }
            };
            await speechRecognizer.StartContinuousRecognitionAsync();
            speechConfig.SetProperty(PropertyId.Speech_SegmentationSilenceTimeoutMs, "2000");
            // Waits for completion. Use Task.WaitAny to keep the task rooted.

            Task.WaitAny(new[] { stopRecognition.Task });
            //// 将列表中的文本连接成一个串
            //string combinedText = string.Join(" ", all_result);
            //Console.WriteLine("Combined Text: " + combinedText);
            // Stops recognition.
            await speechRecognizer.StopContinuousRecognitionAsync().ConfigureAwait(false);


            return View();
        }
        //async static Task FileToText(SpeechConfig speechConfig)
        //{

        //    var audioConfig = AudioConfig.FromWavFileInput("D:\\中山醫實習\\專案\\Practice\\video\\test.wav");
        //    var speechRecognizer = new SpeechRecognizer(speechConfig, audioConfig);
        //    var stopRecognition = new TaskCompletionSource<int>();
        //    var all_result = new List<string>();

        //    Console.WriteLine("Speech To Txt");


        //    speechRecognizer.Recognized += (s, e) =>
        //    {
        //        if (e.Result.Reason == ResultReason.RecognizedSpeech)
        //        {
        //            string text = e.Result.Text;
        //            all_result.Add(text);
        //            Console.WriteLine($"{text}");
        //        }
        //        else if (e.Result.Reason == ResultReason.NoMatch)
        //        {
        //            Console.WriteLine($"NOMATCH: Speech could not be recognized.");
        //        }
        //    };
        //    await speechRecognizer.StartContinuousRecognitionAsync();
        //    speechConfig.SetProperty(PropertyId.Speech_SegmentationSilenceTimeoutMs, "2000");
        //    // Waits for completion. Use Task.WaitAny to keep the task rooted.

        //    Task.WaitAny(new[] { stopRecognition.Task });
        //    //// 将列表中的文本连接成一个串
        //    //string combinedText = string.Join(" ", all_result);
        //    //Console.WriteLine("Combined Text: " + combinedText);
        //    // Stops recognition.
        //    await speechRecognizer.StopContinuousRecognitionAsync().ConfigureAwait(false);


        //}

        //async static Task Main(string[] args)
        //{
        //    var speechConfig = SpeechConfig.FromSubscription(speechKey, speechRegion);
        //    //speechConfig.SpeechRecognitionLanguage = "en-US";
        //    speechConfig.SpeechRecognitionLanguage = "zh-tw";
        //    await FileToText(speechConfig);
        //}



    }
}