using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace BruteForceApp
{
    /// <summary>
    /// Orchestrates brute-force attacks: both multi-threaded and single-threaded.
    /// Generator and Validator are used independently per requirements.
    /// </summary>
    public class BruteForceEngine
    {
        private readonly PasswordManager _validator;
        private readonly CombinationGenerator _generator;

        private CancellationTokenSource _cts;
        private long _totalCombinations;
        private long _checkedCount;

        // Events for GUI updates
        public event Action<long, long> ProgressUpdated;   // (checked, total)
        public event Action<string, TimeSpan> PasswordFound; // (password, elapsed)
        public event Action<string> LogMessage;

        public bool IsRunning => _cts != null && !_cts.IsCancellationRequested;

        public BruteForceEngine(PasswordManager validator, CombinationGenerator generator)
        {
            _validator = validator;
            _generator = generator;
            _totalCombinations = generator.TotalCombinations();
        }

        // ──────────────────────────────────────────────
        //  MULTI-THREADED ATTACK
        // ──────────────────────────────────────────────

        /// <summary>
        /// Starts multi-threaded brute force using (CPU cores - 1) threads.
        /// </summary>
        public void StartMultiThreaded(string targetHash)
        {
            _cts = new CancellationTokenSource();
            _checkedCount = 0;

            int threadCount = Math.Max(1, Environment.ProcessorCount - 1);
            LogMessage?.Invoke($"[Multi] Starting with {threadCount} threads (CPU cores: {Environment.ProcessorCount})");

            var sw = Stopwatch.StartNew();
            var found = new ConcurrentBag<string>();

            Task[] tasks = new Task[threadCount];
            for (int t = 0; t < threadCount; t++)
            {
                int threadIdx = t;
                tasks[t] = Task.Run(() =>
                {
                    foreach (var (globalIndex, candidate) in _generator.GetPartition(threadIdx, threadCount))
                    {
                        if (_cts.IsCancellationRequested) break;

                        Interlocked.Increment(ref _checkedCount);

                        // Report progress every 10000 checks
                        if (_checkedCount % 10000 == 0)
                            ProgressUpdated?.Invoke(_checkedCount, _totalCombinations);

                        // VALIDATOR used independently from GENERATOR
                        if (_validator.Validate(candidate, targetHash))
                        {
                            found.Add(candidate);
                            _cts.Cancel();
                            break;
                        }
                    }
                }, _cts.Token);
            }

            Task.Run(async () =>
            {
                try { await Task.WhenAll(tasks); } catch { }
                sw.Stop();
                ProgressUpdated?.Invoke(_checkedCount, _totalCombinations);

                if (found.TryPeek(out string result))
                    PasswordFound?.Invoke(result, sw.Elapsed);
                else
                    LogMessage?.Invoke("[Multi] Password not found.");
            });
        }

        // ──────────────────────────────────────────────
        //  SINGLE-THREADED ATTACK (for performance log)
        // ──────────────────────────────────────────────

        /// <summary>
        /// Runs single-threaded brute force synchronously and returns elapsed time.
        /// </summary>
        public (string password, TimeSpan elapsed) RunSingleThreaded(string targetHash)
        {
            var sw = Stopwatch.StartNew();
            long total = _generator.TotalCombinations();
            for (long i = 0; i < total; i++)
            {
                string candidate = _generator.IndexToCandidate(i);
                if (_validator.Validate(candidate, targetHash))
                {
                    sw.Stop();
                    return (candidate, sw.Elapsed);
                }
            }
            sw.Stop();
            return (null, sw.Elapsed);
        }

        public void Stop()
        {
            _cts?.Cancel();
            LogMessage?.Invoke("Attack stopped by user.");
        }

        public long CheckedCount => Interlocked.Read(ref _checkedCount);
        public long TotalCombinations => _totalCombinations;
    }
}
