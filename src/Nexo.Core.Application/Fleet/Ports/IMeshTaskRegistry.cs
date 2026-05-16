using Nexo.Core.Application.Fleet.Models;

namespace Nexo.Core.Application.Fleet.Ports;

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
}
