using Microsoft.Extensions.Logging;

namespace Ashlar.Orchestration.Agents;

/// <summary>
/// Adapter to convert ILogger<T> to ILogger<BaseAgent>.
/// 
/// Allows domain-specific agents (e.g., GameplayAgent, SecurityAgent) to use
/// their specific logger type while BaseAgent requires ILogger<BaseAgent>.
/// 
/// Implements the Adapter pattern to bridge the logger type mismatch.
/// </summary>
internal sealed class LoggerAdapter<T> : ILogger<BaseAgent>
{
    private readonly ILogger<T> _logger;

    public LoggerAdapter(ILogger<T> logger)
    {
        _logger = logger;
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => _logger.BeginScope(state);

    public bool IsEnabled(LogLevel logLevel) => _logger.IsEnabled(logLevel);

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        _logger.Log(logLevel, eventId, state, exception, formatter);
    }
}

