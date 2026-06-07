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

        private volatile bool _found = false;
        private volatile bool _stopped = false;
        private volatile int _attempts = 0;

        private Stopwatch _stopwatch;

        public int ThreadCount { get; private set; }
        public long TotalAttempts { get { return _attempts; } }

        public BruteForceEngine(string targetHash, string charset)
        {
            _targetHash = targetHash;
            _charset = charset;
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
                    for (int len = 1; len <= _maxLength; len++)
                    {
                        if (_found || _stopped) break;

                        // instead of loading ALL combinations into memory,
                        // we split by first character - each thread gets
                        // one starting character to work from
                        Task[] tasks = new Task[ThreadCount];
                        int charsPerThread = _charset.Length / ThreadCount;

                        for (int t = 0; t < ThreadCount; t++)
                        {
                            int start = t * charsPerThread;
                            int end = (t == ThreadCount - 1)
                                ? _charset.Length
                                : start + charsPerThread;
                            int currentLen = len;

                            tasks[t] = Task.Run(() =>
                            {
                                // each thread works on its own range of first characters
                                for (int c = start; c < end; c++)
                                {
                                    if (_found || _stopped) return;

                                    // generate combinations starting with this character
                                    int[] indices = new int[currentLen];
                                    indices[0] = c;

                                    while (true)
                                    {
                                        if (_found || _stopped) return;

                                        // build the candidate string
                                        char[] combo = new char[currentLen];
                                        for (int i = 0; i < currentLen; i++)
                                            combo[i] = _charset[indices[i]];

                                        string candidate = new string(combo);
                                        _attempts++;

                                        if (HashHelper.Verify(candidate, _targetHash))
                                        {
                                            _found = true;
                                            _stopwatch.Stop();
                                            OnPasswordFound?.Invoke(candidate, _stopwatch.Elapsed);
                                            return;
                                        }

                                        if (_attempts % 10000 == 0)
                                            OnProgressUpdate?.Invoke(_attempts);

                                        // increment from the rightmost position
                                        // but never change index[0] - that belongs to this thread
                                        int pos = currentLen - 1;
                                        while (pos > 0)
                                        {
                                            indices[pos]++;
                                            if (indices[pos] < _charset.Length) break;
                                            indices[pos] = 0;
                                            pos--;
                                        }

                                        // if pos reached 0 it means we finished
                                        // all combinations for this starting character
                                        if (pos == 0) break;
                                    }
                                }
                            });
                        }

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

        public TimeSpan RunSingleThreaded(string targetHash)
        {
            Stopwatch sw = Stopwatch.StartNew();
            bool found = false;

            for (int len = 1; len <= _maxLength; len++)
            {
                if (found) break;

                int[] indices = new int[len];

                while (true)
                {
                    char[] combo = new char[len];
                    for (int i = 0; i < len; i++)
                        combo[i] = _charset[indices[i]];

                    if (HashHelper.Verify(new string(combo), targetHash))
                    {
                        found = true;
                        break;
                    }

                    int pos = len - 1;
                    while (pos >= 0)
                    {
                        indices[pos]++;
                        if (indices[pos] < _charset.Length) break;
                        indices[pos] = 0;
                        pos--;
                    }
                    if (pos < 0) break;
                }
            }

            sw.Stop();
            return sw.Elapsed;
        }

        public void Stop()
        {
            _stopped = true;
        }
    }
}