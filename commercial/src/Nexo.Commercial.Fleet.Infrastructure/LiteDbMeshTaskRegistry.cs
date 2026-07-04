using LiteDB;
using Nexo.Commercial.Fleet.Contracts.Models;
using Nexo.Commercial.Fleet.Contracts.Ports;

namespace Nexo.Commercial.Fleet.Infrastructure;

/// <summary>
/// LiteDB-backed mesh task registry (director persistence).
/// </summary>
public sealed class LiteDbMeshTaskRegistry : IMeshTaskRegistry
{
    private readonly string _connectionString;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public LiteDbMeshTaskRegistry(string pathOrConnectionString)
    {
        _connectionString = LiteDbMeshDirectorConnection.ToConnectionString(pathOrConnectionString);
    }

    /// <summary>Creates async.</summary>
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
            using var db = new LiteDatabase(_connectionString);
            var col = db.GetCollection<MeshTaskDoc>(LiteDbMeshDirectorConnection.TasksCollection);
            col.EnsureIndex(x => x.IdempotencyKey);

            if (idem is not null)
            {
                var existing = col.FindOne(x => x.IdempotencyKey == idem);
                if (existing is not null)
                    return existing.ToState();
            }

            col.Insert(MeshTaskDoc.FromState(task));
            return task;
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>Attempts to get by idempotency key async.</summary>
    public Task<MeshTaskState?> TryGetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(idempotencyKey))
            return Task.FromResult<MeshTaskState?>(null);

        var key = idempotencyKey.Trim();
        using var db = new LiteDatabase(_connectionString);
        var col = db.GetCollection<MeshTaskDoc>(LiteDbMeshDirectorConnection.TasksCollection);
        var doc = col.FindOne(x => x.IdempotencyKey == key);
        return Task.FromResult(doc?.ToState());
    }

    /// <summary>Gets async.</summary>
    public async Task<MeshTaskState?> GetAsync(string taskId, CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            using var db = new LiteDatabase(_connectionString);
            var col = db.GetCollection<MeshTaskDoc>(LiteDbMeshDirectorConnection.TasksCollection);
            var doc = col.FindById(taskId);
            return doc?.ToState();
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>List async operation.</summary>
    public async Task<IReadOnlyList<MeshTaskState>> ListAsync(CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            using var db = new LiteDatabase(_connectionString);
            var col = db.GetCollection<MeshTaskDoc>(LiteDbMeshDirectorConnection.TasksCollection);
            return col.FindAll()
                .Select(d => d.ToState())
                .OrderByDescending(t => t.Priority)
                .ThenByDescending(t => t.CreatedAtUtc)
                .ToList();
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>Update async operation.</summary>
    public async Task<bool> UpdateAsync(MeshTaskState task, CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            using var db = new LiteDatabase(_connectionString);
            var col = db.GetCollection<MeshTaskDoc>(LiteDbMeshDirectorConnection.TasksCollection);
            if (col.FindById(task.TaskId) is null)
                return false;
            return col.Update(MeshTaskDoc.FromState(task));
        }
        finally
        {
            _lock.Release();
        }
    }
}
