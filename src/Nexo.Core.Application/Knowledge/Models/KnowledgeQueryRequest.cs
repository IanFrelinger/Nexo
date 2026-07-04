namespace Nexo.Core.Application.Knowledge.Models;

/// <summary>Filter criteria for a cross-source knowledge query.</summary>
public sealed record KnowledgeQueryRequest
{
    /// <summary>Earliest timestamp to include (inclusive).</summary>
    public DateTimeOffset? Since { get; init; }

    /// <summary>Latest timestamp to include (exclusive).</summary>
    public DateTimeOffset? Until { get; init; }

    /// <summary>Optional data type filter.</summary>
    public string? DataType { get; init; }

    /// <summary>Optional event type filter.</summary>
    public string? EventType { get; init; }

    /// <summary>Maximum entries to return in this page.</summary>
    public int MaxCount { get; init; } = 100;

    /// <summary>Offset for pagination.</summary>
    public int Offset { get; init; } = 0;

    /// <summary>Sources to query; null queries all configured sources.</summary>
    public IReadOnlyList<KnowledgeSource>? Sources { get; init; }
}
