using Ashlar.Core.Domain;
using Ashlar.Core.Domain.Execution;

namespace Ashlar.Core.Application.Execution.Routing;

/// <summary>
/// Describes what a job needs from its execution target.
/// <para>
/// <see cref="ICapabilityRouter.ResolveExecutionTarget"/> compares these
/// requirements against the local node's capabilities (via NCR) and, if the
/// local node is insufficient, against available peers and RunPod GPU tiers.
/// </para>
/// <para>
/// <see cref="RemoteExecutionPreference"/> interacts with the trust tier
/// system: <c>PeerNetworkOnly</c> will only route to peers whose trust
/// level satisfies the policy in <c>RunPodBrickConfig.PeerTrustPolicy</c>.
/// </para>
/// </summary>
public sealed record JobRequirements
{
    /// <summary>Minimum VRAM required; used to filter GPU tiers.</summary>
    public long MinimumVramBytes { get; init; }

    /// <summary>Compute class floor for this job.</summary>
    public GpuComputeClass ComputeClass { get; init; } = GpuComputeClass.Low;

    /// <summary>Estimated wall-clock duration; influences queue-depth routing decisions.</summary>
    public TimeSpan EstimatedDuration { get; init; } = TimeSpan.FromMinutes(1);

    /// <summary>Target model identifier (must be loadable on the chosen target).</summary>
    public string ModelId { get; init; } = string.Empty;

    /// <summary>Hints that this job is low-priority and can be deferred to
    /// cheaper / slower execution targets (e.g. spot instances).</summary>
    public bool IsOvernightOrBackground { get; init; }

    /// <summary>Overrides the system-default routing cascade for this job.
    /// See <see cref="RemoteExecutionPreference"/> for options.</summary>
    public RemoteExecutionPreference RemoteExecutionPreference { get; init; } = RemoteExecutionPreference.UseSystemDefault;
}
