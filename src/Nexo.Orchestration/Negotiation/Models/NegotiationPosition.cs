using Nexo.Orchestration.Architect.Models;

namespace Nexo.Orchestration.Negotiation.Models;

/// <summary>
/// Represents an agent's position in a negotiation.
/// 
/// Contains:
/// - Primary goal and underlying goals (why it matters)
/// - Hard constraints (cannot be violated)
/// - Soft constraints with flexibility scores (can be relaxed)
/// - Resource requirements
/// - Overall flexibility score
/// 
/// Used by NegotiationProtocol to understand each agent's requirements and constraints.
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
