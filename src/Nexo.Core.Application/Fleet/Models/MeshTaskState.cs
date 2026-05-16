namespace Nexo.Core.Application.Fleet.Models;

/// <summary>
/// A schedulable task tracked by the Phase 1 director (in-memory).
/// </summary>
public sealed record MeshTaskState(
    string TaskId,
    string? Name,
    int Steps,
    IReadOnlyList<string> RequiredBrickIds,
    IReadOnlyDictionary<string, string> Affinity,
    int Priority,
    DateTimeOffset? DeadlineUtc,
    MeshTaskStatus Status,
    string? AssignedPeerId,
    string? AssignedApiBaseUrl,
    string? PlacementReason,
    int AttemptCount,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? LastScheduledAtUtc);
