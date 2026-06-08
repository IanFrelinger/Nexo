using System.Collections.Concurrent;
using Nexo.Commercial.Fleet.Contracts.Models;
using Nexo.Commercial.Fleet.Contracts.Ports;

namespace Nexo.Commercial.Fleet.Infrastructure;

/// <summary>
/// Thread-safe in-memory mesh task store (Phase 1); Phase 3 adds idempotency key index.
/// </summary>
public sealed class InMemoryMeshTaskRegistry : IMeshTaskRegistry
{
    private readonly ConcurrentDictionary<string, MeshTaskState> _tasks = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, string> _idempotencyToTaskId = new(StringComparer.OrdinalIgnoreCase);
    private readonly SemaphoreSlim _lock = new(1, 1);

    public async Task<MeshTaskState> CreateAsync(MeshTaskCreateSpec spec, CancellationToken cancellationToken = default)
    {
        var id = $"{DateTimeOffset.UtcNow:yyyyMMddHHmmssfff}-{Guid.NewGuid():N}";
        var correlation = string.IsNullOrWhiteSpace(spec.CorrelationId) ? null : spec.CorrelationId.Trim();
        var idem = string.IsNullOrWhiteSpace(spec.IdempotencyKey) ? null : spec.IdempotencyKey.Trim();

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
            LastScheduledAtUtc: null,
            CorrelationId: correlation,
            IdempotencyKey: idem,
            LastScheduleIdempotencyKey: null,
            ResultSummary: null,
            ResultHandle: null,
            LeaseToken: null,
            LeaseOwnerPeerId: null,
            LeaseExpiresUtc: null,
            CheckpointHandle: null);

        await _lock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (idem is not null)
            {
                if (_idempotencyToTaskId.TryGetValue(idem, out var existingId) &&
                    _tasks.TryGetValue(existingId, out var existingTask))
                {
                    return existingTask;
                }

                _idempotencyToTaskId[idem] = id;
            }

            _tasks[id] = task;
            return task;
        }
        finally
        {
            _lock.Release();
        }
    }

    public Task<MeshTaskState?> TryGetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(idempotencyKey))
            return Task.FromResult<MeshTaskState?>(null);
        var key = idempotencyKey.Trim();
        return Task.FromResult(
            _idempotencyToTaskId.TryGetValue(key, out var taskId) && _tasks.TryGetValue(taskId, out var t)
                ? t
                : null);
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
