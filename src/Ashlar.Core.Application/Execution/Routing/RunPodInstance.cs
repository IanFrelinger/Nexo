using Ashlar.Core.Domain;
using Ashlar.Core.Domain.Execution;

namespace Ashlar.Core.Application.Execution.Routing;

/// <summary>
/// Represents a live RunPod GPU instance that Ashlar has provisioned.
/// Instances are ephemeral — spun up for a job batch and terminated
/// after idle timeout or explicit <see cref="IRunPodClient.TerminateInstance"/>.
/// </summary>
public sealed record RunPodInstance
{
    /// <summary>Opaque RunPod instance identifier.</summary>
    public string InstanceId { get; init; } = string.Empty;

    /// <summary>Model loaded on this instance.</summary>
    public string ModelId { get; init; } = string.Empty;

    /// <summary>GPU tier provisioned for this instance.</summary>
    public string GpuType { get; init; } = string.Empty;

    /// <summary>UTC timestamp when the instance was spun up.</summary>
    public DateTimeOffset StartedAt { get; init; } = DateTimeOffset.UtcNow;
}
