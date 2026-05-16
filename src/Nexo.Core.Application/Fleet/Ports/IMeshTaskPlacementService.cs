using Nexo.Core.Application.Fleet.Models;

namespace Nexo.Core.Application.Fleet.Ports;

/// <summary>
/// Picks an eligible worker for a pending mesh task (Phase 1 — no migration).
/// </summary>
public interface IMeshTaskPlacementService
{
    /// <summary>
    /// Assigns a pending task to the best eligible node, or returns false with a reason.
    /// </summary>
    Task<(bool Ok, MeshTaskState? Task, string? Error)> TryScheduleAsync(string taskId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resets assignment and re-attempts scheduling (retry on another node).
    /// </summary>
    Task<(bool Ok, MeshTaskState? Task, string? Error)> TryRetryAsync(string taskId, CancellationToken cancellationToken = default);
}
