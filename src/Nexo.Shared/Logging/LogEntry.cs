using System;
using System.Collections.Generic;

namespace Nexo.Shared.Logging
{
    /// <summary>
    /// Log entry structure
    /// </summary>
    public class LogEntry
    {
        public LogLevel Level { get; set; }
        public string Message { get; set; } = string.Empty;
        public Dictionary<string, object> Properties { get; set; } = new();
        public string CorrelationId { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }

    /// <summary>
    /// Log level enumeration
    /// </summary>
    public enum LogLevel
    {
        Trace,
        Debug,
        Information,
        Warning,
        Error,
        Critical
    }
}
