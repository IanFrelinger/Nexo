using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;

namespace FeatureFactoryDemo
{
    /// <summary>
    /// Test infrastructure and models
    /// </summary>
    public partial class LoggingSystemValidation
    {
        // Test infrastructure is defined in this partial class
    }

    /// <summary>
    /// Test logger provider that captures logs for testing
    /// </summary>
    public class TestLoggerProvider : ILoggerProvider
    {
        private readonly List<TestLogEntry> _logs = new();
        private readonly object _lock = new();

        public ILogger CreateLogger(string categoryName)
        {
            return new TestLogger(categoryName, this);
        }

        public void AddLog(TestLogEntry logEntry)
        {
            lock (_lock)
            {
                _logs.Add(logEntry);
            }
        }

        public List<TestLogEntry> GetLogs()
        {
            lock (_lock)
            {
                return new List<TestLogEntry>(_logs);
            }
        }

        public void ClearLogs()
        {
            lock (_lock)
            {
                _logs.Clear();
            }
        }

        public void Dispose()
        {
            // Nothing to dispose
        }
    }

    /// <summary>
    /// Test logger implementation
    /// </summary>
    public class TestLogger : ILogger
    {
        private readonly string _categoryName;
        private readonly TestLoggerProvider _provider;

        public TestLogger(string categoryName, TestLoggerProvider provider)
        {
            _categoryName = categoryName;
            _provider = provider;
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            return new TestScope();
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true; // Enable all levels for testing
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            var message = formatter(state, exception);
            var logEntry = new TestLogEntry
            {
                Level = logLevel,
                Message = message,
                Exception = exception,
                CategoryName = _categoryName,
                EventId = eventId,
                Timestamp = DateTime.UtcNow
            };
            _provider.AddLog(logEntry);
        }
    }

    /// <summary>
    /// Test log entry model
    /// </summary>
    public class TestLogEntry
    {
        public LogLevel Level { get; set; }
        public string Message { get; set; } = string.Empty;
        public Exception? Exception { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public EventId EventId { get; set; }
        public DateTime Timestamp { get; set; }
    }

    /// <summary>
    /// Test scope implementation
    /// </summary>
    public class TestScope : IDisposable
    {
        public void Dispose()
        {
            // Nothing to dispose
        }
    }

    /// <summary>
    /// Validation result model
    /// </summary>
    public class ValidationResult
    {
        public bool OverallSuccess { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public TestResult BasicDependencyInjection { get; set; } = new();
        public TestResult LoggerTypeSafety { get; set; } = new();
        public TestResult LogLevels { get; set; } = new();
        public TestResult StructuredLogging { get; set; } = new();
        public TestResult ExceptionLogging { get; set; } = new();
        public TestResult ScopeFunctionality { get; set; } = new();
        public TestResult ServiceLifetimeManagement { get; set; } = new();
        public TestResult Performance { get; set; } = new();
        public TestResult ConcurrentOperations { get; set; } = new();
        public TestResult MemoryUsage { get; set; } = new();
    }

    /// <summary>
    /// Test result model
    /// </summary>
    public class TestResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
