using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NAudio.Wave;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp1
{
    internal class Program
    {
        static void Main(string[] args)
        {
            string inputFile = @"C:\Users\Administrator\Desktop\aiMeeting\ConsoleApp1\UploadFile\TaipeiMeet.wav";
            string outputDirectory = @"C:\Users\Administrator\Desktop\aiMeeting\ConsoleApp1\cutWav\";
            int chunkSizeInSeconds = 600; // 每個小檔案的秒數

            using (var reader = new WaveFileReader(inputFile))
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
            Console.WriteLine("裁切完成！");                       
            // 列出目錄中的所有檔案
            string[] files = Directory.GetFiles(outputDirectory, "chunk_*.wav");

            // 對檔案名稱進行自定義的數字排序
            Array.Sort(files, new NumericComparer());

            foreach (string file in files)
            {
                Console.WriteLine($"Reading file: {file}");
            }
            foreach (string file in files)
            {
                File.Delete(file);
            }

            Console.ReadLine();
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
    
    }
}
