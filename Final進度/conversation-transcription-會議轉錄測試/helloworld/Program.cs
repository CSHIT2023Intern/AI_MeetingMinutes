using System;
using System.IO;
using System.Net.Http;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Transcription;
using Newtonsoft.Json;

namespace helloworld
{
    class Program
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

        /// <summary>
        /// Class which defines VoiceSignatureData which is used when creating/adding participants
        /// </summary>
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

        //定義使用者聲音樣本
         private static string voiceSignatureUser1;
        private static string voiceSignatureUser2;
        private static string voiceSignatureUser3;
        private static string voiceSignatureUser4;

        //voiceSample是聲音樣本變數 讓主程式可以去多次使用 後面就是APIKEY跟區域
        private static async Task<VoiceSignature> CreateVoiceSignatureFromVoiceSample(string voiceSample, string subscriptionKey, string region)
        {
            byte[] fileBytes = File.ReadAllBytes(voiceSample); //讀取位於 voiceSample 路徑的檔案的所有位元組，將其讀取到 fileBytes 陣列中
            var content = new ByteArrayContent(fileBytes);
            var client = new HttpClient();  //讀取位於 voiceSample 路徑的檔案的所有位元組，將其讀取到 fileBytes 陣列中
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", subscriptionKey); //指定的 URL 發送 POST 請求，將 fileBytes 作為內容傳送。該請求將位元組內容提交給指定的 URL 以生成語音簽名。
            var response = await client.PostAsync($"https://signature.{region}.cts.speech.microsoft.com/api/v1/Signature/GenerateVoiceSignatureFromByteArray", content);
            // 語音簽名包含來自響應正文的 Signature json 結構的版本、標籤和數據鍵值。
            // 語音簽名格式示例： { "Version": <數字字符串或整數值>, "Tag": "string", "Data": "string" }
            var jsonData = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<VoiceSignature>(jsonData);
            return result;
        }

        public static async Task TranscribeConversationsAsync(string conversationWaveFile, string subscriptionKey, string region)
        {
            var config = SpeechConfig.FromSubscription(subscriptionKey, region);
            //這個設定參數是為了支援多人會議場景，讓語音轉錄系統可以處理多個與會者的語音，並將其轉換成文字資訊
            config.SetProperty("ConversationTranscriptionInRoomAndOnline", "true");

            config.SetProperty("DifferentiateGuestSpeakers", "true");


            //默認是英文但也可以指定
            config.SpeechRecognitionLanguage = "zh-tw";

            //TaskCreationOptions.RunContinuationsAsynchronously）是為了確保後續操作在非同步環境中運行，從而避免阻塞主要執行緒。這在需要確保非同步操作不會阻止主執行緒時是有用的，特別是在 UI 應用程序中。
            var stopRecognition = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);

