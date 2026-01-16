using Nexo.Core.Domain.Bricks;
using Nexo.Core.Domain.Workflows;

namespace Nexo.Core.Domain.Execution;

/// <summary>
/// Result of executing a behavior.
/// </summary>
public class BehaviorResult
{
    public bool Success { get; init; }
    public Dictionary<string, object> Outputs { get; init; } = new();
    public IReadOnlyList<string> Errors { get; init; } = [];
    public TimeSpan Duration { get; init; }
}

/// <summary>
/// Options for behavior execution.
/// </summary>
public class ExecutionOptions
{
    public bool IsAirGapped { get; init; }
    public bool AuditMode { get; init; }
    public string Provider { get; init; } = "openai";
    
    /// <summary>
    /// Implementation mode for the execution.
    /// </summary>
    public ImplementationMode ImplementationMode { get; init; } = ImplementationMode.Auto;
    
    /// <summary>
    /// Per-behavior implementation overrides.
    /// </summary>
    public IReadOnlyDictionary<string, ImplementationMode> BehaviorOverrides { get; init; } 
        = new Dictionary<string, ImplementationMode>();
    
    /// <summary>
    /// Per-brick implementation overrides.
    /// </summary>
    public IReadOnlyDictionary<string, ImplementationType> BrickOverrides { get; init; } 
        = new Dictionary<string, ImplementationType>();

    /// <summary>
    /// Runtime per-brick preferences + fallback chains (hot-swappable implementations).
    /// Keyed by brick id.
    /// </summary>
    public IReadOnlyDictionary<string, BrickRuntimeSpec> BrickRuntime { get; init; }
        = new Dictionary<string, BrickRuntimeSpec>();

    /// <summary>
    /// If true, a brick step that fails (throws) will be retried using the fallback chain.
    /// </summary>
    public bool SwapOnFailure { get; init; } = true;
}
