namespace Ashlar.Core.Application.Agent.Models;

/// <summary>
/// Result of an agent execution.
/// 
/// Contains:
/// - Agent name and execution status
/// - Success/failure indication
/// - Result message
/// - Execution timestamp and duration
/// - Optional output data
/// 
/// Produced by IAgentExecutor after executing an agent.
/// Used by CLI commands to display agent execution results.
/// </summary>
public record AgentExecutionResult
{
    /// <summary>Name of the agent that was executed.</summary>
    public required string AgentName { get; init; }

    /// <summary>Whether the agent completed successfully.</summary>
    public required bool Success { get; init; }

    /// <summary>Human-readable result or error message.</summary>
    public required string Message { get; init; }

    /// <summary>UTC timestamp when execution completed.</summary>
    public DateTime ExecutedAt { get; init; } = DateTime.UtcNow;

    /// <summary>Wall-clock duration of the agent run, when measured.</summary>
    public TimeSpan? Duration { get; init; }

    /// <summary>Optional structured output from the agent.</summary>
    public object? Output { get; init; }
}
