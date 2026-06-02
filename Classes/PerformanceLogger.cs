using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace PasswordCracker.Classes
{
    public class PerformanceLogger
    {
        private readonly List<string> _logEntries = new List<string>();

        public TimeSpan? LastSingleThreadTime { get; private set; }
        public TimeSpan? LastMultiThreadTime { get; private set; }
        public int LastThreadCount { get; private set; }

        public void LogSingleThread(string password, TimeSpan elapsed, long attempts)
        {
            LastSingleThreadTime = elapsed;
            string entry = $"[{DateTime.Now:HH:mm:ss}] SINGLE-THREAD | Password: '{password}' | " +
                           $"Time: {elapsed.TotalSeconds:F3}s | Attempts: {attempts:N0}";
            _logEntries.Add(entry);
        }

        public void LogMultiThread(string password, TimeSpan elapsed, long attempts, int threadCount)
        {
            LastMultiThreadTime = elapsed;
            LastThreadCount = threadCount;
            string entry = $"[{DateTime.Now:HH:mm:ss}] MULTI-THREAD  | Password: '{password}' | " +
                           $"Time: {elapsed.TotalSeconds:F3}s | Attempts: {attempts:N0} | Threads: {threadCount}";
            _logEntries.Add(entry);
        }

        public string GetComparisonReport()
        {
            var sb = new StringBuilder();
            sb.AppendLine("╔══════════════════════════════════════════════════╗");
            sb.AppendLine("║         PERFORMANCE COMPARISON REPORT            ║");
            sb.AppendLine("╠══════════════════════════════════════════════════╣");

            if (LastSingleThreadTime.HasValue)
                sb.AppendLine($"║  Single-Thread : {LastSingleThreadTime.Value.TotalSeconds,8:F3} seconds                ║");
            else
                sb.AppendLine("║  Single-Thread : Not run yet                     ║");

            if (LastMultiThreadTime.HasValue)
                sb.AppendLine($"║  Multi-Thread  : {LastMultiThreadTime.Value.TotalSeconds,8:F3} seconds ({LastThreadCount} threads)   ║");
            else
                sb.AppendLine("║  Multi-Thread  : Not run yet                     ║");

            if (LastSingleThreadTime.HasValue && LastMultiThreadTime.HasValue)
            {
                double speedup = LastSingleThreadTime.Value.TotalSeconds /
                                 LastMultiThreadTime.Value.TotalSeconds;
                sb.AppendLine("╠══════════════════════════════════════════════════╣");
                sb.AppendLine($"║  Speedup       : {speedup,8:F2}x faster                    ║");
            }

            sb.AppendLine("╚══════════════════════════════════════════════════╝");
            return sb.ToString();
        }

        public string GetAllLogs() => string.Join(Environment.NewLine, _logEntries);

        public void SaveToFile()
        {
            try
            {
                string desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                string path = Path.Combine(desktop,
                    $"PasswordCracker_Log_{DateTime.Now:yyyyMMdd_HHmmss}.txt");
                File.WriteAllText(path, GetAllLogs() + Environment.NewLine + GetComparisonReport());
            }
            catch { }
        }

        public void Clear()
        {
            _logEntries.Clear();
            LastSingleThreadTime = null;
            LastMultiThreadTime = null;
        }
    }
}