using Ashlar.Core.Domain.Bricks;

namespace Ashlar.Core.Domain.Execution.Events;

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
