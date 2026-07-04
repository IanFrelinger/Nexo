namespace Nexo.Core.Application.NodeCapabilityRuntime.Models;

/// <summary>
/// Minimum acceptable output quality for a task.
/// </summary>
public enum QualityRequirement
{
    /// <summary>Low quality is acceptable; favor speed and resource efficiency.</summary>
    Low,

    /// <summary>Balanced quality suitable for most general tasks.</summary>
    Medium,

    /// <summary>High quality required; prefer capable models over speed.</summary>
    High,

    /// <summary>Highest available quality regardless of latency or cost.</summary>
    Maximum
}
