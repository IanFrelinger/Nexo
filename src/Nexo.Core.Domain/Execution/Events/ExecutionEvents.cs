using Nexo.Core.Domain.Bricks;

namespace Nexo.Core.Domain.Execution.Events;

/// <summary>
/// Base class for all execution events.
/// </summary>
public abstract class ExecutionEvent
{
    /// <summary>Event type identifier (e.g. behavior_started, step_completed).</summary>
    public string Type { get; init; } = default!;
    /// <summary>When the event occurred (UTC).</summary>
    public DateTime Timestamp { get; init; }
    
    protected ExecutionEvent(string type, DateTime timestamp)
    {
        Type = type;
        Timestamp = timestamp;
    }
}

// Behavior-level events
/// <summary>
/// Emitted when a behavior execution starts.
/// </summary>
public class BehaviorStartedEvent : ExecutionEvent
{
    /// <summary>ID of the behavior being executed.</summary>
    public string BehaviorId { get; init; } = default!;
    /// <summary>Display name of the behavior.</summary>
    public string BehaviorName { get; init; } = default!;
    
    public BehaviorStartedEvent(string behaviorId, string behaviorName, DateTime timestamp)
        : base("behavior_started", timestamp)
    {
        BehaviorId = behaviorId;
        BehaviorName = behaviorName;
    }
}

/// <summary>
/// Emitted when a behavior execution completes (success or failure).
/// </summary>
public class BehaviorCompletedEvent : ExecutionEvent
{
    /// <summary>ID of the completed behavior.</summary>
    public string BehaviorId { get; init; } = default!;
    /// <summary>Whether the behavior succeeded.</summary>
    public bool Success { get; init; }
    /// <summary>Output variables from the execution.</summary>
    public IReadOnlyDictionary<string, object> Outputs { get; init; } = new Dictionary<string, object>();
    
    public BehaviorCompletedEvent(string behaviorId, bool success, IReadOnlyDictionary<string, object> outputs)
        : base("behavior_completed", DateTime.UtcNow)
    {
        BehaviorId = behaviorId;
        Success = success;
        Outputs = outputs;
    }
}

/// <summary>
/// Emitted when a behavior execution is cancelled.
/// </summary>
public class BehaviorCancelledEvent : ExecutionEvent
{
    /// <summary>ID of the cancelled behavior.</summary>
    public string BehaviorId { get; init; } = default!;
    
    public BehaviorCancelledEvent(string behaviorId)
        : base("behavior_cancelled", DateTime.UtcNow)
    {
        BehaviorId = behaviorId;
    }
}

// Step-level events
/// <summary>
/// Emitted when a behavior step starts executing.
/// </summary>
public class StepStartedEvent : ExecutionEvent
{
    /// <summary>ID of the step.</summary>
    public string StepId { get; init; } = default!;
    /// <summary>ID of the brick being executed.</summary>
    public string BrickId { get; init; } = default!;
    /// <summary>Display name of the brick.</summary>
    public string BrickName { get; init; } = default!;
    /// <summary>Implementation type (Deterministic or Agentic).</summary>
    public ImplementationType Implementation { get; init; }
    /// <summary>True if a fallback implementation is being used.</summary>
    public bool UsedFallback { get; init; }
    /// <summary>0-based index of this step.</summary>
    public int StepIndex { get; init; }
    /// <summary>Total number of steps in the behavior.</summary>
    public int TotalSteps { get; init; }
    
    public StepStartedEvent(
        string stepId,
        string brickId,
        string brickName,
        ImplementationType implementation,
        bool usedFallback,
        int stepIndex,
        int totalSteps)
        : base("step_started", DateTime.UtcNow)
    {
        StepId = stepId;
        BrickId = brickId;
        BrickName = brickName;
        Implementation = implementation;
        UsedFallback = usedFallback;
        StepIndex = stepIndex;
        TotalSteps = totalSteps;
    }
}

/// <summary>
/// Emitted when a behavior step completes successfully.
/// </summary>
public class StepCompletedEvent : ExecutionEvent
{
    /// <summary>ID of the completed step.</summary>
    public string StepId { get; init; } = default!;
    /// <summary>ID of the brick.</summary>
    public string BrickId { get; init; } = default!;
    /// <summary>Implementation type used.</summary>
    public ImplementationType Implementation { get; init; }
    /// <summary>Execution latency in milliseconds.</summary>
    public long LatencyMs { get; init; }
    /// <summary>Optional summary from the brick output.</summary>
    public string? Summary { get; init; }
    
    public StepCompletedEvent(
        string stepId,
        string brickId,
        ImplementationType implementation,
        long latencyMs,
        string? summary = null)
        : base("step_completed", DateTime.UtcNow)
    {
        StepId = stepId;
        BrickId = brickId;
        Implementation = implementation;
        LatencyMs = latencyMs;
        Summary = summary;
    }
}

/// <summary>
/// Emitted when a behavior step is skipped (e.g. condition not met).
/// </summary>
public class StepSkippedEvent : ExecutionEvent
{
    /// <summary>ID of the skipped step.</summary>
    public string StepId { get; init; } = default!;
    /// <summary>Reason the step was skipped.</summary>
    public string Reason { get; init; } = default!;
    
    public StepSkippedEvent(string stepId, string reason)
        : base("step_skipped", DateTime.UtcNow)
    {
        StepId = stepId;
        Reason = reason;
    }
}

/// <summary>
/// Emitted when a behavior step fails with an error.
/// </summary>
public class StepErrorEvent : ExecutionEvent
{
    /// <summary>ID of the step that failed.</summary>
    public string StepId { get; init; } = default!;
    /// <summary>Error message.</summary>
    public string Error { get; init; } = default!;
    /// <summary>Optional latency before failure (ms).</summary>
    public long? LatencyMs { get; init; }
    
    public StepErrorEvent(string stepId, string error, long? latencyMs = null)
        : base("step_error", DateTime.UtcNow)
    {
        StepId = stepId;
        Error = error;
        LatencyMs = latencyMs;
    }
}

// Cache events
/// <summary>
/// Emitted when a step result is served from cache.
/// </summary>
public class CacheHitEvent : ExecutionEvent
{
    /// <summary>ID of the step.</summary>
    public string StepId { get; init; } = default!;
    /// <summary>Cache key used.</summary>
    public string CacheKey { get; init; } = default!;
    
    public CacheHitEvent(string stepId, string cacheKey)
        : base("cache_hit", DateTime.UtcNow)
    {
        StepId = stepId;
        CacheKey = cacheKey;
    }
}

// Provider events
/// <summary>
/// Emitted when the LLM provider is switched at runtime.
/// </summary>
public class ProviderSwitchedEvent : ExecutionEvent
{
    /// <summary>Previous provider name.</summary>
    public string FromProvider { get; init; } = default!;
    /// <summary>New provider name.</summary>
    public string ToProvider { get; init; } = default!;
    
    public ProviderSwitchedEvent(string fromProvider, string toProvider)
        : base("provider_switched", DateTime.UtcNow)
    {
        FromProvider = fromProvider;
        ToProvider = toProvider;
    }
}

/// <summary>
/// Emitted when offline mode is activated (air-gapped execution).
/// </summary>
public class OfflineModeActivatedEvent : ExecutionEvent
{
    public OfflineModeActivatedEvent()
        : base("offline_mode_activated", DateTime.UtcNow)
    {
    }
}

