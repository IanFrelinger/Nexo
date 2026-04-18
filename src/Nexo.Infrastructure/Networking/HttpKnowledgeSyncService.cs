using System.Collections.Concurrent;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nexo.Core.Application.Networking.Models;
using Nexo.Core.Application.Networking.Ports;

namespace Nexo.Infrastructure.Networking;

/// <summary>
/// HTTP-based knowledge sync: push chunks to peer /api/knowledge/sync and pull from peer GET /api/knowledge/sync.
/// </summary>
public sealed class HttpKnowledgeSyncService : IKnowledgeSyncService
{
    private readonly KnowledgeSyncServiceOptions _options;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<HttpKnowledgeSyncService> _logger;
    private readonly Dictionary<string, string> _peerUrlByNodeId = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, DateTimeOffset> _lastSyncByPeer = new(StringComparer.OrdinalIgnoreCase);

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public HttpKnowledgeSyncService(
        IOptions<KnowledgeSyncServiceOptions> options,
        IHttpClientFactory httpClientFactory,
        ILogger<HttpKnowledgeSyncService> logger)
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
    public async Task PushAsync(KnowledgeChunk chunk, CancellationToken cancellationToken = default)
    {
        if (chunk == null) throw new ArgumentNullException(nameof(chunk));

        foreach (var (nodeId, baseUrl) in _peerUrlByNodeId.ToList())
        {
            if (string.Equals(nodeId, NodeId, StringComparison.OrdinalIgnoreCase)) continue;
            try
            {
                var client = _httpClientFactory.CreateClient("Nexo.Networking");
                client.BaseAddress = new Uri(baseUrl + "/");
                var response = await client.PostAsJsonAsync("/api/knowledge/sync", chunk, JsonOptions, cancellationToken).ConfigureAwait(false);
                if (response.IsSuccessStatusCode)
                    _lastSyncByPeer[nodeId] = DateTimeOffset.UtcNow;
                else
                    _logger.LogWarning("Push to {NodeId} failed: {Status}", nodeId, response.StatusCode);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Push to {NodeId} failed", nodeId);
            }
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<KnowledgeChunk>> PullAsync(string nodeId, string? contentType = null, int maxCount = 100, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(nodeId)) return Array.Empty<KnowledgeChunk>();
        if (!_peerUrlByNodeId.TryGetValue(nodeId, out var baseUrl)) return Array.Empty<KnowledgeChunk>();

        var query = new List<string> { $"maxCount={maxCount}" };
        if (!string.IsNullOrEmpty(contentType)) query.Add($"contentType={Uri.EscapeDataString(contentType)}");
        var path = "/api/knowledge/sync?" + string.Join("&", query);

        try
        {
            var client = _httpClientFactory.CreateClient("Nexo.Networking");
            client.BaseAddress = new Uri(baseUrl + "/");
            var list = await client.GetFromJsonAsync<List<KnowledgeChunk>>(path, JsonOptions, cancellationToken).ConfigureAwait(false);
            if (list != null)
            {
                _lastSyncByPeer[nodeId] = DateTimeOffset.UtcNow;
                return list;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Pull from {NodeId} failed", nodeId);
        }

        return Array.Empty<KnowledgeChunk>();
    }

    /// <inheritdoc />
    public Task<KnowledgeSyncStatus> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        var peerIds = _peerUrlByNodeId.Keys.ToList();
        var lastSync = new Dictionary<string, DateTimeOffset>(_lastSyncByPeer, StringComparer.OrdinalIgnoreCase);
        return Task.FromResult(new KnowledgeSyncStatus
        {
            NodeId = NodeId,
            PeerIds = peerIds,
            LastSyncByPeer = lastSync
        });
    }
}
