namespace Ashlar.Core.Application.Observation.Models;

/// <summary>
/// Query parameters for the pattern store.
/// </summary>
public sealed record PatternStoreQueryParams
{
    /// <summary>Optional project path filter.</summary>
    public string? ProjectPath { get; init; }

    /// <summary>Include patterns observed after this time.</summary>
    public DateTimeOffset? Since { get; init; }

    /// <summary>Include patterns observed before this time.</summary>
    public DateTimeOffset? Until { get; init; }

    /// <summary>Maximum number of patterns to return.</summary>
    public int MaxCount { get; init; } = 100;

    /// <summary>Optional event type filter (e.g. repeated-edits).</summary>
    public string? EventType { get; init; }
}
