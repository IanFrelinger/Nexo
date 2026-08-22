using System.Text.Json;

namespace Ashlar.Commercial.Fleet.Contracts.Networking.Models;
/// <summary>
/// Entry for an agent known to the network (this node or a peer).
/// </summary>
public sealed record NetworkAgentEntry
{
    /// <summary>Unique agent identifier.</summary>
    public required string AgentId { get; init; }

    /// <summary>Node that hosts this agent.</summary>
    public required string NodeId { get; init; }

    /// <summary>Domain or role (e.g. "Combat", "Economy").</summary>
    public string? Domain { get; init; }

    /// <summary>Human-readable description.</summary>
    public string? Description { get; init; }

    /// <summary>Capability tags (e.g. "schema-negotiation", "resource").</summary>
    public IReadOnlyList<string> Capabilities { get; init; } = [];

    /// <summary>Last time this entry was seen or refreshed.</summary>
    public DateTimeOffset LastSeen { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>Optional metadata (JSON).</summary>
    public JsonElement? Metadata { get; init; }
}
