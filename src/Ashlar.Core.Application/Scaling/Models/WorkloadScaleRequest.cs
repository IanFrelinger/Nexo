namespace Ashlar.Core.Application.Scaling.Models;

/// <summary>Request to change desired replicas for a workload.</summary>
public sealed record WorkloadScaleRequest(
    string WorkloadId,
    int DesiredReplicas,
    string? Reason = null);
