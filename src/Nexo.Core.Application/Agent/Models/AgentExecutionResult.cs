namespace Nexo.Core.Application.Agent.Models;

/// <summary>
/// Result of an agent execution.
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

