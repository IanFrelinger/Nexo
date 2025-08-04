using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Nexo.Core.Application.Enums;

namespace Nexo.Core.Application.Interfaces.Logging
{
    /// <summary>
    /// Represents a logger that can be used to log messages at different levels.
    /// This interface abstracts logging functionality to allow for different logging implementations.
    /// </summary>
    public interface ILogger
    {
        void Log(LogLevel level, string message, params object[] args);
        void Log(LogLevel level, Exception exception, string message, params object[] args);
        void Log(LogLevel level, string message, IDictionary<string, object> properties, params object[] args);
        void Log(LogLevel level, Exception exception, string message, IDictionary<string, object> properties, params object[] args);
        void LogTrace(string message, params object[] args);
        void LogTrace(Exception exception, string message, params object[] args);
        void LogDebug(string message, params object[] args);
        void LogDebug(Exception exception, string message, params object[] args);
        void LogInformation(string message, params object[] args);
        void LogInformation(Exception exception, string message, params object[] args);
        void LogWarning(string message, params object[] args);
        void LogWarning(Exception exception, string message, params object[] args);
        void LogError(string message, params object[] args);
        void LogError(Exception exception, string message, params object[] args);
        void LogCritical(string message, params object[] args);
        void LogCritical(Exception exception, string message, params object[] args);
        bool IsEnabled(LogLevel level);
        IDisposable BeginScope(string state);
        IDisposable BeginScope(string state, IDictionary<string, object> properties);
    }
} 