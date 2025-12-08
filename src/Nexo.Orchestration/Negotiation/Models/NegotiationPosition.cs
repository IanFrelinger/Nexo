using Nexo.Orchestration.Architect.Models;

namespace Nexo.Orchestration.Negotiation.Models;

/// <summary>
/// Represents an agent's position in a negotiation.
/// </summary>
public sealed record NegotiationPosition
{
    public required string AgentId { get; init; }
    public required string Domain { get; init; }

    /// <summary>
    /// The agent's primary goal that must be achieved.
    /// </summary>
    public required string PrimaryGoal { get; init; }

    /// <summary>
    /// Underlying goals that explain WHY the primary goal matters.
    /// Used for finding creative resolutions.
    /// </summary>
    public IReadOnlyList<string> UnderlyingGoals { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Hard constraints that cannot be violated.
    /// </summary>
    public IReadOnlyList<string> HardConstraints { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Soft constraints with flexibility scores (0-1, higher = more flexible).
    /// </summary>
    public IReadOnlyDictionary<string, double> SoftConstraints { get; init; } =
        new Dictionary<string, double>();

    /// <summary>
    /// Resources the agent requires.
    /// </summary>
    public ResourceRequirements? Resources { get; init; }

    /// <summary>
    /// Overall flexibility score (0-1). Higher = more willing to compromise.
    /// </summary>
    public double FlexibilityScore { get; init; }
}

/// <summary>
/// Result of impact analysis for a single agent.
/// </summary>
public sealed record ImpactAnalysis
{
    public required string AgentId { get; init; }

    /// <summary>
    /// Impact score if this agent yields (0-1). Lower = less impact = should yield first.
    /// </summary>
    public double ImpactScore { get; init; }

    /// <summary>
    /// Description of what happens if this agent yields.
    /// </summary>
    public string? ImpactDescription { get; init; }
}

/// <summary>
/// A proposed resolution to a conflict.
/// </summary>
public sealed record ProposedResolution
{
    public required string ProposerId { get; init; }
    public required string Description { get; init; }

    /// <summary>
    /// Changes required from each agent (agentId → required changes).
    /// </summary>
    public IReadOnlyDictionary<string, string> RequiredChanges { get; init; } =
        new Dictionary<string, string>();

    /// <summary>
    /// The resolved artifact (schema, resource allocation, etc.)
    /// </summary>
    public object? ResolvedArtifact { get; init; }

    /// <summary>
    /// Confidence that this resolution will work (0-1).
    /// </summary>
    public double Confidence { get; init; }
}

