namespace Ashlar.Abstractions.Routing;

/// <summary>
/// Declarative endpoint descriptor used by routing policy.
/// </summary>
public sealed record EndpointDescriptor(
    string Endpoint,
    string Name,
    IReadOnlyList<string> SupportedCapabilities,
    IReadOnlyList<string> AcceptedBarrierLevels,
    string? Region,
    int Priority,
    bool IsHealthy);
