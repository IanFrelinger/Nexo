namespace Ashlar.Core.Application.Scaling.Models;

/// <summary>Outcome of a scale operation.</summary>
public sealed record WorkloadScaleResult(
    bool Success,
    string WorkloadId,
    int PreviousDesiredReplicas,
    int DesiredReplicas,
    string? Message = null);
