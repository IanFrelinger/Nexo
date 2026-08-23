namespace Ashlar.Core.Domain.Bricks;

/// <summary>
/// Resource usage characteristics of an implementation.
/// </summary>
public enum ResourceUsage
{
    /// <summary>Minimal CPU and memory footprint.</summary>
    Low,

    /// <summary>Moderate CPU and memory footprint.</summary>
    Medium,

    /// <summary>High CPU, memory, or GPU utilization.</summary>
    High
}
