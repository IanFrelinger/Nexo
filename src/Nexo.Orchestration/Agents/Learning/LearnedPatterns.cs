using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace Nexo.Orchestration.Agents.Learning;

/// <summary>
/// Patterns learned from agent execution history.
/// 
/// Contains:
/// - Agent ID and execution statistics
/// - Success rate and average execution time
/// - Lists of common issues and best practices
/// 
/// Used by AgentMemory to provide insights for agent improvement.
/// </summary>
public sealed record LearnedPatterns
{
    /// <summary>Agent whose patterns were derived.</summary>
    public required string AgentId { get; init; }

    /// <summary>Total executions in the learning window.</summary>
    public required int TotalExecutions { get; init; }

    /// <summary>Fraction of executions that succeeded.</summary>
    public required double SuccessRate { get; init; }

    /// <summary>Mean execution duration.</summary>
    public required TimeSpan AverageExecutionTime { get; init; }

    /// <summary>Recurring issues observed in history.</summary>
    public IReadOnlyList<string> CommonIssues { get; init; } = new List<string>();

    /// <summary>Recommended practices inferred from successes.</summary>
    public IReadOnlyList<string> BestPractices { get; init; } = new List<string>();
}
