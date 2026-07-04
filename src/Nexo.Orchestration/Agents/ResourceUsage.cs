using Microsoft.Extensions.Logging;
using Nexo.Abstractions.Agents;
using Nexo.Orchestration.Architect.Models;

namespace Nexo.Orchestration.Agents;

/// <summary>
/// Resource usage statistics for an agent.
/// </summary>
public sealed record ResourceUsage
{
    /// <summary>
    /// Gets the agent ID.
    /// </summary>
    public required string AgentId { get; init; }

    /// <summary>
    /// Gets the execution duration.
    /// </summary>
    public TimeSpan Duration { get; init; }

    /// <summary>
    /// Gets the estimated compute seconds (from agent spec).
    /// </summary>
    public int? EstimatedComputeSeconds { get; init; }

    /// <summary>
    /// Gets the required context tokens (from agent spec).
    /// </summary>
    public int? RequiredContextTokens { get; init; }

    /// <summary>
    /// Gets the required memory in MB (from agent spec).
    /// </summary>
    public int? RequiredMemoryMB { get; init; }
}
