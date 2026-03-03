namespace Nexo.Core.Application.Rollback.Models;

/// <summary>
/// Metadata for a stored snapshot.
/// </summary>
public record SnapshotEntry(
    string SnapshotId,
    string Label,
    DateTimeOffset TakenAt,
    IReadOnlyList<string> ComponentsIncluded);
