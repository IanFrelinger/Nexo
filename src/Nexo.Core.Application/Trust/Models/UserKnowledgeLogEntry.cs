namespace Nexo.Core.Application.Trust.Models;

/// <summary>
/// A user-facing knowledge log entry: what Nexo has learned about the user
/// (preferences, patterns, workflow habits). Includes provenance for audit.
/// </summary>
public sealed record UserKnowledgeLogEntry
{
    /// <summary>Unique entry id.</summary>
    public required string Id { get; init; }

    /// <summary>Data type (e.g. inferred-preferences, behavioral-patterns).</summary>
    public required string DataType { get; init; }

    /// <summary>Content (text or JSON).</summary>
    public required string Content { get; init; }

    /// <summary>Source observation IDs for provenance; full chain traversable.</summary>
    public IReadOnlyList<string> SourceObservationIds { get; init; } = Array.Empty<string>();

    /// <summary>Version number; updates create new versions.</summary>
    public int Version { get; init; } = 1;

    /// <summary>When the entry was created.</summary>
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>When the entry was last updated.</summary>
    public DateTimeOffset UpdatedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>When the entry was soft-deleted (null if active).</summary>
    public DateTimeOffset? DeletedAt { get; init; }
}
