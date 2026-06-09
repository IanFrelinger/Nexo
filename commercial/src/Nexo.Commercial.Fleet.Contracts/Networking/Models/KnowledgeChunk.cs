using System.Text.Json;

namespace Nexo.Core.Application.Networking.Models;

/// <summary>
/// A chunk of knowledge/memory for cross-node sync (e.g. indexed memory to propagate).
/// </summary>
public sealed record KnowledgeChunk
{
    /// <summary>Unique chunk id (e.g. document id or composite key).</summary>
    public required string Id { get; init; }

    /// <summary>Node that originated this chunk.</summary>
    public required string SourceNodeId { get; init; }

    /// <summary>Content type (e.g. "text/plain", "memory.indexed").</summary>
    public string ContentType { get; init; } = "application/json";

    /// <summary>Timestamp when the chunk was created or indexed.</summary>
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>Optional content hash for deduplication.</summary>
    public string? ContentHash { get; init; }

    /// <summary>Payload (e.g. text snippet or JSON).</summary>
    public JsonElement? Payload { get; init; }
}