            // 從 wav 文件或默認麥克風創建音頻流
            using (var audioInput = AudioStreamReader.OpenWavFile(conversationWaveFile))
            {
                //使用 Guid.NewGuid() 創建一個唯一的會議識別碼。
                var meetingID = Guid.NewGuid().ToString();
                //創建一個新的 Conversation 物件：使用創建的語音辨識配置和會議識別碼，創建一個 Conversation 物件，代表正在進行的會議。
                using (var conversation = await Conversation.CreateConversationAsync(config, meetingID))
                {
                    // Create a conversation transcriber using audio stream input
                    //創建一個會話轉錄器 ConversationTranscriber：使用之前創建的音頻輸入流創建一個會話轉錄器，該轉錄器將接收會議中的語音音頻。
                    using (var conversationTranscriber = new ConversationTranscriber(audioInput))
                    {
                        // Subscribe to events
                        //訂閱事件：為會話轉錄器的不同事件（例如 Transcribing、Transcribed、Canceled 等）設置事件處理程序，以便在事件發生時執行相應的操作。
                        conversationTranscriber.Transcribing += (s, e) =>
                        {
                            //Console.WriteLine($"TRANSCRIBING: Text={e.Result.Text} SpeakerId={e.Result.UserId}");
                            //Console.WriteLine("END");
                        };
                        conversationTranscriber.Canceled += (s, e) =>
                        {
                            Console.WriteLine($"CANCELED: Reason={e.Reason}");

                            if (e.Reason == CancellationReason.Error)
                            {
                                Console.WriteLine($"CANCELED: ErrorCode={e.ErrorCode}");
                                Console.WriteLine($"CANCELED: ErrorDetails={e.ErrorDetails}");
                                Console.WriteLine($"CANCELED: Did you set the speech resource key and region values?");
                                stopRecognition.TrySetResult(0);
                            }
                        };

                        conversationTranscriber.SessionStarted += (s, e) =>
                        {
                            Console.WriteLine($"\nSession started event. SessionId={e.SessionId}");
                        };

                        conversationTranscriber.SessionStopped += (s, e) =>
                        {
                            Console.WriteLine($"\nSession stopped event. SessionId={e.SessionId}");
                            Console.WriteLine("\nStop recognition.");
                            stopRecognition.TrySetResult(0);
                        };
                        //Console.WriteLine("STRAT");
                        conversationTranscriber.Transcribed += (s, e) =>
                        {
                            //if (e.Result.Reason == ResultReason.RecognizedSpeech)
                            //{
                            //Console.WriteLine(s);

                            //Console.WriteLine("s    ");
                            //Console.WriteLine(e);
                            //Console.WriteLine(ResultReason.RecognizedSpeech);
                            //Console.WriteLine("ER    ");
                            //Console.WriteLine(e.Result.Reason);
                            //Console.WriteLine("e    ");
                            Console.WriteLine($"{e.Result.UserId}:  {e.Result.Text} ");

                            //}
                            //else if (e.Result.Reason == ResultReason.NoMatch)
                            //{
                            //    Console.WriteLine($"NOMATCH: Speech could not be recognized.");
                            //}
                        };

                        // Add participants to the conversation.
                        // Voice signature needs to be in the following format:
                        // { "Version": <Numeric value>, "Tag": "string", "Data": "string" }
                        //創建與會者並加入會話：根據之前創建的語音簽名 
                        //創建表示會議參與者的 Participant 物件，然後將這些參與者添加到會話中。
                        var languageForUser1 = "zh-tw"; // For example "en-US"
                        var speakerA = Participant.From("家儀", languageForUser1, voiceSignatureUser1);
                        var languageForUser2 = "zh-tw"; // For example "en-US"
                        var speakerB = Participant.From("詠琪", languageForUser2, voiceSignatureUser2);
                        var languageForUser3 = "zh-TW"; // For example "en-US"
                        var speakerC = Participant.From("jahow", languageForUser3, voiceSignatureUser3);
                        var languageForUser4 = "zh-TW"; // For example "en-US"
                        var speakerD = Participant.From("yating", languageForUser4, voiceSignatureUser4);
                        await conversation.AddParticipantAsync(speakerA);
                        await conversation.AddParticipantAsync(speakerB);
                        await conversation.AddParticipantAsync(speakerC);
                        await conversation.AddParticipantAsync(speakerD);

                        // Join to the conversation.
                        //連接到會話並開始轉錄：將會話轉錄器連接到會話中，然後開始進行會話的語音轉錄。
                        await conversationTranscriber.JoinConversationAsync(conversation);

                        // Starts transcribing of thpe conversation. Uses StopTranscribingAsync() to stop transcribing when all participants leave.
                        //使用 Task.WaitAny 等待轉錄完成：等待轉錄器完成，使用 Task.WaitAny 方法確保在轉錄完成前保持任務處於活動狀態。
                        await conversationTranscriber.StartTranscribingAsync().ConfigureAwait(false);

                        //停止轉錄：在所有參與者離開會話後，停止轉錄並關閉會話轉錄器。
                        // Waits for completion.
                        // Use Task.WaitAny to keep the task rooted.
                        //Task.WaitAny(new[] { stopRecognition.Task });
                        await Task.Delay(1800000);

                        // Stop transcribing the conversation.
                        await conversationTranscriber.StopTranscribingAsync().ConfigureAwait(false);
                    }
                }
            }
        }

        static async Task Main()
        {
            var subscriptionKey = "2581928f7d5042e190f8fb24c94540a1";
            var serviceRegion = "eastus";

            // The input audio wave format for voice signatures is 16-bit samples, 16 kHz sample rate, and a single channel (mono).
            // The recommended length for each sample is between thirty seconds and two minutes.
            var voiceSignatureWaveFileUser1 = "家儀.wav";
            var voiceSignatureWaveFileUser2 = "詠琪.wav";
            var voiceSignatureWaveFileUser3 = "enrollment_audio_jahow.wav";
            var voiceSignatureWaveFileUser4 = "enrollment_audio_yating.wav";

            // This sample expects a wavfile which is captured using a supported devices (8 channel, 16kHz, 16-bit PCM)
            // See https://docs.microsoft.com/azure/cognitive-services/speech-service/speech-devices-sdk-microphone
            //var conversationWaveFile = "jaya.wav";
            var conversationWaveFile = "chunk_124.wav";

            // Create voice signature for the user1 and convert it to json string
            var voiceSignature = CreateVoiceSignatureFromVoiceSample(voiceSignatureWaveFileUser1, subscriptionKey, serviceRegion);
            voiceSignatureUser1 = JsonConvert.SerializeObject(voiceSignature.Result.Signature);

            // Create voice signature for the user2 and convert it to json string
            voiceSignature = CreateVoiceSignatureFromVoiceSample(voiceSignatureWaveFileUser2, subscriptionKey, serviceRegion);
            voiceSignatureUser2 = JsonConvert.SerializeObject(voiceSignature.Result.Signature);

            //// Create voice signature for the user3 and convert it to json string
            voiceSignature = CreateVoiceSignatureFromVoiceSample(voiceSignatureWaveFileUser3, subscriptionKey, serviceRegion);
            voiceSignatureUser3 = JsonConvert.SerializeObject(voiceSignature.Result.Signature);

            //// Create voice signature for the user4 and convert it to json string
            voiceSignature = CreateVoiceSignatureFromVoiceSample(voiceSignatureWaveFileUser4, subscriptionKey, serviceRegion);
            voiceSignatureUser4 = JsonConvert.SerializeObject(voiceSignature.Result.Signature);

            await TranscribeConversationsAsync(conversationWaveFile, subscriptionKey, serviceRegion);
            //Console.WriteLine("Please press <Return> to continue.");
            Console.ReadLine();
        }
    }
}