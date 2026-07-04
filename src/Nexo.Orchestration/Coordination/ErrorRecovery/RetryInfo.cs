using Microsoft.Extensions.Logging;
using Nexo.Orchestration.Agents;
using Nexo.Abstractions.Agents;

namespace Nexo.Orchestration.Coordination.ErrorRecovery;

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
    /// <summary>Agent whose retries are tracked.</summary>
    public required string AgentId { get; init; }

    /// <summary>Number of retry attempts so far.</summary>
    public int RetryCount { get; set; }

    /// <summary>Most recent error that triggered a retry.</summary>
    public Exception? LastError { get; set; }

    /// <summary>UTC timestamp of the most recent retry attempt.</summary>
    public DateTimeOffset? LastRetryAttempt { get; set; }
}
