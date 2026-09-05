using Ashlar.Commercial.Fleet.Contracts.Models;

namespace Ashlar.Commercial.Fleet.Contracts.Ports;

/// <summary>
/// In-process store for mesh tasks (Phase 1).
/// </summary>
public interface IMeshTaskRegistry
{
    Task<MeshTaskState> CreateAsync(MeshTaskCreateSpec spec, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns an existing task created with the same <see cref="MeshTaskCreateSpec.IdempotencyKey"/> (Phase 3), or null.
    /// </summary>
    Task<MeshTaskState?> TryGetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken = default);

    Task<MeshTaskState?> GetAsync(string taskId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MeshTaskState>> ListAsync(CancellationToken cancellationToken = default);

    Task<bool> UpdateAsync(MeshTaskState task, CancellationToken cancellationToken = default);

    /// <summary>
    /// Writes <paramref name="task"/> only when the stored row still has
    /// <paramref name="expectedStatus"/>. Returns false when the task is missing
    /// or another writer already changed its status.
    /// </summary>
    Task<bool> UpdateIfStatusAsync(
        MeshTaskState task,
        MeshTaskStatus expectedStatus,
        CancellationToken cancellationToken = default);
}
