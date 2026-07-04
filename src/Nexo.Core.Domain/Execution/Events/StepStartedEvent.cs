using Nexo.Core.Domain.Bricks;

namespace Nexo.Core.Domain.Execution.Events;

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
