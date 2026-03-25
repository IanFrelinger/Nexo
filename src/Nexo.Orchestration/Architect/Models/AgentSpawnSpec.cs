using System.Text.Json;

namespace Nexo.Orchestration.Architect.Models;

/// <summary>
/// Specification for spawning a specialized agent to handle a specific task.
/// 
/// Contains:
/// - Agent identification (ID, domain, goal, description)
/// - Dependencies on other agents
/// - Output schema requirements
/// - Constraints (performance, security, etc.)
/// - Resource requirements (compute, context, memory)
/// - Priority level
/// 
/// Created by ArchitectAgent during request decomposition.
/// Used by AgentFactory to instantiate specialized agents.
/// </summary>
public sealed record AgentSpawnSpec
{
    /// <summary>
    /// Unique identifier for this agent instance.
    /// </summary>
    public required string AgentId { get; init; }

    /// <summary>
    /// Friendly agent name for policy and diagnostics. Defaults to <see cref="AgentId"/>.
    /// </summary>
    public string Name { get; init; } = string.Empty;

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

    /// <summary>
    /// Capabilities this agent requires from the execution endpoint.
    /// </summary>
    public IReadOnlyList<string> RequiredCapabilities { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Optional metadata used by orchestration and routing policy.
    /// </summary>
    public IReadOnlyDictionary<string, object?> Metadata { get; init; } =
        new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Resolved target endpoint for remote execution. Null means in-process.
    /// </summary>
    public string? TargetEndpoint { get; init; }
}

/// <summary>
/// Represents a constraint that an agent must satisfy.
/// 
/// Contains:
/// - Constraint type (Performance, Security, Compatibility, etc.)
/// - Human-readable description
/// - Whether the constraint is mandatory or optional
/// 
/// Used to specify requirements that agents must meet during execution.
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
/// 
/// Contains:
/// - Estimated compute time in seconds
/// - Required context window size in tokens
/// - Memory requirements in MB
/// 
/// Used by ResourceAllocator to allocate resources to agents.
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

