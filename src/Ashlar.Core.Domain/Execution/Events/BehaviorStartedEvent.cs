using Ashlar.Core.Domain.Bricks;

namespace Ashlar.Core.Domain.Execution.Events;

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
