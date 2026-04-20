using Microsoft.Extensions.Logging;
using Nexo.Orchestration.Agents;
using Nexo.Abstractions.Agents;

namespace Nexo.Orchestration.Coordination.ErrorRecovery;

/// <summary>
/// Manages error recovery and retry logic for agents.
/// 
/// Responsibilities:
/// - Attempts to recover from agent failures
/// - Implements retry logic with exponential backoff
/// - Tracks retry attempts and failure history
/// - Determines if recovery is possible
/// 
/// Used by Orchestrator to handle transient agent failures.
/// Provides automatic recovery for recoverable errors.
/// </summary>
public sealed class ErrorRecoveryManager
{
    private readonly ILogger<ErrorRecoveryManager> _logger;
    private readonly Dictionary<string, RetryInfo> _retryInfo = new();
    private readonly int _maxRetries;
    private readonly TimeSpan _retryDelay;

    /// <summary>
    /// Initializes a new instance of the <see cref="ErrorRecoveryManager"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="maxRetries">Maximum number of retry attempts (default: 3).</param>
    /// <param name="retryDelay">Base delay between retries (default: 5 seconds). Uses exponential backoff.</param>
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
    /// 
    /// Implements retry logic with exponential backoff:
    /// - Tracks retry count per agent
    /// - Determines if error is recoverable
    /// - Calculates exponential backoff delay
    /// - Returns recovery result indicating if retry should occur
    /// </summary>
    /// <param name="container">The agent container that failed.</param>
    /// <param name="exception">The exception that caused the failure.</param>
    /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
    /// <returns>A <see cref="RecoveryResult"/> indicating whether recovery is possible and when to retry.</returns>
    /// <exception cref="ArgumentNullException">Thrown if container is null.</exception>
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
    /// <param name="agentId">The ID of the agent whose retry info should be reset.</param>
    public void ResetRetryInfo(string agentId)
    {
        _retryInfo.Remove(agentId);
        _logger.LogDebug("Reset retry info for agent {AgentId}", agentId);
    }

    /// <summary>
    /// Gets retry information for an agent.
    /// </summary>
    /// <param name="agentId">The ID of the agent to get retry info for.</param>
    /// <returns>The <see cref="RetryInfo"/> for the agent, or null if not found.</returns>
    public RetryInfo? GetRetryInfo(string agentId)
    {
        return _retryInfo.TryGetValue(agentId, out var info) ? info : null;
    }

    /// <summary>
    /// Determines if an exception represents a recoverable error.
    /// 
    /// Non-recoverable errors include:
    /// - ArgumentNullException, InvalidOperationException, NotSupportedException
    /// 
    /// Recoverable errors include:
    /// - TimeoutException
    /// - Network/connection errors
    /// - Most other exceptions (default to recoverable)
    /// </summary>
    /// <param name="exception">The exception to check.</param>
    /// <returns>True if the error is recoverable, false otherwise.</returns>
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
/// 
/// Contains:
/// - Success status
/// - Whether retry should occur
/// - Retry delay duration
/// - Reason for the result
/// 
/// Returned by ErrorRecoveryManager.RecoverAsync().
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
/// 
/// Contains:
/// - Agent ID
/// - Retry count
/// - Last error encountered
/// - Last retry attempt timestamp
/// 
/// Tracked by ErrorRecoveryManager to manage retry state per agent.
/// </summary>
public sealed class RetryInfo
{
    public required string AgentId { get; init; }
    public int RetryCount { get; set; }
    public Exception? LastError { get; set; }
    public DateTimeOffset? LastRetryAttempt { get; set; }
}

