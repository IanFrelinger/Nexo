using System.Collections.Concurrent;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nexo.Commercial.Fleet.Contracts.Networking.Models;
using Nexo.Commercial.Fleet.Contracts.Networking.Ports;

namespace Nexo.Commercial.Fleet.Infrastructure.Networking;
/// <summary>
/// HTTP-based network bus: broadcasts by POSTing to all peer URLs,
/// deduplicates by EventId, respects Hops/MaxHops, optional relay (gossip).
/// </summary>
public sealed class HttpNetworkBus : INetworkBus, IDisposable
{
    private readonly NetworkBusOptions _options;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<HttpNetworkBus> _logger;
    private readonly List<(string? EventType, string Id, Func<NetworkEvent, CancellationToken, Task> Handler)> _handlers = new();
    private readonly object _handlersLock = new();
    private readonly ConcurrentQueue<string> _recentEventIds = new();
    private readonly object _dedupeLock = new();
    private readonly HashSet<string> _eventIdSet = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<string> _peerNodeIds = new();
    private readonly Dictionary<string, string> _peerUrlByNodeId = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _peerLock = new();
    private CancellationTokenSource? _heartbeatCts;
    private Task? _heartbeatTask;

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public HttpNetworkBus(
        IOptions<NetworkBusOptions> options,
        IHttpClientFactory httpClientFactory,
        ILogger<HttpNetworkBus> logger)
    {
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        NodeId = _options.NodeId ?? Environment.MachineName;
        foreach (var url in _options.PeerUrls ?? [])
        {
            if (string.IsNullOrWhiteSpace(url)) continue;
            var u = url.TrimEnd('/');
            var nodeId = new Uri(u).Host;
            lock (_peerLock)
            {
                _peerUrlByNodeId[nodeId] = u;
                if (!_peerNodeIds.Contains(nodeId)) _peerNodeIds.Add(nodeId);
            }
        }
        if (_options.PeerUrls?.Count > 0 && _options.HeartbeatIntervalSeconds > 0)
            StartHeartbeat();
    }

    /// <summary>node id value.</summary>
    public string NodeId { get; }

    public IReadOnlyList<string> ConnectedPeers
    {
        get { lock (_peerLock) return _peerNodeIds.ToList(); }
    }

