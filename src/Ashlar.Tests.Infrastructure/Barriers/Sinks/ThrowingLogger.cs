using Microsoft.Extensions.Logging;

namespace Ashlar.Tests.Infrastructure.Barriers.Sinks;

/// <summary>Throwing logger.</summary>
internal sealed class ThrowingLogger<T> : ILogger<T>
{
    public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;
    /// <summary>Returns whether  enabled.</summary>
    /// <param name="logLevel">Log level.</param>
    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
        => throw new InvalidOperationException("Injected logger failure.");

    /// <summary>Null scope.</summary>
    private sealed class NullScope : IDisposable
    {
        public static readonly NullScope Instance = new();
        /// <summary>Dispose.</summary>
        public void Dispose() { }
    }
}
