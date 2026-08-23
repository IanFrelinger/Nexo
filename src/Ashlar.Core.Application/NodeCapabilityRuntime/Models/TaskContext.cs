namespace Ashlar.Core.Application.NodeCapabilityRuntime.Models;

/// <summary>
/// Task requirements used to select an appropriate model and inference target.
/// </summary>
public sealed record TaskContext
{
    /// <summary>Capability the selected model must support.</summary>
    public TaskCapability RequiredCapability { get; init; }

    /// <summary>Minimum acceptable output quality for this task.</summary>
    public QualityRequirement Quality { get; init; } = QualityRequirement.Medium;

    /// <summary>Maximum acceptable latency class for this task.</summary>
    public LatencyRequirement Latency { get; init; } = LatencyRequirement.Background;

    /// <summary>Privacy constraints governing where inference may run.</summary>
    public PrivacyBoundary Privacy { get; init; } = PrivacyBoundary.Standard;

    /// <summary>When true, the task is directly visible to the end user.</summary>
    public bool IsUserFacing { get; init; }

    /// <summary>When true, the task was delegated from another agent or node.</summary>
    public bool IsDelegationTask { get; init; }

    /// <summary>Optional preferred provider name for model selection.</summary>
    public string? RequestedProvider { get; init; }
}
