using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nexo.Core.Application.Fleet.Models;

namespace Nexo.Infrastructure.Fleet;

/// <summary>
/// Periodically GETs <c>/api/mesh/knowledge/export</c> from configured peers and imports into the local host.
/// </summary>
public sealed class MeshPeerKnowledgePullBackgroundService : BackgroundService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly MeshKnowledgeImportService _import;
    private readonly IOptionsMonitor<MeshPeerKnowledgeSyncOptions> _options;
    private readonly ILogger<MeshPeerKnowledgePullBackgroundService>? _logger;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public MeshPeerKnowledgePullBackgroundService(
        IHttpClientFactory httpClientFactory,
        MeshKnowledgeImportService import,
        IOptionsMonitor<MeshPeerKnowledgeSyncOptions> options,
        ILogger<MeshPeerKnowledgePullBackgroundService>? logger = null)
    {
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _import = import ?? throw new ArgumentNullException(nameof(import));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var opts = _options.CurrentValue;
            var interval = TimeSpan.FromMinutes(Math.Max(1, opts.IntervalMinutes));
            try
            {
                if (opts.Enabled && opts.PeerBaseUrls is { Length: > 0 })
                {
                    var since = DateTimeOffset.UtcNow - interval * Math.Max(1, opts.SinceLookbackMultiplier);
                    foreach (var raw in opts.PeerBaseUrls)
                    {
                        if (string.IsNullOrWhiteSpace(raw))
                            continue;
                        if (!Uri.TryCreate(raw.Trim(), UriKind.Absolute, out var baseUri))
                        {
                            _logger?.LogWarning("mesh-knowledge-sync invalid peer URL {Url}", raw);
                            continue;
                        }

                        await PullOnePeerAsync(baseUri, since, opts.MaxAdaptations, opts.MaxPatterns, stoppingToken).ConfigureAwait(false);
                    }
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "mesh-knowledge-sync round failed");
            }

            try
            {
                await Task.Delay(interval, stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }

    private async Task PullOnePeerAsync(
        Uri peerBase,
        DateTimeOffset since,
        int maxAdaptations,
        int maxPatterns,
        CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient();
        var exportUrl = new Uri(peerBase, "api/mesh/knowledge/export").ToString()
                         + $"?since={Uri.EscapeDataString(since.ToString("O"))}"
                         + $"&maxAdaptations={maxAdaptations}&maxPatterns={maxPatterns}";
        using var response = await client.GetAsync(exportUrl, cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            _logger?.LogWarning(
                "mesh-knowledge-sync peer={Peer} status={Status}",
                peerBase,
                (int)response.StatusCode);
            return;
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        var payload = await JsonSerializer.DeserializeAsync<MeshKnowledgeExportPayload>(stream, JsonOptions, cancellationToken)
            .ConfigureAwait(false);
        if (payload is null)
        {
            _logger?.LogWarning("mesh-knowledge-sync peer={Peer} empty payload", peerBase);
            return;
        }

        var result = await _import.ImportAsync(payload, cancellationToken).ConfigureAwait(false);
        _logger?.LogInformation(
            "mesh-knowledge-sync peer={Peer} adaptations={A}/{As} patterns={P}/{Ps}",
            peerBase,
            result.AdaptationsApplied,
            result.AdaptationsSkipped,
            result.PatternsApplied,
            result.PatternsSkipped);
    }
}
