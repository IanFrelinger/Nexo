using Ashlar.Core.Domain.Bricks;

namespace Ashlar.Core.Domain.Execution.Events;

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
