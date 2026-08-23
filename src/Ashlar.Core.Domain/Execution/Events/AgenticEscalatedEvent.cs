using Ashlar.Core.Domain.Bricks;

namespace Ashlar.Core.Domain.Execution.Events;

/// <summary>
/// Emitted when an agentic step is escalated by NCR instead of running locally.
/// </summary>
public class AgenticEscalatedEvent : ExecutionEvent
{
    /// <summary>ID of the step that was escalated.</summary>
    public string StepId { get; init; } = default!;
    /// <summary>ID of the brick whose execution was escalated.</summary>
    public string BrickId { get; init; } = default!;
    /// <summary>NCR escalation reason.</summary>
    public string Reason { get; init; } = default!;
    /// <summary>Optional model id considered by NCR.</summary>
    public string? ModelId { get; init; }
    /// <summary>Optional provider considered by NCR.</summary>
    public string? Provider { get; init; }

    public AgenticEscalatedEvent(
        string stepId,
        string brickId,
        string reason,
        string? modelId = null,
        string? provider = null)
        : base("agentic_escalated", DateTime.UtcNow)
    {
        StepId = stepId;
        BrickId = brickId;
        Reason = reason;
        ModelId = modelId;
        Provider = provider;
    }
}
