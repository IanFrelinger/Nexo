namespace Ashlar.Core.Application.Scaling.Models;

/// <summary>Inputs for <see cref="Ports.IWorkloadScalePolicy"/>.</summary>
public sealed record WorkloadScaleContext(
    WorkloadDescriptor Workload,
    WorkloadReplicaSnapshot Replicas,
    WorkloadDemandSnapshot Demand);
