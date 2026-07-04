using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace Nexo.Orchestration.Agents.Learning;

/// <summary>
/// Record of an agent execution.
/// 
/// Contains:
/// - Agent ID and execution timestamp
/// - Duration and success status
/// - Optional error message
/// - Optional metadata dictionary
/// 
/// Used by AgentMemory to track execution history.
/// </summary>
public sealed record ExecutionRecord
{
    /// <summary>Agent that performed the execution.</summary>
    public required string AgentId { get; init; }

    /// <summary>UTC timestamp when execution started.</summary>
    public required DateTimeOffset ExecutedAt { get; init; }

    /// <summary>Elapsed execution duration.</summary>
    public required TimeSpan Duration { get; init; }

    /// <summary>Whether the execution completed successfully.</summary>
    public required bool Success { get; init; }

    /// <summary>Error message when execution failed.</summary>
    public string? ErrorMessage { get; init; }

    /// <summary>Additional execution metadata.</summary>
    public IReadOnlyDictionary<string, object> Metadata { get; init; } = new Dictionary<string, object>();
}
