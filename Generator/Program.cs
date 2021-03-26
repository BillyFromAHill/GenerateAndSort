using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Generator
{
    class Program
    {

        static string fileName = "generated.txt";

        static long generatorsCount = Environment.ProcessorCount;
        static long bufferSizeBytes = 10960000;
        static long buffersWritten = 0;
        static long destinationSizeBytes = 10000000000;

        static List<Task> taskList = new List<Task>();
        static object syncRoot = new object();
        static EventWaitHandle waitHandle = new EventWaitHandle(false, EventResetMode.ManualReset);
        static async Task Main(string[] args)
        {
            var stopWatch = new Stopwatch();
            stopWatch.Start();

            for (int i = 0; i < generatorsCount; i++)
            {
                taskList.Add(AddChainedTask());
            }

            waitHandle.WaitOne();

            await Task.WhenAll(taskList);
            stopWatch.Stop();

            Console.WriteLine($"ElapsedSeconds: {stopWatch.Elapsed.TotalSeconds}");
            Console.ReadLine();
        }

        public static Task AddChainedTask()
        {
            return new BufferGenerator(bufferSizeBytes).GenerateBuffer().ContinueWith(a =>
            {
                lock (syncRoot)
                {

                    File.AppendAllText(fileName, a.Result.CreateString());
                    buffersWritten++;
                    if (buffersWritten * bufferSizeBytes < destinationSizeBytes)
                    {
                        AddChainedTask();
                    }
                    else
                    {
                        waitHandle.Set();
                    }

                }
            });
        }
    }
}
