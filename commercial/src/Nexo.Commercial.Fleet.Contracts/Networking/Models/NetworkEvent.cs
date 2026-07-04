using System.Text.Json;

namespace Nexo.Commercial.Fleet.Contracts.Networking.Models;
/// <summary>
/// Network-wide event for cross-node signal propagation.
/// </summary>
public sealed record NetworkEvent
{
    /// <summary>Stable event identifier for deduplication.</summary>
    public required string EventId { get; init; }
    /// <summary>Originating mesh node id.</summary>
    public required string SourceNodeId { get; init; }
    /// <summary>Optional target node id for directed events.</summary>
    public string? TargetNodeId { get; init; }
    /// <summary>Event type label (see <see cref="NetworkEventTypes"/>).</summary>
    public required string EventType { get; init; }
    /// <summary>UTC timestamp when the event was emitted.</summary>
    public required DateTimeOffset Timestamp { get; init; }
    /// <summary>Number of relay hops already traversed.</summary>
    public int Hops { get; init; }
    /// <summary>Maximum relay hops before the event is dropped.</summary>
    public int MaxHops { get; init; } = 3;
    /// <summary>Optional JSON payload carried with the event.</summary>
    public JsonElement? Payload { get; init; }
}
