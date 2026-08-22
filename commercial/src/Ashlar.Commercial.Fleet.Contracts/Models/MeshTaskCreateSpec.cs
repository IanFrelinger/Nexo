namespace Ashlar.Commercial.Fleet.Contracts.Models;

/// <summary>
/// Input for creating a new mesh task (server assigns <see cref="MeshTaskState.TaskId"/>).
/// </summary>
public sealed record MeshTaskCreateSpec(
    string? Name,
    int Steps,
    IReadOnlyList<string> RequiredBrickIds,
    IReadOnlyDictionary<string, string>? Affinity,
    int Priority,
    DateTimeOffset? DeadlineUtc,
    string? CorrelationId = null,
    string? IdempotencyKey = null);
