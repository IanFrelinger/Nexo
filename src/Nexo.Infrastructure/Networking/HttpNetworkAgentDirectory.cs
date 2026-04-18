using System.Collections.Concurrent;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nexo.Core.Application.Networking.Models;
using Nexo.Core.Application.Networking.Ports;

namespace Nexo.Infrastructure.Networking;

/// <summary>
/// In-memory directory of local agents + HTTP discovery of peer agents.
/// </summary>
public sealed class HttpNetworkAgentDirectory : INetworkAgentDirectory
{
    private readonly NetworkAgentDirectoryOptions _options;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<HttpNetworkAgentDirectory> _logger;
    private readonly ConcurrentDictionary<string, NetworkAgentEntry> _local = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, (DateTimeOffset At, List<NetworkAgentEntry> Entries)> _discoveredByNode = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, string> _peerUrlByNodeId = new(StringComparer.OrdinalIgnoreCase);

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public HttpNetworkAgentDirectory(
        IOptions<NetworkAgentDirectoryOptions> options,
        IHttpClientFactory httpClientFactory,
        ILogger<HttpNetworkAgentDirectory> logger)
    {
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        NodeId = _options.NodeId ?? Environment.MachineName;
        foreach (var url in _options.PeerUrls ?? [])
        {
            if (string.IsNullOrWhiteSpace(url)) continue;
            var u = url.TrimEnd('/');
            try
            {
                var nodeId = new Uri(u).Host;
                _peerUrlByNodeId[nodeId] = u;
            }
            catch (UriFormatException ex) { System.Diagnostics.Debug.WriteLine($"Skipping invalid peer URL: {ex.Message}"); }
        }
    }

    public string NodeId { get; }

    /// <inheritdoc />
    public Task RegisterAsync(NetworkAgentEntry entry, CancellationToken cancellationToken = default)
    {
        if (entry == null) throw new ArgumentNullException(nameof(entry));
        var updated = entry with { NodeId = NodeId, LastSeen = DateTimeOffset.UtcNow };
        _local[entry.AgentId] = updated;
        _logger.LogDebug("Registered agent {AgentId} on node {NodeId}", entry.AgentId, NodeId);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task UnregisterAsync(string agentId, CancellationToken cancellationToken = default)
    {
        _local.TryRemove(agentId, out _);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<NetworkAgentEntry>> GetByNodeAsync(string nodeId, CancellationToken cancellationToken = default)
    {
        if (string.Equals(nodeId, NodeId, StringComparison.OrdinalIgnoreCase))
            return Task.FromResult<IReadOnlyList<NetworkAgentEntry>>(_local.Values.ToList());

        if (_discoveredByNode.TryGetValue(nodeId, out var pair))
            return Task.FromResult<IReadOnlyList<NetworkAgentEntry>>(pair.Entries);

        return Task.FromResult<IReadOnlyList<NetworkAgentEntry>>(Array.Empty<NetworkAgentEntry>());
    }

    /// <inheritdoc />
    public async Task<NetworkAgentEntry?> GetByAgentIdAsync(string agentId, CancellationToken cancellationToken = default)
    {
        if (_local.TryGetValue(agentId, out var local)) return local;
        await RefreshFromPeersAsync(cancellationToken).ConfigureAwait(false);
        foreach (var (_, (_, entries)) in _discoveredByNode)
        {
            var found = entries.FirstOrDefault(e => string.Equals(e.AgentId, agentId, StringComparison.OrdinalIgnoreCase));
            if (found != null) return found;
        }
        return null;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<NetworkAgentEntry>> GetAllAsync(bool refreshFromPeers = false, CancellationToken cancellationToken = default)
    {
        if (refreshFromPeers) await RefreshFromPeersAsync(cancellationToken).ConfigureAwait(false);

        var list = new List<NetworkAgentEntry>(_local.Values);
        foreach (var (_, (_, entries)) in _discoveredByNode)
            list.AddRange(entries);
        return list;
    }

    /// <inheritdoc />
    public async Task RefreshFromPeersAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var maxAge = TimeSpan.FromSeconds(_options.DiscoveredEntryMaxAgeSeconds);

        foreach (var (nodeId, baseUrl) in _peerUrlByNodeId.ToList())
        {
            if (string.Equals(nodeId, NodeId, StringComparison.OrdinalIgnoreCase)) continue;

            if (_discoveredByNode.TryGetValue(nodeId, out var pair) && (now - pair.At) < maxAge)
                continue;

            try
            {
                var client = _httpClientFactory.CreateClient("Nexo.Networking");
                client.BaseAddress = new Uri(baseUrl + "/");
                var agents = await client.GetFromJsonAsync<List<NetworkAgentEntry>>("/api/agents", JsonOptions, cancellationToken).ConfigureAwait(false);
                var entries = agents ?? new List<NetworkAgentEntry>();
                _discoveredByNode[nodeId] = (now, entries);
                _logger.LogDebug("Discovered {Count} agents from peer {NodeId}", entries.Count, nodeId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to discover agents from peer {NodeId}", nodeId);
            }
        }
    }
}
