namespace Ashlar.Core.Application.Knowledge.Models;

/// <summary>Paged knowledge query result.</summary>
public sealed record KnowledgeQueryResult
{
    /// <summary>Matching entries for the current page.</summary>
    public IReadOnlyList<KnowledgeQueryEntry> Entries { get; init; } = Array.Empty<KnowledgeQueryEntry>();

    /// <summary>Total matching entries across all pages.</summary>
    public int Total { get; init; }

    /// <summary>True when more pages are available.</summary>
    public bool HasMore { get; init; }
}
