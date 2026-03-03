using Nexo.Core.Application.Rollback.Models;

namespace Nexo.Core.Application.Rollback.Ports;

/// <summary>
/// Stores state snapshots before inheritance operations.
/// Enables rollback to a known-good state.
/// </summary>
public interface ISnapshotStore
{
    Task<string> TakeSnapshotAsync(string label, IReadOnlyList<string> componentPaths, CancellationToken ct = default);
    Task<IEnumerable<SnapshotEntry>> ListSnapshotsAsync(CancellationToken ct = default);
    Task RestoreSnapshotAsync(string snapshotId, CancellationToken ct = default);
    Task DeleteSnapshotAsync(string snapshotId, CancellationToken ct = default);
}
