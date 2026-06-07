using Nexo.Commercial.Fleet.Contracts.Models;

namespace Nexo.Commercial.Fleet.Contracts.Ports;

/// <summary>
/// Picks an eligible worker for a pending mesh task (Phase 1 — no migration).
/// </summary>
public interface IMeshTaskPlacementService
{
    /// <summary>
    /// Assigns a pending task to the best eligible node, or returns false with a reason.
    /// When <paramref name="scheduleIdempotencyKey"/> matches a previous successful schedule for this task, returns the same assignment (no double placement).
    /// </summary>
    Task<(bool Ok, MeshTaskState? Task, string? Error)> TryScheduleAsync(
        string taskId,
        string? scheduleIdempotencyKey = null,
        string? correlationId = null,
        int? leaseSecondsOverride = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Resets assignment and re-attempts scheduling (retry on another node).
    /// </summary>
    Task<(bool Ok, MeshTaskState? Task, string? Error)> TryRetryAsync(
        string taskId,
        string? scheduleIdempotencyKey = null,
        string? correlationId = null,
        int? leaseSecondsOverride = null,
        CancellationToken cancellationToken = default);
}
