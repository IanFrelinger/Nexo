using Microsoft.Extensions.Logging;

namespace Ashlar.Tests.Infrastructure.Barriers.Sinks;

/// <summary>Test logger.</summary>
internal sealed class TestLogger<T> : ILogger<T>
{
    private readonly List<TestLogEntry> _entries = [];
    private readonly object _sync = new();

    public IReadOnlyList<TestLogEntry> Entries
    {
        get
        {
            lock (_sync)
            {
                return _entries.ToList();
            }
        }
    }

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
    {
        var properties = new Dictionary<string, object?>(StringComparer.Ordinal);
        if (state is IEnumerable<KeyValuePair<string, object?>> nullablePairs)
        {
            foreach (var pair in nullablePairs)
            {
                properties[pair.Key] = pair.Value;
            }
        }
        else if (state is IEnumerable<KeyValuePair<string, object>> pairs)
        {
            foreach (var pair in pairs)
            {
                properties[pair.Key] = pair.Value;
            }
        }

        var message = formatter(state, exception);
        lock (_sync)
        {
            _entries.Add(new TestLogEntry(logLevel, message, exception, properties));
        }
    }

    /// <summary>Null scope.</summary>
    private sealed class NullScope : IDisposable
    {
        public static readonly NullScope Instance = new();
        /// <summary>Dispose.</summary>
        public void Dispose() { }
    }
}
