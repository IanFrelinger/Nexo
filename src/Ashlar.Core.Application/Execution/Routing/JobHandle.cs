using Ashlar.Core.Domain;
using Ashlar.Core.Domain.Execution;

namespace Ashlar.Core.Application.Execution.Routing;

/// <summary>
/// Opaque handle returned by <see cref="IRunPodClient.DispatchJob"/> and used
/// for subsequent <see cref="IRunPodClient.PollJobStatus"/> /
/// <see cref="IRunPodClient.PullResults"/> calls.
/// </summary>
public sealed record JobHandle
{
    /// <summary>RunPod instance that is executing the job.</summary>
    public string InstanceId { get; init; } = string.Empty;

    /// <summary>RunPod-assigned job identifier.</summary>
    public string JobId { get; init; } = string.Empty;
}