    /// <inheritdoc />
    public async Task BroadcastAsync(NetworkEvent evt, CancellationToken cancellationToken = default)
    {
        if (evt == null) throw new ArgumentNullException(nameof(evt));
        var peers = ConnectedPeers;
        foreach (var nodeId in peers)
        {
            if (string.Equals(nodeId, NodeId, StringComparison.OrdinalIgnoreCase)) continue;
            await SendAsync(nodeId, evt, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public async Task SendAsync(string targetNodeId, NetworkEvent evt, CancellationToken cancellationToken = default)
    {
        if (evt == null) throw new ArgumentNullException(nameof(evt));
        if (string.IsNullOrEmpty(targetNodeId)) return;

        string? baseUrl;
        lock (_peerLock) _peerUrlByNodeId.TryGetValue(targetNodeId, out baseUrl);
        if (string.IsNullOrEmpty(baseUrl))
        {
            _logger.LogWarning("No peer URL for node {NodeId}, skipping send", targetNodeId);
            return;
        }

        var relay = evt with { Hops = evt.Hops + 1 };
        if (relay.Hops > relay.MaxHops)
        {
            _logger.LogDebug("Dropping event {EventId} (hops {Hops} > max {MaxHops})", evt.EventId, relay.Hops, relay.MaxHops);
            return;
        }

        try
        {
            var client = _httpClientFactory.CreateClient("Nexo.Networking");
            client.BaseAddress = new Uri(baseUrl + "/");
            var response = await client.PostAsJsonAsync("/api/network/events", relay, JsonOptions, cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
                _logger.LogWarning("Failed to send event to {NodeId}: {Status}", targetNodeId, response.StatusCode);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send event to {NodeId}", targetNodeId);
        }
    }

    /// <inheritdoc />
    public Task DeliverAsync(NetworkEvent evt, CancellationToken cancellationToken = default)
    {
        if (evt == null) throw new ArgumentNullException(nameof(evt));

        if (!TryAddSeen(evt.EventId))
        {
            _logger.LogDebug("Duplicate event {EventId}, skipping", evt.EventId);
            return Task.CompletedTask;
        }

        List<(string?, Func<NetworkEvent, CancellationToken, Task>)> copy;
        lock (_handlersLock)
        {
            copy = _handlers.Select(h => (h.EventType, h.Handler)).ToList();
        }
        foreach (var (eventType, handler) in copy)
        {
            if (eventType != null && eventType != "*" && !string.Equals(eventType, evt.EventType, StringComparison.OrdinalIgnoreCase)) continue;
            _ = Task.Run(() => handler(evt, cancellationToken), cancellationToken);
        }

        if (evt.Hops < evt.MaxHops)
        {
            var relay = evt with { Hops = evt.Hops + 1 };
            _ = RelayToPeersExceptAsync(relay, evt.SourceNodeId, cancellationToken);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<IDisposable> SubscribeAsync(string? eventType, Func<NetworkEvent, CancellationToken, Task> handler, CancellationToken cancellationToken = default)
    {
        if (handler == null) throw new ArgumentNullException(nameof(handler));
        var id = Guid.NewGuid().ToString();
        lock (_handlersLock)
        {
            _handlers.Add((eventType, id, handler));
        }
        return Task.FromResult<IDisposable>(new SubscriptionToken(() =>
        {
            lock (_handlersLock)
            {
                for (var i = _handlers.Count - 1; i >= 0; i--)
                {
                    if (_handlers[i].Id == id) { _handlers.RemoveAt(i); break; }
                }
            }
        }));
    }

    private bool TryAddSeen(string eventId)
    {
        lock (_dedupeLock)
        {
            if (_eventIdSet.Contains(eventId)) return false;
            _eventIdSet.Add(eventId);
            _recentEventIds.Enqueue(eventId);
            while (_recentEventIds.Count > _options.MaxEventHistory && _recentEventIds.TryDequeue(out var old))
                _eventIdSet.Remove(old);
            return true;
        }
    }

    private void StartHeartbeat()
    {
        _heartbeatCts = new CancellationTokenSource();
        _heartbeatTask = Task.Run(async () =>
        {
            while (!_heartbeatCts.Token.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromSeconds(_options.HeartbeatIntervalSeconds), _heartbeatCts.Token).ConfigureAwait(false);
                var evt = new NetworkEvent
                {
                    EventId = Guid.NewGuid().ToString(),
                    SourceNodeId = NodeId,
                    EventType = NetworkEventTypes.NodeHeartbeat,
                    Timestamp = DateTimeOffset.UtcNow,
                    MaxHops = 1
                };
                await BroadcastAsync(evt, _heartbeatCts.Token).ConfigureAwait(false);
            }
        });
    }

    private async Task RelayToPeersExceptAsync(NetworkEvent evt, string? exceptNodeId, CancellationToken cancellationToken)
    {
        var peers = ConnectedPeers;
        foreach (var nodeId in peers)
        {
            if (string.Equals(nodeId, NodeId, StringComparison.OrdinalIgnoreCase)) continue;
            if (!string.IsNullOrEmpty(exceptNodeId) && string.Equals(nodeId, exceptNodeId, StringComparison.OrdinalIgnoreCase)) continue;
            await SendAsync(nodeId, evt, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>Releases resources held by this instance.</summary>
    public void Dispose() => _heartbeatCts?.Cancel();

    private sealed class SubscriptionToken : IDisposable
    {
        private readonly Action _onDispose;

        public SubscriptionToken(Action onDispose) => _onDispose = onDispose;
        /// <summary>Releases resources held by this instance.</summary>
        public void Dispose() => _onDispose();
    }
}
