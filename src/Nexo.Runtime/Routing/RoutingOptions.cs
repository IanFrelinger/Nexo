using Nexo.Abstractions.Routing;

namespace Nexo.Runtime.Routing;

/// <summary>
/// Configuration model for endpoint routing.
/// </summary>
public sealed class RoutingOptions
{
    /// <summary>Configured endpoint descriptors loaded at startup.</summary>
    public IList<EndpointDescriptorConfig> Endpoints { get; init; } = [];

    /// <summary>Interval between endpoint health probes, in seconds.</summary>
    public int HealthCheckIntervalSeconds { get; init; } = 30;

    /// <summary>
    /// Number of consecutive failed probes required before logging a degraded warning.
    /// Helps suppress noisy warnings for transient one-off probe failures.
    /// </summary>
    public int DegradedLogFailureThreshold { get; init; } = 1;
}
