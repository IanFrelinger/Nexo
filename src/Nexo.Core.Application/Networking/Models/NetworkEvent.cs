using System.Text.Json;

namespace Nexo.Core.Application.Networking.Models;

/// <summary>
/// Network-wide event for cross-node signal propagation.
/// </summary>
public sealed record NetworkEvent
{
    public required string EventId { get; init; }
    public required string SourceNodeId { get; init; }
    public string? TargetNodeId { get; init; }
    public required string EventType { get; init; }
    public required DateTimeOffset Timestamp { get; init; }
    public int Hops { get; init; }
    public int MaxHops { get; init; } = 3;
    public JsonElement? Payload { get; init; }
}
