namespace Nexo.Core.Application.Agent.Models;

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
    public required string AgentName { get; init; }
    public required bool Success { get; init; }
    public required string Message { get; init; }
    public DateTime ExecutedAt { get; init; } = DateTime.UtcNow;
    public TimeSpan? Duration { get; init; }
    public object? Output { get; init; }
}

