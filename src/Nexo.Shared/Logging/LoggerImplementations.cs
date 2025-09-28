using System;
using System.Collections.Generic;
using System.Linq;

namespace Nexo.Shared.Logging
{
    /// <summary>
    /// Console logger implementation
    /// </summary>
    public class ConsoleLogger : ILogger
    {
        public void Log(LogEntry entry)
        {
            var timestamp = entry.Timestamp.ToString("yyyy-MM-dd HH:mm:ss.fff");
            var level = entry.Level.ToString().ToUpper();
            var correlationId = string.IsNullOrEmpty(entry.CorrelationId) ? "" : $" [{entry.CorrelationId}]";
            
            Console.WriteLine($"[{timestamp}] {level}{correlationId}: {entry.Message}");
            
            if (entry.Properties.Count > 0)
            {
                foreach (var (key, value) in entry.Properties)
                {
                    Console.WriteLine($"  {key}: {value}");
                }
            }
        }
    }

    /// <summary>
    /// File logger implementation
    /// </summary>
    public class FileLogger : ILogger
    {
        private readonly string _filePath;

        public FileLogger(string filePath)
        {
            _filePath = filePath;
        }

        public void Log(LogEntry entry)
        {
            var logLine = $"{entry.Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{entry.Level}] [{entry.CorrelationId}] {entry.Message}";
            
            if (entry.Properties.Count > 0)
            {
                logLine += $" | Properties: {string.Join(", ", entry.Properties.Select(kvp => $"{kvp.Key}={kvp.Value}"))}";
            }
            
            System.IO.File.AppendAllText(_filePath, logLine + Environment.NewLine);
        }
    }
}
