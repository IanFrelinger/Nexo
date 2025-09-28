using System;
using System.Collections.Generic;
using System.Linq;

namespace Nexo.Shared.Logging
{
    /// <summary>
    /// Centralized logging system
    /// </summary>
    public static class LoggingManager
    {
        private static readonly List<ILogger> _loggers = new();
        private static LogLevel _minimumLevel = LogLevel.Information;

        /// <summary>
        /// Adds a logger to the system
        /// </summary>
        public static void AddLogger(ILogger logger)
        {
            _loggers.Add(logger);
        }

        /// <summary>
        /// Sets the minimum log level
        /// </summary>
        public static void SetMinimumLevel(LogLevel level)
        {
            _minimumLevel = level;
        }

        /// <summary>
        /// Logs an information message
        /// </summary>
        public static void LogInformation(string message, Dictionary<string, object>? properties = null, string? correlationId = null)
        {
            Log(LogLevel.Information, message, properties, correlationId);
        }

        /// <summary>
        /// Logs a warning message
        /// </summary>
        public static void LogWarning(string message, Dictionary<string, object>? properties = null, string? correlationId = null)
        {
            Log(LogLevel.Warning, message, properties, correlationId);
        }

        /// <summary>
        /// Logs an error message
        /// </summary>
        public static void LogError(string message, Exception? exception = null, Dictionary<string, object>? properties = null, string? correlationId = null)
        {
            var errorProperties = properties ?? new Dictionary<string, object>();
            if (exception != null)
            {
                errorProperties["Exception"] = exception.ToString();
                errorProperties["ExceptionType"] = exception.GetType().Name;
            }
            
            Log(LogLevel.Error, message, errorProperties, correlationId);
        }

        /// <summary>
        /// Logs a debug message
        /// </summary>
        public static void LogDebug(string message, Dictionary<string, object>? properties = null, string? correlationId = null)
        {
            Log(LogLevel.Debug, message, properties, correlationId);
        }

        /// <summary>
        /// Logs a critical message
        /// </summary>
        public static void LogCritical(string message, Exception? exception = null, Dictionary<string, object>? properties = null, string? correlationId = null)
        {
            var criticalProperties = properties ?? new Dictionary<string, object>();
            if (exception != null)
            {
                criticalProperties["Exception"] = exception.ToString();
                criticalProperties["ExceptionType"] = exception.GetType().Name;
            }
            
            Log(LogLevel.Critical, message, criticalProperties, correlationId);
        }

        /// <summary>
        /// Logs a structured message
        /// </summary>
        public static void Log(LogLevel level, string message, Dictionary<string, object>? properties = null, string? correlationId = null)
        {
            if (level < _minimumLevel)
                return;

            var logEntry = new LogEntry
            {
                Level = level,
                Message = message,
                Properties = properties ?? new Dictionary<string, object>(),
                CorrelationId = correlationId ?? Guid.NewGuid().ToString(),
                Timestamp = DateTime.UtcNow
            };

            foreach (var logger in _loggers)
            {
                try
                {
                    logger.Log(logEntry);
                }
                catch
                {
                    // Don't let logging failures break the application
                }
            }
        }

        /// <summary>
        /// Clears all loggers
        /// </summary>
        public static void ClearLoggers()
        {
            _loggers.Clear();
        }
    }
}
