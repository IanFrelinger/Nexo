using System.Collections.Concurrent;
using Nexo.Core.Application.Fleet.Models;
using Nexo.Core.Application.Fleet.Ports;

namespace Nexo.Infrastructure.Fleet;

/// <summary>
/// Thread-safe in-memory fleet registry (Phase 1). Replaced by external store in later phases.
/// </summary>
public sealed class InMemoryFleetNodeRegistry : IFleetNodeRegistry
{
    private readonly ConcurrentDictionary<string, MeshFleetNodeState> _nodes = new(StringComparer.OrdinalIgnoreCase);
    private readonly SemaphoreSlim _lock = new(1, 1);

    public async Task RegisterOrUpdateAsync(MeshFleetNodeState node, CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            _nodes[node.PeerId] = node;
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
            return _nodes.TryRemove(peerId, out _);
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
            return _nodes.Values.OrderBy(n => n.PeerId, StringComparer.OrdinalIgnoreCase).ToList();
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
            return _nodes.TryGetValue(peerId, out var n) ? n : null;
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
            if (!_nodes.TryGetValue(peerId, out var existing))
                return false;
            _nodes[peerId] = existing with { Drained = drained };
            return true;
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
            if (!_nodes.TryGetValue(peerId, out var existing))
                return false;
            _nodes[peerId] = existing with { Admitted = admitted };
            return true;
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
            if (_nodes.TryGetValue(peerId, out var existing))
            {
                var depth = reportedQueueDepth ?? existing.ReportedQueueDepth;
                if (depth < 0) depth = 0;
                _nodes[peerId] = existing with
                {
                    LastHeartbeatUtc = DateTimeOffset.UtcNow,
                    ReportedQueueDepth = depth
                };
            }
        }
        finally
        {
            _lock.Release();
        }
    }
}
