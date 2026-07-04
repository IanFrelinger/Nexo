namespace Nexo.Orchestration.Coordination.Conflicts;

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
