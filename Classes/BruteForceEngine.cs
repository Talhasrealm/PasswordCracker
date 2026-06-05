using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace PasswordCracker.Classes
{
    public class BruteForceEngine
    {
        // Events to communicate back to the GUI
        public event Action<long> OnProgressUpdate;
        public event Action<string, TimeSpan> OnPasswordFound;
        public event Action OnStopped;

        private readonly string _charset;
        private readonly string _targetHash;
        private readonly int _maxLength = 6;

        // volatile = all threads see the updated value immediately
        // Professor taught this is how threads share a simple flag
        private volatile bool _found = false;
        private volatile bool _stopped = false;

        private long _totalAttempts = 0;
        private readonly object _lock = new object(); // lock = taught in synchronization topic

        private Stopwatch _stopwatch;

        // CPU cores - 1 (minimum 1)
        public int ThreadCount { get; }

        public long TotalAttempts
        {
            get { lock (_lock) { return _totalAttempts; } }
        }

        public BruteForceEngine(string targetHash, string charset)
        {
            _targetHash = targetHash;
            _charset = charset;
            ThreadCount = Math.Max(1, Environment.ProcessorCount - 1);
        }

        /// <summary>
        /// Multi-threaded brute force using Task array.
        /// Each Task = one worker trying a chunk of combinations.
        /// Professor taught: Task is mid-level, easier and safer than Thread.
        /// </summary>
        public void StartMultiThreaded()
        {
            _found = false;
            _stopped = false;
            _totalAttempts = 0;
            _stopwatch = Stopwatch.StartNew();

            // Run everything in a background Task so GUI doesn't freeze
            Task.Run(() =>
            {
                try
                {
                    // Try lengths 1, 2, 3, 4, 5, 6
                    for (int length = 1; length <= _maxLength; length++)
                    {
                        if (_found || _stopped) break;

                        // Get all combinations for this length
                        List<string> allCombinations = GetCombinationsForLength(length);

                        // Split into chunks - one chunk per thread
                        List<List<string>> chunks = SplitIntoChunks(allCombinations, ThreadCount);

                        // Create one Task per chunk (like hiring workers)
                        Task[] tasks = new Task[chunks.Count];

                        for (int i = 0; i < chunks.Count; i++)
                        {
                            List<string> chunk = chunks[i]; // capture for closure

                            // Each Task works on its own chunk independently
                            tasks[i] = Task.Run(() =>
                            {
                                foreach (string candidate in chunk)
                                {
                                    // Stop if another thread already found it
                                    if (_found || _stopped) return;

                                    // Count attempts using lock (taught in synchronization)
                                    lock (_lock)
                                    {
                                        _totalAttempts++;
                                    }

                                    // Check if this candidate matches the hash
                                    if (HashHelper.Verify(candidate, _targetHash))
                                    {
                                        _found = true;
                                        _stopwatch.Stop();
                                        OnPasswordFound?.Invoke(candidate, _stopwatch.Elapsed);
                                        return;
                                    }

                                    // Report progress every 10000 attempts
                                    if (_totalAttempts % 10000 == 0)
                                        OnProgressUpdate?.Invoke(_totalAttempts);
                                }
                            });
                        }

                        // Wait for ALL tasks to finish before moving to next length
                        // Task.WaitAll = taught in multithreading notes
                        Task.WaitAll(tasks);
                    }
                }
                finally
                {
                    _stopwatch?.Stop();
                    if (!_found)
                        OnStopped?.Invoke();
                }
            });
        }

        /// <summary>
        /// Single-threaded version for performance comparison.
        /// Same logic but only one worker, no Tasks.
        /// </summary>
        public TimeSpan RunSingleThreaded(string targetHash)
        {
            var sw = Stopwatch.StartNew();
            bool found = false;

            for (int length = 1; length <= _maxLength && !found; length++)
            {
                foreach (string candidate in GetCombinationsForLength(length))
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

        /// <summary>
        /// Stop the attack by setting the shared flag.
        /// All Tasks check this flag and stop themselves.
        /// </summary>
        public void Stop()
        {
            _stopped = true;
        }

        // ── Private Helpers ──

        /// <summary>
        /// Generates all combinations for a given length.
        /// Example length=2, charset="ab": aa, ab, ba, bb
        /// </summary>
        private List<string> GetCombinationsForLength(int length)
        {
            List<string> results = new List<string>();
            int[] indices = new int[length];
            int charsetLen = _charset.Length;

            while (true)
            {
                // Build current combination from index array
                char[] combo = new char[length];
                for (int i = 0; i < length; i++)
                    combo[i] = _charset[indices[i]];
                results.Add(new string(combo));

                // Increment index array like counting up a number
                int pos = length - 1;
                while (pos >= 0)
                {
                    indices[pos]++;
                    if (indices[pos] < charsetLen) break;
                    indices[pos] = 0;
                    pos--;
                }

                if (pos < 0) break; // All combinations done
            }

            return results;
        }

        /// <summary>
        /// Splits a list into N smaller chunks.
        /// Like dividing work equally between workers.
        /// </summary>
        private List<List<string>> SplitIntoChunks(List<string> items, int chunkCount)
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