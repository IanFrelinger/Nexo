using Ashlar.Core.Domain.Bricks;

namespace Ashlar.Core.Domain.Execution.Events;

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
