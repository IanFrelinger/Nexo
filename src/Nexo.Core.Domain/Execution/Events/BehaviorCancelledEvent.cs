using Nexo.Core.Domain.Bricks;

namespace Nexo.Core.Domain.Execution.Events;

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
