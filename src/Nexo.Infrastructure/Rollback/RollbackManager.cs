using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Adaptation.Models;
using Nexo.Core.Application.Adaptation.Ports;
using Nexo.Core.Application.Rollback.Models;
using Nexo.Core.Application.Rollback.Ports;

namespace Nexo.Infrastructure.Rollback;

/// <summary>
/// Orchestrates rollback using snapshot store and dependency graph.
/// </summary>
public sealed class RollbackManager : IRollbackManager
{
    private readonly ISnapshotStore _snapshotStore;
    private readonly IDependencyGraph _dependencyGraph;
    private readonly IAdaptationAuditLog _auditLog;
    private readonly ILogger<RollbackManager>? _logger;
    private readonly Dictionary<string, (string? SnapshotId, IReadOnlyList<string> Paths)> _adaptationSnapshots = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _lock = new();

    /// <summary>Initializes a new rollback manager.</summary>
    public RollbackManager(
        ISnapshotStore snapshotStore,
        IDependencyGraph dependencyGraph,
        IAdaptationAuditLog auditLog,
        ILogger<RollbackManager>? logger = null)
    {
        _snapshotStore = snapshotStore ?? throw new ArgumentNullException(nameof(snapshotStore));
        _dependencyGraph = dependencyGraph ?? throw new ArgumentNullException(nameof(dependencyGraph));
        _auditLog = auditLog ?? throw new ArgumentNullException(nameof(auditLog));
        _logger = logger;
    }

    /// <summary>Prepare for inherit.</summary>
    public void PrepareForInherit(string adaptationId, IReadOnlyList<string> affectedPaths)
    {
        if (string.IsNullOrWhiteSpace(adaptationId))
            throw new ArgumentNullException(nameof(adaptationId));
        lock (_lock)
        {
            _adaptationSnapshots[adaptationId] = (null, affectedPaths ?? Array.Empty<string>());
        }
    }

    /// <summary>Before inherit asynchronously.</summary>
    public async Task<string> BeforeInheritAsync(string adaptationId, CancellationToken ct = default)
    {
        IReadOnlyList<string> paths;
        lock (_lock)
        {
            if (!_adaptationSnapshots.TryGetValue(adaptationId, out var entry))
                throw new InvalidOperationException($"No prepared paths for adaptation {adaptationId}. Call PrepareForInherit first.");
            paths = entry.Paths;
        }

        var snapshotId = await _snapshotStore.TakeSnapshotAsync($"before-inherit-{adaptationId}", paths, ct).ConfigureAwait(false);

        lock (_lock)
        {
            _adaptationSnapshots[adaptationId] = (snapshotId, paths);
        }

        await _auditLog.LogAsync(new AdaptationAuditEntry
        {
            Id = Guid.NewGuid().ToString("N"),
            Timestamp = DateTimeOffset.UtcNow,
            AutonomyLevel = "rollback",
            Outcome = "SnapshotCreated",
            BrickId = null,
            FailureType = null,
            FilePath = null,
            RegressionPassed = true,
            Promoted = false,
            Message = $"Snapshot {snapshotId} created for adaptation {adaptationId}",
        }).ConfigureAwait(false);

        _logger?.LogInformation("Created snapshot {SnapshotId} for adaptation {AdaptationId}", snapshotId, adaptationId);
        return snapshotId;
    }

    /// <summary>Rollback asynchronously.</summary>
    public async Task RollbackAsync(string adaptationId, CancellationToken ct = default)
    {
        string snapshotId;
        lock (_lock)
        {
            if (!_adaptationSnapshots.TryGetValue(adaptationId, out var entry) || string.IsNullOrEmpty(entry.SnapshotId))
                throw new InvalidOperationException($"No snapshot found for adaptation {adaptationId}");
            snapshotId = entry.SnapshotId;
        }

        await _snapshotStore.RestoreSnapshotAsync(snapshotId, ct).ConfigureAwait(false);

        await _auditLog.LogAsync(new AdaptationAuditEntry
        {
            Id = Guid.NewGuid().ToString("N"),
            Timestamp = DateTimeOffset.UtcNow,
            AutonomyLevel = "rollback",
            Outcome = "Rollback",
            BrickId = null,
            FailureType = null,
            FilePath = null,
            RegressionPassed = false,
            Promoted = false,
            Message = $"Rolled back adaptation {adaptationId} via snapshot {snapshotId}",
        }).ConfigureAwait(false);

        lock (_lock)
        {
            _adaptationSnapshots.Remove(adaptationId);
        }

        _logger?.LogInformation("Rolled back adaptation {AdaptationId} via snapshot {SnapshotId}", adaptationId, snapshotId);
    }

    /// <summary>Rollback to snapshot asynchronously.</summary>
    public async Task RollbackToSnapshotAsync(string snapshotId, CancellationToken ct = default)
    {
        await _snapshotStore.RestoreSnapshotAsync(snapshotId, ct).ConfigureAwait(false);

        await _auditLog.LogAsync(new AdaptationAuditEntry
        {
            Id = Guid.NewGuid().ToString("N"),
            Timestamp = DateTimeOffset.UtcNow,
            AutonomyLevel = "rollback",
            Outcome = "RollbackToSnapshot",
            BrickId = null,
            FailureType = null,
            FilePath = null,
            RegressionPassed = false,
            Promoted = false,
            Message = $"Rolled back to snapshot {snapshotId}",
        }).ConfigureAwait(false);

        _logger?.LogInformation("Rolled back to snapshot {SnapshotId}", snapshotId);
    }

    /// <summary>Preview rollback asynchronously.</summary>
    public Task<RollbackImpact> PreviewRollbackAsync(string adaptationId, CancellationToken ct = default)
    {
        var transitive = _dependencyGraph.GetTransitiveDependents(adaptationId).ToList();
        var additional = transitive.Where(x => !string.Equals(x, adaptationId, StringComparison.OrdinalIgnoreCase)).ToList();
        var impact = new RollbackImpact(
            adaptationId,
            additional,
            WillCauseDataLoss: additional.Count > 0,
            Summary: additional.Count > 0
                ? $"Rolling back {adaptationId} will also affect {additional.Count} dependent component(s): {string.Join(", ", additional.Take(5))}{(additional.Count > 5 ? "..." : "")}"
                : $"Rolling back {adaptationId} will restore the affected files to their pre-inheritance state.");
        return Task.FromResult(impact);
    }
}
