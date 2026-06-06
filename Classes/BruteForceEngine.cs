using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace PasswordCracker.Classes
{
    public class BruteForceEngine
    {
        public event Action<long> OnProgressUpdate;
        public event Action<string, TimeSpan> OnPasswordFound;
        public event Action OnStopped;

        private string _charset;
        private string _targetHash;
        private int _maxLength = 6;

        // flag to stop all threads when password is found
        private volatile bool _found = false;
        private volatile bool _stopped = false;

        private long _attempts = 0;
        private object _lock = new object();
        private Stopwatch _stopwatch;

        public int ThreadCount { get; private set; }

        public long TotalAttempts
        {
            get { return _attempts; }
        }

        public BruteForceEngine(string targetHash, string charset)
        {
            _targetHash = targetHash;
            _charset = charset;

            // use cpu cores - 1 so computer doesnt freeze
            ThreadCount = Environment.ProcessorCount - 1;
            if (ThreadCount < 1) ThreadCount = 1;
        }

        public void StartMultiThreaded()
        {
            _found = false;
            _stopped = false;
            _attempts = 0;
            _stopwatch = Stopwatch.StartNew();

            Task.Run(() =>
            {
                try
                {
                    // try all lengths starting from 1
                    for (int len = 1; len <= _maxLength; len++)
                    {
                        if (_found || _stopped) break;

                        // get all combinations for this length
                        List<string> combinations = GetCombinations(len);

                        // split work between threads
                        List<List<string>> chunks = SplitWork(combinations, ThreadCount);

                        // start one task per chunk
                        Task[] tasks = new Task[chunks.Count];

                        for (int i = 0; i < chunks.Count; i++)
                        {
                            List<string> chunk = chunks[i];

                            tasks[i] = Task.Run(() =>
                            {
                                foreach (string candidate in chunk)
                                {
                                    if (_found || _stopped) return;

                                    // count how many we tried
                                    lock (_lock)
                                    {
                                        _attempts++;
                                    }

                                    if (HashHelper.Verify(candidate, _targetHash))
                                    {
                                        _found = true;
                                        _stopwatch.Stop();
                                        OnPasswordFound?.Invoke(candidate, _stopwatch.Elapsed);
                                        return;
                                    }

                                    // update progress every 10000 attempts
                                    if (_attempts % 10000 == 0)
                                        OnProgressUpdate?.Invoke(_attempts);
                                }
                            });
                        }

                        // wait for all tasks to finish
                        Task.WaitAll(tasks);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error: " + ex.Message);
                }
                finally
                {
                    _stopwatch?.Stop();
                    if (!_found)
                        OnStopped?.Invoke();
                }
            });
        }

        // single thread version to compare performance
        public TimeSpan RunSingleThreaded(string targetHash)
        {
            Stopwatch sw = Stopwatch.StartNew();
            bool found = false;

            for (int len = 1; len <= _maxLength; len++)
            {
                if (found) break;

                List<string> combinations = GetCombinations(len);

                foreach (string candidate in combinations)
                {
                    if (HashHelper.Verify(candidate, targetHash))
                    {
                        found = true;
                        break;
                    }
                }
            }

            sw.Stop();
            return sw.Elapsed;
        }

        public void Stop()
        {
            _stopped = true;
        }

        // generates all possible combinations for given length
        private List<string> GetCombinations(int length)
        {
            List<string> results = new List<string>();
            int[] indices = new int[length];
            int charsetLen = _charset.Length;

            while (true)
            {
                char[] combo = new char[length];
                for (int i = 0; i < length; i++)
                    combo[i] = _charset[indices[i]];

                results.Add(new string(combo));

                // increment like counting numbers
                int pos = length - 1;
                while (pos >= 0)
                {
                    indices[pos]++;
                    if (indices[pos] < charsetLen) break;
                    indices[pos] = 0;
                    pos--;
                }

                if (pos < 0) break;
            }

            return results;
        }

        // splits list into equal chunks for each thread
        private List<List<string>> SplitWork(List<string> items, int chunkCount)
        {
            List<List<string>> chunks = new List<List<string>>();

            for (int i = 0; i < chunkCount; i++)
                chunks.Add(new List<string>());

            for (int i = 0; i < items.Count; i++)
                chunks[i % chunkCount].Add(items[i]);

            return chunks;
        }
    }
}