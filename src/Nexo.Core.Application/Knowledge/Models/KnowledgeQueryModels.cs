namespace Nexo.Core.Application.Knowledge.Models;

public enum KnowledgeSource
{
    Adaptation = 0,
    Pattern = 1,
    UserKnowledge = 2
}

public sealed record KnowledgeQueryRequest
{
    public DateTimeOffset? Since { get; init; }
    public DateTimeOffset? Until { get; init; }
    public string? DataType { get; init; }
    public string? EventType { get; init; }
    public int MaxCount { get; init; } = 100;
    public int Offset { get; init; } = 0;
    public IReadOnlyList<KnowledgeSource>? Sources { get; init; }
}

public sealed record KnowledgeQueryResult
{
    public IReadOnlyList<KnowledgeQueryEntry> Entries { get; init; } = Array.Empty<KnowledgeQueryEntry>();
    public int Total { get; init; }
    public bool HasMore { get; init; }
}

public sealed record KnowledgeQueryEntry
{
    public required string Id { get; init; }
    public required KnowledgeSource Source { get; init; }
    public required DateTimeOffset Timestamp { get; init; }
    public string? DataType { get; init; }
    public string? EventType { get; init; }
    public string? Summary { get; init; }
    public IReadOnlyDictionary<string, string> Provenance { get; init; } = new Dictionary<string, string>();
}
