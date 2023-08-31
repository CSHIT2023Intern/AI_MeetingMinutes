using Microsoft.CognitiveServices.Speech.Audio;
using System;

namespace AzureMVC.Controllers
{
    internal class AudioRecorder
    {
        private string outputPath;
        private AudioStreamFormat audioFormat;

        public AudioRecorder(string outputPath, AudioStreamFormat audioFormat)
        {
            this.outputPath = outputPath;
            this.audioFormat = audioFormat;
        }
    }
}