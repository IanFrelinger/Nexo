using Microsoft.Extensions.Logging;
using Nexo.Orchestration.Agents;
using Nexo.Orchestration.Agents.Models;

namespace Nexo.Orchestration.Coordination.ErrorRecovery;

/// <summary>
/// Manages error recovery and retry logic for agents.
/// </summary>
public sealed class ErrorRecoveryManager
{
    private readonly ILogger<ErrorRecoveryManager> _logger;
    private readonly Dictionary<string, RetryInfo> _retryInfo = new();
    private readonly int _maxRetries;
    private readonly TimeSpan _retryDelay;

    public ErrorRecoveryManager(
        ILogger<ErrorRecoveryManager> logger,
        int maxRetries = 3,
        TimeSpan? retryDelay = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _maxRetries = maxRetries;
        _retryDelay = retryDelay ?? TimeSpan.FromSeconds(5);
    }

    /// <summary>
    /// Attempts to recover from an agent failure.
    /// </summary>
    public async Task<RecoveryResult> RecoverAsync(
        AgentContainer container,
        Exception exception,
        CancellationToken cancellationToken = default)
    {
        if (container == null)
        {
            throw new ArgumentNullException(nameof(container));
        }

        var agentId = container.AgentId;

        if (!_retryInfo.TryGetValue(agentId, out var retryInfo))
        {
            retryInfo = new RetryInfo
            {
                AgentId = agentId,
                RetryCount = 0,
                LastError = exception
            };
            _retryInfo[agentId] = retryInfo;
        }

        retryInfo.RetryCount++;
        retryInfo.LastError = exception;
        retryInfo.LastRetryAttempt = DateTimeOffset.UtcNow;

        _logger.LogWarning(
            "Agent {AgentId} failed (attempt {Attempt}/{MaxRetries}): {Error}",
            agentId, retryInfo.RetryCount, _maxRetries, exception.Message);

        if (retryInfo.RetryCount > _maxRetries)
        {
            _logger.LogError(
                "Agent {AgentId} exceeded max retries ({MaxRetries}). Marking as failed.",
                agentId, _maxRetries);

            return new RecoveryResult
            {
                Success = false,
                ShouldRetry = false,
                RetryDelay = TimeSpan.Zero,
                Reason = $"Exceeded max retries ({_maxRetries})"
            };
        }

        // Determine if error is recoverable
        if (!IsRecoverableError(exception))
        {
            _logger.LogError(
                "Agent {AgentId} encountered non-recoverable error: {ErrorType}",
                agentId, exception.GetType().Name);

            return new RecoveryResult
            {
                Success = false,
                ShouldRetry = false,
                RetryDelay = TimeSpan.Zero,
                Reason = $"Non-recoverable error: {exception.GetType().Name}"
            };
        }

        // Calculate exponential backoff
        var delay = TimeSpan.FromMilliseconds(
            _retryDelay.TotalMilliseconds * Math.Pow(2, retryInfo.RetryCount - 1));

        _logger.LogInformation(
            "Scheduling retry for agent {AgentId} in {Delay}ms (attempt {Attempt})",
            agentId, delay.TotalMilliseconds, retryInfo.RetryCount);

        await Task.Delay(delay, cancellationToken);

        return new RecoveryResult
        {
            Success = true,
            ShouldRetry = true,
            RetryDelay = delay,
            Reason = "Scheduled retry"
        };
    }

    /// <summary>
    /// Resets retry information for an agent (after successful execution).
    /// </summary>
    public void ResetRetryInfo(string agentId)
    {
        _retryInfo.Remove(agentId);
        _logger.LogDebug("Reset retry info for agent {AgentId}", agentId);
    }

    /// <summary>
    /// Gets retry information for an agent.
    /// </summary>
    public RetryInfo? GetRetryInfo(string agentId)
    {
        return _retryInfo.TryGetValue(agentId, out var info) ? info : null;
    }

    private static bool IsRecoverableError(Exception exception)
    {
        // Non-recoverable errors
        var nonRecoverableTypes = new[]
        {
            typeof(ArgumentNullException),
            typeof(InvalidOperationException),
            typeof(NotSupportedException)
        };

        if (nonRecoverableTypes.Contains(exception.GetType()))
        {
            return false;
        }

        // Timeout errors are recoverable
        if (exception is TimeoutException)
        {
            return true;
        }

        // Network errors are recoverable
        if (exception.Message.Contains("network", StringComparison.OrdinalIgnoreCase) ||
            exception.Message.Contains("connection", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // Default to recoverable
        return true;
    }
}

/// <summary>
/// Result of a recovery attempt.
/// </summary>
public sealed record RecoveryResult
{
    public bool Success { get; init; }
    public bool ShouldRetry { get; init; }
    public TimeSpan RetryDelay { get; init; }
    public string? Reason { get; init; }
}

/// <summary>
/// Information about retry attempts for an agent.
/// </summary>
public sealed class RetryInfo
{
    public required string AgentId { get; init; }
    public int RetryCount { get; set; }
    public Exception? LastError { get; set; }
    public DateTimeOffset? LastRetryAttempt { get; set; }
}

