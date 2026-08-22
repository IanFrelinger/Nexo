namespace Ashlar.Core.Application.Scaling.Models;

/// <summary>Demand metrics used as autoscale input.</summary>
public sealed record WorkloadDemandSnapshot(
    string WorkloadId,
    int QueueDepth,
    int PendingTasks,
    DateTimeOffset ObservedAtUtc);
