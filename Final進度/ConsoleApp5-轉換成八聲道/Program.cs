using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp5
{
    internal class Program
    {
        static void Main(string[] args)
        {
            string inputFilePath = "chunk_1.wav"; 
            string outputFilePath = "chunk_12.wav"; 

            using (var reader = new AudioFileReader(inputFilePath))
            {
                var newFormat = new WaveFormat(reader.WaveFormat.SampleRate, 8);

                using (var writer = new WaveFileWriter(outputFilePath, newFormat))
                {
                    var buffer = new float[reader.WaveFormat.SampleRate * reader.WaveFormat.Channels]; 

                    while (reader.Position < reader.Length)
                    {
                        int bytesRead = reader.Read(buffer, 0, buffer.Length);

                        
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
                                    writer.WriteSample(0); 
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
