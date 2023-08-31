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

        //�إ߻y��ñ��
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

        //�w�q�ϥΪ��n���˥�
         private static string voiceSignatureUser1;
        private static string voiceSignatureUser2;
        private static string voiceSignatureUser3;
        private static string voiceSignatureUser4;

        //voiceSample�O�n���˥��ܼ� ���D�{���i�H�h�h���ϥ� �᭱�N�OAPIKEY��ϰ�
        private static async Task<VoiceSignature> CreateVoiceSignatureFromVoiceSample(string voiceSample, string subscriptionKey, string region)
        {
            byte[] fileBytes = File.ReadAllBytes(voiceSample); //Ū����� voiceSample ���|���ɮת��Ҧ��줸�աA�N��Ū���� fileBytes �}�C��
            var content = new ByteArrayContent(fileBytes);
            var client = new HttpClient();  //Ū����� voiceSample ���|���ɮת��Ҧ��줸�աA�N��Ū���� fileBytes �}�C��
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", subscriptionKey); //���w�� URL �o�e POST �ШD�A�N fileBytes �@�����e�ǰe�C�ӽШD�N�줸�դ��e���浹���w�� URL �H�ͦ��y��ñ�W�C
            var response = await client.PostAsync($"https://signature.{region}.cts.speech.microsoft.com/api/v1/Signature/GenerateVoiceSignatureFromByteArray", content);
            // �y��ñ�W�]�t�Ӧ��T�����媺 Signature json ���c�������B���ҩM�ƾ���ȡC
            // �y��ñ�W�榡�ܨҡG { "Version": <�Ʀr�r�Ŧ�ξ�ƭ�>, "Tag": "string", "Data": "string" }
            var jsonData = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<VoiceSignature>(jsonData);
            return result;
        }

        public static async Task TranscribeConversationsAsync(string conversationWaveFile, string subscriptionKey, string region)
        {
            var config = SpeechConfig.FromSubscription(subscriptionKey, region);
            //�o�ӳ]�w�ѼƬO���F�䴩�h�H�|ĳ�����A���y������t�Υi�H�B�z�h�ӻP�|�̪��y���A�ñN���ഫ����r��T
            config.SetProperty("ConversationTranscriptionInRoomAndOnline", "true");

            config.SetProperty("DifferentiateGuestSpeakers", "true");


            //�q�{�O�^����]�i�H���w
            config.SpeechRecognitionLanguage = "zh-tw";

            //TaskCreationOptions.RunContinuationsAsynchronously�^�O���F�T�O����ާ@�b�D�P�B���Ҥ��B��A�q���קK����D�n������C�o�b�ݭn�T�O�D�P�B�ާ@���|����D������ɬO���Ϊ��A�S�O�O�b UI ���ε{�Ǥ��C
            var stopRecognition = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);

            // �q wav �����q�{���J���Ыح��W�y
            using (var audioInput = AudioStreamReader.OpenWavFile(conversationWaveFile))
            {
                //�ϥ� Guid.NewGuid() �Ыؤ@�Ӱߤ@���|ĳ�ѧO�X�C
                var meetingID = Guid.NewGuid().ToString();
                //�Ыؤ@�ӷs�� Conversation ����G�ϥγЫت��y�����Ѱt�m�M�|ĳ�ѧO�X�A�Ыؤ@�� Conversation ����A�N���b�i�檺�|ĳ�C
                using (var conversation = await Conversation.CreateConversationAsync(config, meetingID))
                {
                    // Create a conversation transcriber using audio stream input
                    //�Ыؤ@�ӷ|������� ConversationTranscriber�G�ϥΤ��e�Ыت����W��J�y�Ыؤ@�ӷ|��������A��������N�����|ĳ�����y�����W�C
                    using (var conversationTranscriber = new ConversationTranscriber(audioInput))
                    {
                        // Subscribe to events
                        //�q�\�ƥ�G���|������������P�ƥ�]�Ҧp Transcribing�BTranscribed�BCanceled ���^�]�m�ƥ�B�z�{�ǡA�H�K�b�ƥ�o�ͮɰ���������ާ@�C
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
                        //�ЫػP�|�̨å[�J�|�ܡG�ھڤ��e�Ыت��y��ñ�W 
                        //�Ыت�ܷ|ĳ�ѻP�̪� Participant ����A�M��N�o�ǰѻP�̲K�[��|�ܤ��C
                        var languageForUser1 = "zh-tw"; // For example "en-US"
                        var speakerA = Participant.From("�a��", languageForUser1, voiceSignatureUser1);
                        var languageForUser2 = "zh-tw"; // For example "en-US"
                        var speakerB = Participant.From("���X", languageForUser2, voiceSignatureUser2);
                        var languageForUser3 = "zh-TW"; // For example "en-US"
                        var speakerC = Participant.From("jahow", languageForUser3, voiceSignatureUser3);
                        var languageForUser4 = "zh-TW"; // For example "en-US"
                        var speakerD = Participant.From("yating", languageForUser4, voiceSignatureUser4);
                        await conversation.AddParticipantAsync(speakerA);
                        await conversation.AddParticipantAsync(speakerB);
                        await conversation.AddParticipantAsync(speakerC);
                        await conversation.AddParticipantAsync(speakerD);

                        // Join to the conversation.
                        //�s����|�ܨö}�l����G�N�|��������s����|�ܤ��A�M��}�l�i��|�ܪ��y������C
                        await conversationTranscriber.JoinConversationAsync(conversation);

                        // Starts transcribing of thpe conversation. Uses StopTranscribingAsync() to stop transcribing when all participants leave.
                        //�ϥ� Task.WaitAny ������������G��������������A�ϥ� Task.WaitAny ��k�T�O�b��������e�O�����ȳB�󬡰ʪ��A�C
                        await conversationTranscriber.StartTranscribingAsync().ConfigureAwait(false);

                        //��������G�b�Ҧ��ѻP�����}�|�ܫ�A��������������|��������C
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
            var voiceSignatureWaveFileUser1 = "�a��.wav";
            var voiceSignatureWaveFileUser2 = "���X.wav";
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