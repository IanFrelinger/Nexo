namespace Nexo.Commercial.Fleet.Infrastructure;

/// <summary>
/// Phase 5: periodic re-attempt placement for stale pending mesh tasks.
/// </summary>
public sealed class MeshElasticSchedulingOptions
{
    public const string SectionPath = "Nexo:Mesh:Elastic";

    /// <summary>When true, runs <see cref="MeshPendingTaskRebalancerBackgroundService"/>.</summary>
    public bool Enabled { get; set; }

    /// <summary>Minutes between rebalance rounds.</summary>
    public int IntervalMinutes { get; set; } = 2;

    /// <summary>Re-invoke placement for <see cref="Nexo.Commercial.Fleet.Contracts.Models.MeshTaskStatus.Pending"/> tasks older than this (seconds).</summary>
    public int PendingStaleSeconds { get; set; } = 120;
}
