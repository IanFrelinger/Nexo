using Ashlar.Core.Domain.Bricks;
using Ashlar.Core.Domain.Workflows;

namespace Ashlar.Core.Domain.Execution;

/// <summary>
/// Options for behavior execution.
/// </summary>
public class ExecutionOptions
{
    /// <summary>When true, outbound network and provider calls are blocked.</summary>
    public bool IsAirGapped { get; init; }

    /// <summary>When true, emit extended audit telemetry and prefer deterministic paths.</summary>
    public bool AuditMode { get; init; }

    /// <summary>Preferred LLM provider name for agentic steps.</summary>
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
    /// <summary>When true, retry failed brick steps using configured fallback chains.</summary>
    public bool SwapOnFailure { get; init; } = true;
}
