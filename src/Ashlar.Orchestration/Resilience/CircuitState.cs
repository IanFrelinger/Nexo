using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace Ashlar.Orchestration.Resilience;

/// <summary>
/// State of a circuit breaker.
/// </summary>
internal sealed class CircuitState
{
    /// <summary>Operation key tracked by this circuit breaker.</summary>
    public required string OperationKey { get; init; }

    /// <summary>Current circuit breaker state.</summary>
    public CircuitStateType State { get; set; }

    /// <summary>Count of consecutive failures.</summary>
    public int ConsecutiveFailures { get; set; }

    /// <summary>Count of consecutive successes.</summary>
    public int ConsecutiveSuccesses { get; set; }

    /// <summary>UTC timestamp of the most recent failure.</summary>
    public DateTimeOffset? LastFailureTime { get; set; }

    /// <summary>UTC timestamp of the most recent success.</summary>
    public DateTimeOffset? LastSuccessTime { get; set; }

    /// <summary>Most recent error that triggered failure tracking.</summary>
    public Exception? LastError { get; set; }

    // Lock for thread-safe state transitions
    public readonly object SyncLock = new();
}
