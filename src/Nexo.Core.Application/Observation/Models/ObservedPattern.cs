using System.Text.Json;

namespace Nexo.Core.Application.Observation.Models;

/// <summary>
/// A detected pattern in the observation stream (e.g. repeated-edits, edit-then-build).
/// </summary>
public sealed record ObservedPattern
{
    /// <summary>Unique identifier for this pattern.</summary>
    public required string PatternId { get; init; }

    /// <summary>Pattern type (e.g. repeated-edits, edit-then-build).</summary>
    public required string EventType { get; init; }

    /// <summary>How many times this pattern was observed.</summary>
    public int Frequency { get; init; }

    /// <summary>When the pattern was first observed.</summary>
    public required DateTimeOffset FirstSeen { get; init; }

    /// <summary>When the pattern was last observed.</summary>
    public required DateTimeOffset LastSeen { get; init; }

    /// <summary>Optional project path for filtering.</summary>
    public string? ProjectPath { get; init; }

    /// <summary>Event IDs that contributed to this pattern (provenance).</summary>
    public IReadOnlyList<string> RelatedEventIds { get; init; } = Array.Empty<string>();

    /// <summary>Pattern-specific metadata (paths, intervals, etc.).</summary>
    public JsonElement? Metadata { get; init; }
}
