namespace Nexo.Core.Application.NodeCapabilityRuntime.Models;

/// <summary>
/// Maximum acceptable latency class for a task.
/// </summary>
public enum LatencyRequirement
{
    /// <summary>Sub-second response required (streaming or live interaction).</summary>
    RealTime,

    /// <summary>Interactive response within a few seconds is acceptable.</summary>
    Interactive,

    /// <summary>Background processing with no strict deadline.</summary>
    Background,

    /// <summary>Deferred batch work that may run during idle or overnight windows.</summary>
    Overnight
}
