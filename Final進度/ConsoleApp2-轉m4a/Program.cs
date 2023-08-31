using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NAudio.Wave;

namespace ConsoleApp2
{
    internal class Program
    {
        static void Main(string[] args)
        {
            string m4aFilePath = "12.m4a";
            string wavFilePath = "12.wav";

            using (var reader = new MediaFoundationReader(m4aFilePath))
            {
                WaveFileWriter.CreateWaveFile(wavFilePath, reader);
            }

        }
    }
}
