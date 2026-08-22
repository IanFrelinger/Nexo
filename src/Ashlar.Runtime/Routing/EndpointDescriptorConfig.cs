using Ashlar.Abstractions.Routing;

namespace Ashlar.Runtime.Routing;

/// <summary>
/// Configuration model for a single endpoint descriptor.
/// </summary>
public sealed class EndpointDescriptorConfig
{
    /// <summary>Remote endpoint URI or address.</summary>
    public string Endpoint { get; init; } = string.Empty;

    /// <summary>Human-readable endpoint name.</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>Capabilities supported by this endpoint.</summary>
    public string[] SupportedCapabilities { get; init; } = [];

    /// <summary>Barrier levels accepted by this endpoint.</summary>
    public string[] AcceptedBarrierLevels { get; init; } = [];

    /// <summary>Optional region label for routing affinity.</summary>
    public string? Region { get; init; }

    /// <summary>Routing priority; lower values are preferred.</summary>
    public int Priority { get; init; } = 100;

    /// <summary>Converts this configuration entry to a runtime <see cref="EndpointDescriptor"/>.</summary>
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
