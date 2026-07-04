namespace Nexo.Commercial.Fleet.Infrastructure;

/// <summary>
/// Phase 6: execution leases and optional sweep for checkpoint / migrate flows.
/// </summary>
public sealed class MeshCheckpointOptions
{
    /// <summary>Constant value for section path.</summary>
    public const string SectionPath = "Nexo:Mesh:Checkpoint";

    /// <summary>Default lease duration after assignment (seconds).</summary>
    public int LeaseSeconds { get; set; } = 1800;

    /// <summary>When true, runs background sweep that expires stale Assigned/Running leases.</summary>
    public bool SweepEnabled { get; set; }

    /// <summary>Minutes between sweep passes.</summary>
    public int SweepIntervalMinutes { get; set; } = 1;
}
