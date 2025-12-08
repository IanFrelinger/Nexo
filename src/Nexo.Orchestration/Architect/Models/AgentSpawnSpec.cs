using System.Text.Json;

namespace Nexo.Orchestration.Architect.Models;

/// <summary>
/// Specification for spawning a specialized agent to handle a specific task.
/// </summary>
public sealed record AgentSpawnSpec
{
    /// <summary>
    /// Unique identifier for this agent instance.
    /// </summary>
    public required string AgentId { get; init; }

    /// <summary>
    /// Domain or category this agent specializes in (e.g., "Combat", "Economy", "AI", "Infrastructure", "Security").
    /// </summary>
    public required string Domain { get; init; }

    /// <summary>
    /// Primary goal or objective for this agent.
    /// </summary>
    public required string Goal { get; init; }

    /// <summary>
    /// Detailed description of what this agent should accomplish.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// List of agent IDs that must complete before this agent can start.
    /// </summary>
    public IReadOnlyList<string> Dependencies { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Expected output schema (JSON schema) that this agent will produce.
    /// </summary>
    public JsonElement? OutputSchema { get; init; }

    /// <summary>
    /// Constraints this agent must satisfy (e.g., performance targets, resource limits).
    /// </summary>
    public IReadOnlyList<AgentConstraint> Constraints { get; init; } = Array.Empty<AgentConstraint>();

    /// <summary>
    /// Resource requirements for this agent (compute, context window, etc.).
    /// </summary>
    public ResourceRequirements? ResourceRequirements { get; init; }

    /// <summary>
    /// Priority level (higher = more critical).
    /// </summary>
    public int Priority { get; init; } = 0;
}

/// <summary>
/// Represents a constraint that an agent must satisfy.
/// </summary>
public sealed record AgentConstraint
{
    /// <summary>
    /// Type of constraint (e.g., "Performance", "Security", "Compatibility").
    /// </summary>
    public required string Type { get; init; }

    /// <summary>
    /// Constraint description or requirement.
    /// </summary>
    public required string Description { get; init; }

    /// <summary>
    /// Whether this constraint is mandatory or optional.
    /// </summary>
    public bool IsMandatory { get; init; } = true;
}

/// <summary>
/// Resource requirements for an agent.
/// </summary>
public sealed record ResourceRequirements
{
    /// <summary>
    /// Estimated compute time in seconds.
    /// </summary>
    public int? EstimatedComputeSeconds { get; init; }

    /// <summary>
    /// Required context window size in tokens.
    /// </summary>
    public int? RequiredContextTokens { get; init; }

    /// <summary>
    /// Memory requirements in MB.
    /// </summary>
    public int? RequiredMemoryMB { get; init; }
}

