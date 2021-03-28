using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Sorter
{
    class Program
    {
        private static long sortBufferSizeBytes = 100000000;
        private static string inputFile = "generated.txt";
        private static long currentPosition = 0;
        private static object syncRoot = new object();
        private static EventWaitHandle waitHandle = new EventWaitHandle(false, EventResetMode.ManualReset);
        static async Task Main(string[] args)
        {

            using (var fileStream = File.Open(inputFile, FileMode.Open, FileAccess.Read))
            using (var streamReader = new StreamReader(fileStream, Encoding.UTF8, true, (int)sortBufferSizeBytes))
            {
                var taskList = new List<Task>();
                for (int i = 0; i < Environment.ProcessorCount; i++)
                {
                    taskList.Add(GetBufferAndSort(streamReader));
                }

                waitHandle.WaitOne();
            }

            Console.WriteLine("Hello World!");
        }

        private static Task GetBufferAndSort(StreamReader fileReader)
        {
            var readCounter = 0;
            return Task.Factory.StartNew(() =>
            {
                long startPosition = 0;

                var itemList = new List<Item>();
                lock (syncRoot)
                {
                    startPosition = currentPosition;
                    string line;
                    while (readCounter < sortBufferSizeBytes && (line = fileReader.ReadLine()) != null)
                    {
                        if (line is null)
                        {
                            waitHandle.Set();
                        }

                        readCounter += line.Length * sizeof(char);
                        itemList.Add(new Item(line));
                    }

                    currentPosition += readCounter;
                }

                var sorted = itemList.OrderBy(i => i.Value).ThenBy(i => i.Number).Select(i => i.ToString());

                File.AppendAllLines($"{startPosition}-{startPosition + readCounter}.txt", sorted);

            }).ContinueWith(t => { 
                if (readCounter > 0)
                {
                    GetBufferAndSort(fileReader);
                }
            });
        }
    }
}
