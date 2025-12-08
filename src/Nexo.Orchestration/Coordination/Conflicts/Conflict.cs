namespace Nexo.Orchestration.Coordination.Conflicts;

/// <summary>
/// Represents a conflict between agents.
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

/// <summary>
/// Types of conflicts that can occur between agents.
/// </summary>
public enum ConflictType
{
    /// <summary>
    /// Agents have incompatible output schemas.
    /// </summary>
    Schema,

    /// <summary>
    /// Agents compete for the same resources.
    /// </summary>
    Resource,

    /// <summary>
    /// Agents have contradictory constraints.
    /// </summary>
    Constraint,

    /// <summary>
    /// Agents have conflicting design philosophies or goals.
    /// </summary>
    Philosophy
}

/// <summary>
/// Severity levels for conflicts.
/// </summary>
public enum ConflictSeverity
{
    /// <summary>
    /// Low severity - may cause minor issues but can be worked around.
    /// </summary>
    Low,

    /// <summary>
    /// Medium severity - may cause problems but has workarounds.
    /// </summary>
    Medium,

    /// <summary>
    /// High severity - likely to cause significant issues, requires resolution.
    /// </summary>
    High,

    /// <summary>
    /// Critical severity - blocks execution, must be resolved.
    /// </summary>
    Critical
}

