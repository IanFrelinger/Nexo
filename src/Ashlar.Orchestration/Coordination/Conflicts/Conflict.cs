namespace Ashlar.Orchestration.Coordination.Conflicts;

/// <summary>
/// Represents a conflict between agents.
/// 
/// Contains:
/// - Conflict type (Schema, Resource, Constraint, Philosophy)
/// - Agent IDs involved in the conflict
/// - Human-readable description
/// - Severity level (Low, Medium, High, Critical)
/// - Additional metadata
/// 
/// Used by ConflictDetector and NegotiationProtocol to identify and resolve conflicts.
/// </summary>
public sealed record Conflict
{
    /// <summary>
    /// Type of conflict.
    /// </summary>
    public required ConflictType ConflictType { get; init; }

    /// <summary>
    /// IDs of agents involved in the conflict.
    /// </summary>
    public required IReadOnlyList<string> AgentIds { get; init; }

    /// <summary>
    /// Human-readable description of the conflict.
    /// </summary>
    public required string Description { get; init; }

    /// <summary>
    /// Severity of the conflict.
    /// </summary>
    public ConflictSeverity Severity { get; init; } = ConflictSeverity.Medium;

    /// <summary>
    /// Additional context or metadata about the conflict.
    /// </summary>
    public Dictionary<string, object>? Metadata { get; init; }
}
