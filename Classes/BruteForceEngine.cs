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

        private CancellationTokenSource _cts;
        private readonly string _charset;
        private readonly string _targetHash;
        private readonly int _maxLength = 6;
        private volatile bool _found = false;
        private long _totalAttempts = 0;
        private Stopwatch _stopwatch;

        public int ThreadCount { get; }
        public long TotalAttempts => Interlocked.Read(ref _totalAttempts);

        public BruteForceEngine(string targetHash, string charset)
        {
            _targetHash = targetHash;
            _charset = charset;
            // Use CPU cores - 1, minimum 1
            ThreadCount = Math.Max(1, Environment.ProcessorCount - 1);
        }

        public void StartMultiThreaded()
        {
            _found = false;
            _totalAttempts = 0;
            _cts = new CancellationTokenSource();
            _stopwatch = Stopwatch.StartNew();

            var options = new ParallelOptions
            {
                MaxDegreeOfParallelism = ThreadCount,
                CancellationToken = _cts.Token
            };

            Task.Run(() =>
            {
                try
                {
                    for (int length = 1; length <= _maxLength && !_found; length++)
                    {
                        var chunks = SplitIntoChunks(
                            GenerateCombinationsForLength(length), ThreadCount);

                        Parallel.ForEach(chunks, options, chunk =>
                        {
                            foreach (string candidate in chunk)
                            {
                                if (_found || _cts.Token.IsCancellationRequested)
                                    return;

                                Interlocked.Increment(ref _totalAttempts);

                                if (HashHelper.Verify(candidate, _targetHash))
                                {
                                    _found = true;
                                    _stopwatch.Stop();
                                    _cts.Cancel();
                                    OnPasswordFound?.Invoke(candidate, _stopwatch.Elapsed);
                                    return;
                                }

                                if (_totalAttempts % 10000 == 0)
                                    OnProgressUpdate?.Invoke(_totalAttempts);
                            }
                        });
                    }
                }
                catch (OperationCanceledException) { }
                finally
                {
                    _stopwatch?.Stop();
                    if (!_found) OnStopped?.Invoke();
                }
            });
        }

        public TimeSpan RunSingleThreaded(string targetHash)
        {
            var sw = Stopwatch.StartNew();
            bool found = false;

            for (int length = 1; length <= _maxLength && !found; length++)
                foreach (string candidate in GenerateCombinationsForLength(length))
                    if (HashHelper.Verify(candidate, targetHash))
                    { found = true; break; }

            sw.Stop();
            return sw.Elapsed;
        }

        public void Stop() => _cts?.Cancel();

        private IEnumerable<string> GenerateCombinationsForLength(int length)
        {
            int[] indices = new int[length];
            int charsetLen = _charset.Length;

            while (true)
            {
                char[] combo = new char[length];
                for (int i = 0; i < length; i++)
                    combo[i] = _charset[indices[i]];
                yield return new string(combo);

                int pos = length - 1;
                while (pos >= 0)
                {
                    indices[pos]++;
                    if (indices[pos] < charsetLen) break;
                    indices[pos] = 0;
                    pos--;
                }
                if (pos < 0) yield break;
            }
        }

        private List<List<string>> SplitIntoChunks(IEnumerable<string> items, int chunkCount)
        {
            var chunks = new List<List<string>>();
            for (int i = 0; i < chunkCount; i++)
                chunks.Add(new List<string>());

            int index = 0;
            foreach (var item in items)
            {
                chunks[index % chunkCount].Add(item);
                index++;
                if (_found) break;
            }
            return chunks;
        }
    }
}