namespace Ashlar.Core.Application.NodeCapabilityRuntime.Models;

/// <summary>
/// Outcome of model selection for a given task context.
/// </summary>
public sealed record ModelResolution
{
    /// <summary>Selected model descriptor.</summary>
    public ModelDescriptor Model { get; init; } = new();

    /// <summary>Whether inference should run locally or escalate to a remote node.</summary>
    public InferenceTarget Target { get; init; }

    /// <summary>Why this model and target were chosen.</summary>
    public ResolutionReason Reason { get; init; }
}
