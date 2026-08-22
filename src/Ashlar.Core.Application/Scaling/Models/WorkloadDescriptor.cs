namespace Ashlar.Core.Application.Scaling.Models;

/// <summary>Logical workload that can be scaled (independent of provider resource kind).</summary>
public sealed record WorkloadDescriptor(
    string WorkloadId,
    string DisplayName,
    int MinReplicas,
    int MaxReplicas,
    string? ProviderResourceHint = null);
