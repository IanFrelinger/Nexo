using System;

namespace Nexo.Core.Application.Interfaces.Logging
{
    /// <summary>
    /// Represents a provider that can create loggers for specific categories.
    /// This interface abstracts logger provider functionality to allow for different logging implementations.
    /// </summary>
    public interface ILoggerProvider : IDisposable
    {
        ILogger CreateLogger(string categoryName);
    }
} 