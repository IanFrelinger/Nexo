namespace Ashlar.Core.Application.Scaling.Models;

/// <summary>Policy recommendation for a workload's desired replica count.</summary>
public sealed record WorkloadScaleDecision(
    int DesiredReplicas,
    bool ShouldScale,
    string Reason);
