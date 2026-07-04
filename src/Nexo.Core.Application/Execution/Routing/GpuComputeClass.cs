using Nexo.Core.Domain;
using Nexo.Core.Domain.Execution;

namespace Nexo.Core.Application.Execution.Routing;

/// <summary>
/// Classifies the GPU horsepower required by a job.
/// <para>
/// Used by <see cref="ICapabilityRouter"/> to match <see cref="JobRequirements"/>
/// against available execution targets.  The numeric ordering (<c>None</c> &lt;
/// <c>Low</c> &lt; … &lt; <c>Extreme</c>) is significant: the router will accept
/// any target whose advertised class is ≥ the requirement.
/// </para>
/// </summary>
public enum GpuComputeClass
{
    /// <summary>No GPU needed — CPU-only workloads.</summary>
    None = 0,
    /// <summary>Entry-level GPU (e.g. ≤ 8 GB VRAM).</summary>
    Low = 1,
    /// <summary>Mid-range GPU (e.g. A4000 / 16 GB class).</summary>
    Medium = 2,
    /// <summary>High-end GPU (e.g. A100 40 GB).</summary>
    High = 3,
    /// <summary>Multi-GPU or top-tier single GPU (e.g. A100 80 GB, H100).</summary>
    Extreme = 4
}
