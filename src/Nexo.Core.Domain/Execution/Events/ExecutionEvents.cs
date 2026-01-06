using Nexo.Core.Domain.Bricks;

namespace Nexo.Core.Domain.Execution.Events;

/// <summary>
/// Base class for all execution events.
/// </summary>
public abstract class ExecutionEvent
{
    public string Type { get; init; } = default!;
    public DateTime Timestamp { get; init; }
    
    protected ExecutionEvent(string type, DateTime timestamp)
    {
        Type = type;
        Timestamp = timestamp;
    }
}

// Behavior-level events
public class BehaviorStartedEvent : ExecutionEvent
{
    public string BehaviorId { get; init; } = default!;
    public string BehaviorName { get; init; } = default!;
    
    public BehaviorStartedEvent(string behaviorId, string behaviorName, DateTime timestamp)
        : base("behavior_started", timestamp)
    {
        BehaviorId = behaviorId;
        BehaviorName = behaviorName;
    }
}

public class BehaviorCompletedEvent : ExecutionEvent
{
    public string BehaviorId { get; init; } = default!;
    public bool Success { get; init; }
    public IReadOnlyDictionary<string, object> Outputs { get; init; } = new Dictionary<string, object>();
    
    public BehaviorCompletedEvent(string behaviorId, bool success, IReadOnlyDictionary<string, object> outputs)
        : base("behavior_completed", DateTime.UtcNow)
    {
        BehaviorId = behaviorId;
        Success = success;
        Outputs = outputs;
    }
}

public class BehaviorCancelledEvent : ExecutionEvent
{
    public string BehaviorId { get; init; } = default!;
    
    public BehaviorCancelledEvent(string behaviorId)
        : base("behavior_cancelled", DateTime.UtcNow)
    {
        BehaviorId = behaviorId;
    }
}

// Step-level events
public class StepStartedEvent : ExecutionEvent
{
    public string StepId { get; init; } = default!;
    public string BrickId { get; init; } = default!;
    public string BrickName { get; init; } = default!;
    public ImplementationType Implementation { get; init; }
    public bool UsedFallback { get; init; }
    public int StepIndex { get; init; }
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

public class StepCompletedEvent : ExecutionEvent
{
    public string StepId { get; init; } = default!;
    public string BrickId { get; init; } = default!;
    public ImplementationType Implementation { get; init; }
    public long LatencyMs { get; init; }
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

public class StepSkippedEvent : ExecutionEvent
{
    public string StepId { get; init; } = default!;
    public string Reason { get; init; } = default!;
    
    public StepSkippedEvent(string stepId, string reason)
        : base("step_skipped", DateTime.UtcNow)
    {
        StepId = stepId;
        Reason = reason;
    }
}

public class StepErrorEvent : ExecutionEvent
{
    public string StepId { get; init; } = default!;
    public string Error { get; init; } = default!;
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
public class CacheHitEvent : ExecutionEvent
{
    public string StepId { get; init; } = default!;
    public string CacheKey { get; init; } = default!;
    
    public CacheHitEvent(string stepId, string cacheKey)
        : base("cache_hit", DateTime.UtcNow)
    {
        StepId = stepId;
        CacheKey = cacheKey;
    }
}

// Provider events
public class ProviderSwitchedEvent : ExecutionEvent
{
    public string FromProvider { get; init; } = default!;
    public string ToProvider { get; init; } = default!;
    
    public ProviderSwitchedEvent(string fromProvider, string toProvider)
        : base("provider_switched", DateTime.UtcNow)
    {
        FromProvider = fromProvider;
        ToProvider = toProvider;
    }
}

public class OfflineModeActivatedEvent : ExecutionEvent
{
    public OfflineModeActivatedEvent()
        : base("offline_mode_activated", DateTime.UtcNow)
    {
    }
}

