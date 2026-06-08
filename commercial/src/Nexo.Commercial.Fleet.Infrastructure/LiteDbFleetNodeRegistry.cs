using LiteDB;
using Nexo.Commercial.Fleet.Contracts.Models;
using Nexo.Commercial.Fleet.Contracts.Ports;

namespace Nexo.Commercial.Fleet.Infrastructure;

/// <summary>
/// LiteDB-backed fleet node registry (director persistence).
/// </summary>
public sealed class LiteDbFleetNodeRegistry : IFleetNodeRegistry
{
    private readonly string _connectionString;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public LiteDbFleetNodeRegistry(string pathOrConnectionString)
    {
        _connectionString = LiteDbMeshDirectorConnection.ToConnectionString(pathOrConnectionString);
    }

    public async Task RegisterOrUpdateAsync(MeshFleetNodeState node, CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            using var db = new LiteDatabase(_connectionString);
            var col = db.GetCollection<MeshFleetNodeDoc>(LiteDbMeshDirectorConnection.FleetCollection);
            col.Upsert(MeshFleetNodeDoc.FromState(node));
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<bool> RemoveAsync(string peerId, CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            using var db = new LiteDatabase(_connectionString);
            var col = db.GetCollection<MeshFleetNodeDoc>(LiteDbMeshDirectorConnection.FleetCollection);
            return col.Delete(peerId);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<IReadOnlyList<MeshFleetNodeState>> ListAsync(CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            using var db = new LiteDatabase(_connectionString);
            var col = db.GetCollection<MeshFleetNodeDoc>(LiteDbMeshDirectorConnection.FleetCollection);
            return col.FindAll()
                .Select(d => d.ToState())
                .OrderBy(n => n.PeerId, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<MeshFleetNodeState?> GetAsync(string peerId, CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            using var db = new LiteDatabase(_connectionString);
            var col = db.GetCollection<MeshFleetNodeDoc>(LiteDbMeshDirectorConnection.FleetCollection);
            var doc = col.FindById(peerId);
            return doc?.ToState();
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<bool> SetDrainedAsync(string peerId, bool drained, CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            using var db = new LiteDatabase(_connectionString);
            var col = db.GetCollection<MeshFleetNodeDoc>(LiteDbMeshDirectorConnection.FleetCollection);
            var doc = col.FindById(peerId);
            if (doc is null)
                return false;
            doc.Drained = drained;
            return col.Update(doc);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<bool> SetAdmittedAsync(string peerId, bool admitted, CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            using var db = new LiteDatabase(_connectionString);
            var col = db.GetCollection<MeshFleetNodeDoc>(LiteDbMeshDirectorConnection.FleetCollection);
            var doc = col.FindById(peerId);
            if (doc is null)
                return false;
            doc.Admitted = admitted;
            return col.Update(doc);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task HeartbeatAsync(string peerId, int? reportedQueueDepth = null, CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            using var db = new LiteDatabase(_connectionString);
            var col = db.GetCollection<MeshFleetNodeDoc>(LiteDbMeshDirectorConnection.FleetCollection);
            var doc = col.FindById(peerId);
            if (doc is null)
                return;

            var depth = reportedQueueDepth ?? doc.ReportedQueueDepth;
            if (depth < 0) depth = 0;
            doc.LastHeartbeatUtc = DateTimeOffset.UtcNow;
            doc.ReportedQueueDepth = depth;
            col.Update(doc);
        }
        finally
        {
            _lock.Release();
        }
    }
}
