namespace Nexo.Core.Application.Scaling.Models;

/// <summary>Observed replica state for a workload.</summary>
public sealed record WorkloadReplicaSnapshot(
    string WorkloadId,
    int DesiredReplicas,
    int CurrentReplicas,
    int ReadyReplicas,
    DateTimeOffset ObservedAtUtc,
    string? ProviderDetail = null);
