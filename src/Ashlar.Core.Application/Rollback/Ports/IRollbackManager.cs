using Ashlar.Core.Application.Rollback.Models;

namespace Ashlar.Core.Application.Rollback.Ports;

/// <summary>
/// Orchestrates rollback using snapshot store and dependency graph.
/// Called before inheritance to create snapshots; invoked on failure to restore.
/// </summary>
public interface IRollbackManager
{
    /// <summary>
    /// Registers affected paths for an adaptation. Call before <see cref="BeforeInheritAsync"/>.
    /// </summary>
    void PrepareForInherit(string adaptationId, IReadOnlyList<string> affectedPaths);

    /// <summary>
    /// Takes a snapshot before inheriting a change. Call <see cref="PrepareForInherit"/> first.
    /// </summary>
    Task<string> BeforeInheritAsync(string adaptationId, CancellationToken ct = default);

    /// <summary>
    /// Rolls back a specific adaptation and all dependents.
    /// </summary>
    Task RollbackAsync(string adaptationId, CancellationToken ct = default);

    /// <summary>
    /// Rolls back to a specific snapshot.
    /// </summary>
    Task RollbackToSnapshotAsync(string snapshotId, CancellationToken ct = default);

    /// <summary>
    /// Gets the impact of rolling back (what else will be rolled back).
    /// </summary>
    Task<RollbackImpact> PreviewRollbackAsync(string adaptationId, CancellationToken ct = default);
}
