using Ashlar.Core.Domain.Bricks;

namespace Ashlar.Core.Domain.Execution.Events;

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
