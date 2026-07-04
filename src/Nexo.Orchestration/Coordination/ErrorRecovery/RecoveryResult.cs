using Microsoft.Extensions.Logging;
using Nexo.Orchestration.Agents;
using Nexo.Abstractions.Agents;

namespace Nexo.Orchestration.Coordination.ErrorRecovery;

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
    /// <summary>Whether the recovery attempt succeeded.</summary>
    public bool Success { get; init; }

    /// <summary>Whether another retry should be attempted.</summary>
    public bool ShouldRetry { get; init; }

    /// <summary>Delay before the next retry attempt.</summary>
    public TimeSpan RetryDelay { get; init; }

    /// <summary>Human-readable explanation of the outcome.</summary>
    public string? Reason { get; init; }
}
