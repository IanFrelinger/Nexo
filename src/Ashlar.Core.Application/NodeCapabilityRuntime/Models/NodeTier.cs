namespace Ashlar.Core.Application.NodeCapabilityRuntime.Models;

/// <summary>
/// Capability tier of the current node.
/// </summary>
public enum NodeTier
{
    /// <summary>Minimal capability (mobile, edge, or severely constrained hardware).</summary>
    Nano,

    /// <summary>Light workloads with limited VRAM and concurrent inference.</summary>
    Micro,

    /// <summary>General-purpose workstation or mid-range GPU node.</summary>
    Standard,

    /// <summary>High-capacity node with substantial VRAM and throughput.</summary>
    Core
}
