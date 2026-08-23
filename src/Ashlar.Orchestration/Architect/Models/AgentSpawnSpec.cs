using System.Text.Json;
using Ashlar.Abstractions.Execution;

namespace Ashlar.Orchestration.Architect.Models;

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
    /// Additional goals for the agent. If omitted, defaults to the primary <see cref="Goal"/>.
    /// </summary>
    public IReadOnlyList<string> Goals { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Detailed description of what this agent should accomplish.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Optional cluster identifier used to group related agents.
    /// </summary>
    public string? ClusterId { get; init; }

    /// <summary>
    /// Optional direct supervisor agent ID in the chain of command.
    /// </summary>
    public string? ReportsToAgentId { get; init; }

    /// <summary>
    /// Optional command chain, ordered from nearest supervisor to highest authority.
    /// </summary>
    public IReadOnlyList<string> CommandChain { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Optional Ollama model name to use for this specific agent.
    /// </summary>
    public string? OllamaModel { get; init; }

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
    /// How strongly this agent's invocations should be isolated at the transport/execution boundary.
    /// Defaults to <see cref="AgentExecutionIsolationLevel.InProcess"/>; hosts map higher tiers to
    /// processes, pooled containers, or dedicated containers per agent.
    /// </summary>
    public AgentExecutionIsolationLevel ExecutionIsolation { get; init; } = AgentExecutionIsolationLevel.InProcess;

    /// <summary>
    /// Resolved target endpoint for remote execution. Null means in-process.
    /// </summary>
    public string? TargetEndpoint { get; init; }
}
