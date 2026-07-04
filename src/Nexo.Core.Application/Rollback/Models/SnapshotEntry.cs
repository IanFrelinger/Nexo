namespace Nexo.Core.Application.Rollback.Models;

/// <summary>Metadata for a stored snapshot.</summary>
/// <param name="SnapshotId">Stable snapshot identifier.</param>
/// <param name="Label">Human-readable snapshot label.</param>
/// <param name="TakenAt">UTC timestamp when the snapshot was captured.</param>
/// <param name="ComponentsIncluded">Component identifiers included in the snapshot.</param>
public record SnapshotEntry(
    string SnapshotId,
    string Label,
    DateTimeOffset TakenAt,
    IReadOnlyList<string> ComponentsIncluded);
