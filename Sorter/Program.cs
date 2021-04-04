using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
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
        private static object mergeLock = new object();
        private static EventWaitHandle waitSorts = new EventWaitHandle(false, EventResetMode.ManualReset);
        private static EventWaitHandle waitMerge = new EventWaitHandle(false, EventResetMode.ManualReset);
        private static ConcurrentBag<string> filesToMerge = new ConcurrentBag<string>();
        private static int workingCount = 0;

        static async Task Main(string[] args)
        {
            var stopWatch = new Stopwatch();
            stopWatch.Start();
            await using var fileStream = File.Open(inputFile, FileMode.Open, FileAccess.Read);
            using var streamReader = new StreamReader(fileStream, Encoding.UTF8, true, (int) sortBufferSizeBytes);

            var sortTaskList = new List<Task>();
            for (int i = 0; i < Environment.ProcessorCount; i++)
            {
                sortTaskList.Add(GetBufferAndSort(streamReader));
            }

            var mergeTaskList = new List<Task>();
            for (int i = 0; i < Environment.ProcessorCount; i++)
            {
                MergeFiles();
            }

            waitSorts.WaitOne();
            waitMerge.WaitOne();
            stopWatch.Stop();
            Console.WriteLine($"Elapsed seconds: {stopWatch.Elapsed.TotalSeconds}");
            Console.ReadLine();
        }

        private static Task MergeFiles()
        {
            string secondFile = null;
            return Task.Factory.StartNew(async () =>
            {
                string firstFile = null;
                lock (mergeLock)
                {
                    while (filesToMerge.Count < 2 &&
                           (workingCount > 0 || !waitSorts.WaitOne(TimeSpan.FromMilliseconds(10))))
                    {
                        Thread.Sleep(10);
                    }

                    if (filesToMerge.Count >= 2)
                    {
                        filesToMerge.TryTake(out firstFile);
                        filesToMerge.TryTake(out secondFile);
                        workingCount++;
                    }
                }

                if (secondFile is null)
                {
                    if (filesToMerge.Count < 2 && workingCount == 0)
                    {
                        waitMerge.Set();
                    }

                    return;
                }

                var mergedFile = Guid.NewGuid().ToString();

                await using (var resultStream = File.Open(mergedFile, FileMode.Create, FileAccess.Write))
                await using (var streamWriter =
                    new StreamWriter(resultStream, Encoding.UTF8, (int) sortBufferSizeBytes))
                await using (var firstStream = File.Open(firstFile, FileMode.Open, FileAccess.Read))
                using (var firstStreamReader =
                    new StreamReader(firstStream, Encoding.UTF8, true, (int) sortBufferSizeBytes))
                await using (var secondStream = File.Open(secondFile, FileMode.Open, FileAccess.Read))
                using (var secondStreamReader =
                    new StreamReader(secondStream, Encoding.UTF8, true, (int) sortBufferSizeBytes))
                {
                    var firstStringValue =
                        firstStreamReader.EndOfStream ? null : await firstStreamReader.ReadLineAsync();
                    var firstItem = firstStringValue is null ? null : new Item(firstStringValue);

                    var secondStringValue =
                        secondStreamReader.EndOfStream ? null : await secondStreamReader.ReadLineAsync();
                    var secondItem = secondStringValue is null ? null : new Item(secondStringValue);

                    while (!secondStreamReader.EndOfStream || !firstStreamReader.EndOfStream)
                    {
                        if (firstItem != null && secondItem != null)
                        {
                            if (String.Compare(firstItem.Value, secondItem.Value, StringComparison.Ordinal) < 0 ||
                                String.Compare(firstItem.Value, secondItem.Value, StringComparison.Ordinal) == 0 &&
                                firstItem.Number < secondItem.Number)
                            {
                                await streamWriter.WriteLineAsync(firstItem.ToString());
                                firstStringValue =
                                    firstStreamReader.EndOfStream ? null : await firstStreamReader.ReadLineAsync();
                                firstItem = firstStringValue is null ? null : new Item(firstStringValue);
                            }
                            else
                            {
                                await streamWriter.WriteLineAsync(secondItem.ToString());
                                secondStringValue =
                                    secondStreamReader.EndOfStream ? null : await secondStreamReader.ReadLineAsync();
                                secondItem = secondStringValue is null ? null : new Item(secondStringValue);
                            }
                        }
                        else if (firstItem != null)
                        {
                            await streamWriter.WriteLineAsync(firstItem.ToString());
                            firstStringValue =
                                firstStreamReader.EndOfStream ? null : await firstStreamReader.ReadLineAsync();
                            firstItem = firstStringValue is null ? null : new Item(firstStringValue);
                        }
                        else if (secondItem != null)
                        {
                            await streamWriter.WriteLineAsync(secondItem.ToString());
                            secondStringValue =
                                secondStreamReader.EndOfStream ? null : await secondStreamReader.ReadLineAsync();
                            secondItem = secondStringValue is null ? null : new Item(secondStringValue);
                        }
                    }
                }

                File.Delete(firstFile);
                File.Delete(secondFile);
                filesToMerge.Add(mergedFile);
                Interlocked.Decrement(ref workingCount);
                GC.Collect();
            }).ContinueWith(task =>
            {
                if (secondFile != null)
                {
                    MergeFiles();
                }
            });
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
                    while (readCounter < sortBufferSizeBytes && !fileReader.EndOfStream &&
                           (line = fileReader.ReadLine()) != null)
                    {
                        if (string.IsNullOrEmpty(line) || fileReader.EndOfStream)
                        {
                            waitSorts.Set();
                        }

                        readCounter += line.Length * sizeof(char);
                        itemList.Add(new Item(line));
                    }

                    currentPosition += readCounter;
                }

                if (readCounter <= 0) return;

                var sorted = itemList.OrderBy(i => i.Value).ThenBy(i => i.Number).Select(i => i.ToString());
                var fileName = $"{startPosition}-{startPosition + readCounter}.txt";
                File.AppendAllLines(fileName, sorted);
                filesToMerge.Add(fileName);
                GC.Collect();
            }).ContinueWith(t =>
            {
                if (readCounter > 0)
                {
                    GetBufferAndSort(fileReader);
                }
            });
        }
    }
}