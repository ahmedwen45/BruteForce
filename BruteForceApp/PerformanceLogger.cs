using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace BruteForceApp
{
    /// <summary>
    /// Records and compares performance between single-threaded and multi-threaded brute force runs.
    /// </summary>
    public class PerformanceLogger
    {
        private readonly List<string> _entries = new();
        private TimeSpan? _singleTime;
        private TimeSpan? _multiTime;
        private int _threadCount;

        public void LogSingleThread(TimeSpan elapsed, string password)
        {
            _singleTime = elapsed;
            string entry = $"[{DateTime.Now:HH:mm:ss}] SINGLE-THREAD | Found: '{password}' | Time: {elapsed.TotalMilliseconds:F0} ms";
            _entries.Add(entry);
        }

        public void LogMultiThread(TimeSpan elapsed, string password, int threadCount)
        {
            _multiTime = elapsed;
            _threadCount = threadCount;
            string entry = $"[{DateTime.Now:HH:mm:ss}] MULTI-THREAD  | Found: '{password}' | Threads: {threadCount} | Time: {elapsed.TotalMilliseconds:F0} ms";
            _entries.Add(entry);
        }

        public string GetComparisonReport()
        {
            var sb = new StringBuilder();
            sb.AppendLine("═══════════════════════════════════════════");
            sb.AppendLine("       PERFORMANCE COMPARISON REPORT       ");
            sb.AppendLine("═══════════════════════════════════════════");

            foreach (var e in _entries)
                sb.AppendLine(e);

            if (_singleTime.HasValue && _multiTime.HasValue)
            {
                double speedup = _singleTime.Value.TotalMilliseconds / _multiTime.Value.TotalMilliseconds;
                sb.AppendLine("───────────────────────────────────────────");
                sb.AppendLine($"  Single-thread: {_singleTime.Value.TotalMilliseconds:F0} ms");
                sb.AppendLine($"  Multi-thread ({_threadCount} threads): {_multiTime.Value.TotalMilliseconds:F0} ms");
                sb.AppendLine($"  Speedup: {speedup:F2}x");
                sb.AppendLine("═══════════════════════════════════════════");
            }

            return sb.ToString();
        }

        public void SaveToFile(string path = "performance_log.txt")
        {
            File.WriteAllText(path, GetComparisonReport());
        }

        public List<string> Entries => _entries;
    }
}
