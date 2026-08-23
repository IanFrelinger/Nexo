namespace Ashlar.Orchestration.Coordination.Conflicts;

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
