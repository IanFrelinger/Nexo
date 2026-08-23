namespace Ashlar.Abstractions.Routing;

/// <summary>
/// Immutable context used by endpoint routing policies.
/// </summary>
public sealed record EndpointRoutingContext(
    string AgentName,
    string CorrelationId,
    IReadOnlyList<string> RequiredCapabilities,
    string BarrierLevel,
    string? PreferredRegion,
    IReadOnlyDictionary<string, object?> AgentMetadata);
