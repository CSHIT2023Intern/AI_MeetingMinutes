using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.IO;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.CognitiveServices.Speech.Transcription;
using Azure;
using System.Net.Http;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using System.Threading;
using System.Threading.Tasks;
using helloworld;
using System.Text;

namespace AzureMVC.Controllers
{
    public class TranscribeController : Controller
    {
        private static string voiceSignatureUser1, voiceSignatureWaveFileUser1;
        private static string voiceSignatureUser2, voiceSignatureWaveFileUser2;
       //這裡要自己加
        private static string subscriptionKey = "";
        private static string serviceRegion = "";
        static StringBuilder CognizeText = new StringBuilder();


        // GET: Transcribe
        public async Task<ActionResult> Index()
        {
            //voiceSignatureWaveFileUser2 = @"D:\radio\me.wav";
            //voiceSignatureWaveFileUser1 = @"D:\radio\詠.wav";
            voiceSignatureWaveFileUser2 = @"D:\radio\enrollment_audio_steve.wav";
            voiceSignatureWaveFileUser1 = @"D:\radio\enrollment_audio_katie.wav";


            var voiceSignature = await GetVoiceSignatureString(voiceSignatureWaveFileUser1, subscriptionKey, serviceRegion);
            //var voiceSignature = CreateVoiceSignatureFromVoiceSample ("Guest1",subscriptionKey, serviceRegion);
            voiceSignatureUser1 = JsonConvert.SerializeObject(voiceSignature.Signature);

            // Create voice signature for the user2 and convert it to json string
            voiceSignature = await GetVoiceSignatureString(voiceSignatureWaveFileUser2, subscriptionKey, serviceRegion);
            //voiceSignature = CreateVoiceSignatureFromVoiceSample("Guest2", subscriptionKey, serviceRegion);
            voiceSignatureUser2 = JsonConvert.SerializeObject(voiceSignature.Signature);

            //// Upload the audio to the service
            //string meetingId = await UploadAudioAndStartRemoteTranscription(subscriptionKey, serviceRegion);

            //// Poll the service 
            //TestRemoteTranscription(subscriptionKey, serviceRegion, meetingId);


            ////ViewBag.Message = "Your contact page.";
            //var res = await GetVoiceSignatureString(subscriptionKey, serviceRegion);
            var file = @"D:\radio\katiesteve.wav";
            //await TranscribeConversationsAsync(file, subscriptionKey, serviceRegion);
            return View();
        }

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

        //方法1. 
        [HttpPost]
        private static async Task<VoiceSignature> GetVoiceSignatureString(string file, string subscriptionKey, string region)
        {
            //voiceSignatureWaveFileUser1 = @"D:\radio\enrollment_audio_katie.wav";
            byte[] fileBytes = System.IO.File.ReadAllBytes(file);
            var content = new ByteArrayContent(fileBytes);
            var client = new HttpClient();
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", subscriptionKey);
            var response = await client.PostAsync($"https://signature.{region}.cts.speech.microsoft.com/api/v1/Signature/GenerateVoiceSignatureFromByteArray", content);

            var jsonData = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<VoiceSignature>(jsonData);
            return result;
        }

        private static  async Task AddParticipantAsync(Participant participantInfo, string subscriptionKey, string serviceRegion, Conversation conversation)
        {
            try
            {
                var voiceSignature = await GetVoiceSignatureString(participantInfo.WavFilePath, subscriptionKey, serviceRegion);
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
        //方法1. 
        public async Task<ActionResult> TranscribeConversationsAsync()
        {
            var filename = @"D:\radio\katiesteve.wav";


            var config = SpeechConfig.FromSubscription(subscriptionKey, serviceRegion);
            //config.SpeechRecognitionLanguage = "zh-TW";
            config.SetProperty("ConversationTranscriptionInRoomAndOnline", "true");
            config.SetProperty("DifferentiateGuestSpeakers", "true"); //變這行執行時會出現錯誤

            //即時+非同步
            //config.SetServiceProperty("transcriptionMode", "RealTimeAndAsync", ServicePropertyChannel.UriQueryParameter);

            var stopRecognition = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
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

            // Create an audio stream from a wav file or from the default microphone if you want to stream live audio from the supported devices
            using (var audioInput = AudioStreamReader.OpenWavFile(filename))
            {
                using (var conversation = await Conversation.CreateConversationAsync(config, meetingID))
                {
                    // Create a conversation transcriber using audio stream input
                    using (var conversationTranscriber = new ConversationTranscriber(audioInput))
                    {
                        //這裡為轉錄所輸出的文字
                        // Subscribe to events
                        //conversationTranscriber.Transcribing += (s, e) =>
                        //{
                        //    Console.WriteLine($"TRANSCRIBING: Text={e.Result.Text} SpeakerId={e.Result.UserId}");
                        //};

                        // Add participants to the conversation.
                        // Voice signature needs to be in the following format:
                        // { "Version": <Numeric value>, "Tag": "string", "Data": "string" }
                        //var languageForUser1 = "zh-TW"; // For example "en-US"
                        //string fileName1 = Path.GetFileNameWithoutExtension(voiceSignatureWaveFileUser1);
                        //var speakerA = Participant.From(fileName1, languageForUser1, voiceSignatureUser1);
                        //var languageForUser2 = "zh-TW"; // For example "en-US"
                        //string fileName2 = Path.GetFileNameWithoutExtension(voiceSignatureWaveFileUser2);
                        //var speakerB = Participant.From(fileName2, languageForUser2, voiceSignatureUser2);
                        //await conversation.AddParticipantAsync(speakerA);
                        //await conversation.AddParticipantAsync(speakerB);
                        var tasks = new List<Task>();
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
                            if (e.Result.Text.Length > 0)
                            {
                                //Console.WriteLine($"TRANSCRIBED: Text={e.Result.Text} SpeakerId={e.Result.UserId}");
                                Console.WriteLine($"{e.Result.UserId}:Text={e.Result.Text},Time");
                                string cognizetxt = $"{e.Result.UserId}:Text={e.Result.Text}\n";
                                CognizeText.Append(cognizetxt);

                                //if (e.Result.UserId != "Unidentified")
                                //{
                                //    string cognizetxt = $"{e.Result.UserId}:Text={e.Result.Text},Time:{e.Result.Duration}";
                                //    CognizeText.Append(cognizetxt);
                                //}
                            }
                            else if (e.Result.Reason == ResultReason.NoMatch)
                            {
                                Console.WriteLine($"NOMATCH: Speech could not be recognized.");
                            }
                        };


                        // Starts transcribing of the conversation. Uses StopTranscribingAsync() to stop transcribing when all participants leave.
                        await conversationTranscriber.StartTranscribingAsync().ConfigureAwait(false);

                        await Task.Delay(TimeSpan.FromSeconds(300));
                        // Waits for completion.
                        // Use Task.WaitAny to keep the task rooted.
                        //await Task.WhenAny(stopRecognition.Task);
                        //var stopRecogintion = new TaskCompletionSource<int>();

                        //// Stop transcribing the conversation.
                        await conversationTranscriber.StopTranscribingAsync().ConfigureAwait(false);
                        ViewBag.Message = CognizeText.ToString();
                        return Content(CognizeText.ToString());


                        Console.WriteLine("END");
                    }
                }
            }
        }
    }
}