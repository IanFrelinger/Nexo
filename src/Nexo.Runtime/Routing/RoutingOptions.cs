using Nexo.Abstractions.Routing;

namespace Nexo.Runtime.Routing;

/// <summary>
/// Configuration model for endpoint routing.
/// </summary>
public sealed class RoutingOptions
{
    public IList<EndpointDescriptorConfig> Endpoints { get; init; } = [];

    public int HealthCheckIntervalSeconds { get; init; } = 30;
}

/// <summary>
/// Configuration model for a single endpoint descriptor.
/// </summary>
public sealed class EndpointDescriptorConfig
{
    public string Endpoint { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public string[] SupportedCapabilities { get; init; } = [];

    public string[] AcceptedBarrierLevels { get; init; } = [];

    public string? Region { get; init; }

    public int Priority { get; init; } = 100;

    public EndpointDescriptor ToDescriptor()
    {
        return new EndpointDescriptor(
            Endpoint: Endpoint,
            Name: Name,
            SupportedCapabilities: SupportedCapabilities,
            AcceptedBarrierLevels: AcceptedBarrierLevels,
            Region: Region,
            Priority: Priority,
            IsHealthy: true);
    }
}
