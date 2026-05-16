using System.Collections.Concurrent;
using Nexo.Core.Application.Fleet.Models;
using Nexo.Core.Application.Fleet.Ports;

namespace Nexo.Infrastructure.Fleet;

/// <summary>
/// Thread-safe in-memory mesh task store (Phase 1).
/// </summary>
public sealed class InMemoryMeshTaskRegistry : IMeshTaskRegistry
{
    private readonly ConcurrentDictionary<string, MeshTaskState> _tasks = new(StringComparer.OrdinalIgnoreCase);
    private readonly SemaphoreSlim _lock = new(1, 1);

    public async Task<MeshTaskState> CreateAsync(MeshTaskCreateSpec spec, CancellationToken cancellationToken = default)
    {
        var id = $"{DateTimeOffset.UtcNow:yyyyMMddHHmmssfff}-{Guid.NewGuid():N}";
        var task = new MeshTaskState(
            TaskId: id,
            Name: spec.Name,
            Steps: Math.Max(1, spec.Steps),
            RequiredBrickIds: spec.RequiredBrickIds ?? Array.Empty<string>(),
            Affinity: spec.Affinity ?? new Dictionary<string, string>(),
            Priority: spec.Priority,
            DeadlineUtc: spec.DeadlineUtc,
            Status: MeshTaskStatus.Pending,
            AssignedPeerId: null,
            AssignedApiBaseUrl: null,
            PlacementReason: null,
            AttemptCount: 0,
            CreatedAtUtc: DateTimeOffset.UtcNow,
            LastScheduledAtUtc: null);

        await _lock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            _tasks[id] = task;
            return task;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<MeshTaskState?> GetAsync(string taskId, CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            return _tasks.TryGetValue(taskId, out var t) ? t : null;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<IReadOnlyList<MeshTaskState>> ListAsync(CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            return _tasks.Values
                .OrderByDescending(t => t.Priority)
                .ThenByDescending(t => t.CreatedAtUtc)
                .ToList();
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<bool> UpdateAsync(MeshTaskState task, CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (!_tasks.ContainsKey(task.TaskId))
                return false;
            _tasks[task.TaskId] = task;
            return true;
        }
        finally
        {
            _lock.Release();
        }
    }
}
