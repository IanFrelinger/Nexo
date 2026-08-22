namespace Ashlar.Core.Application.Knowledge.Models;

/// <summary>A single knowledge query result entry with provenance metadata.</summary>
public sealed record KnowledgeQueryEntry
{
    /// <summary>Stable entry identifier within its source.</summary>
    public required string Id { get; init; }

    /// <summary>Backing knowledge source.</summary>
    public required KnowledgeSource Source { get; init; }

    /// <summary>UTC timestamp of the original event or record.</summary>
    public required DateTimeOffset Timestamp { get; init; }

    /// <summary>Optional data type label.</summary>
    public string? DataType { get; init; }

    /// <summary>Optional event type label.</summary>
    public string? EventType { get; init; }

    /// <summary>Human-readable summary for operator UIs.</summary>
    public string? Summary { get; init; }

    /// <summary>Provenance key-value metadata (node id, brick id, etc.).</summary>
    public IReadOnlyDictionary<string, string> Provenance { get; init; } = new Dictionary<string, string>();
}
