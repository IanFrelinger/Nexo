using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using Nexo.BrickContracts;

namespace Nexo.BrickCatalog;

/// <summary>
/// Aggregates brick catalog from multiple instance URLs. Sets HostBaseUrl on each entry so callers know where to execute.
/// </summary>
public sealed class CatalogAggregator
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly CentralCatalogOptions _options;
    private readonly ILogger<CatalogAggregator> _logger;
    private List<BrickCatalogEntryDto>? _cached;
    private DateTime _cacheExpiry = DateTime.MinValue;
    private readonly object _cacheLock = new();

    public CatalogAggregator(
        IHttpClientFactory httpClientFactory,
        IOptions<CentralCatalogOptions> options,
        ILogger<CatalogAggregator> logger)
    {
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>Returns merged catalog from all instances; each entry has HostBaseUrl set to the instance that provided it.</summary>
    public async Task<IReadOnlyList<BrickCatalogEntryDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        if (_options.CacheTtlSeconds > 0)
        {
            lock (_cacheLock)
            {
                if (_cached != null && DateTime.UtcNow < _cacheExpiry)
                    return _cached;
            }
        }

        var merged = new Dictionary<string, BrickCatalogEntryDto>(StringComparer.OrdinalIgnoreCase);

        foreach (var baseUrl in _options.InstanceBaseUrls ?? [])
        {
            if (string.IsNullOrWhiteSpace(baseUrl)) continue;
            var url = baseUrl.TrimEnd('/');
            try
            {
                var client = _httpClientFactory.CreateClient("CentralCatalog.Instance");
                client.BaseAddress = new Uri(url + "/");
                var response = await client.GetFromJsonAsync<BrickCatalogResponseDto>("/api/bricks", cancellationToken).ConfigureAwait(false);
                if (response?.Bricks == null) continue;
                foreach (var entry in response.Bricks)
                {
                    if (string.IsNullOrEmpty(entry.Id)) continue;
                    if (!merged.ContainsKey(entry.Id))
                    {
                        entry.HostBaseUrl ??= url;
                        merged[entry.Id] = entry;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to fetch catalog from {BaseUrl}", url);
            }
        }

        var list = merged.Values.ToList();
        if (_options.CacheTtlSeconds > 0)
        {
            lock (_cacheLock)
            {
                _cached = list;
                _cacheExpiry = DateTime.UtcNow.AddSeconds(_options.CacheTtlSeconds);
            }
        }
        return list;
    }

    /// <summary>Returns one brick by id (first instance that advertises it).</summary>
    public async Task<BrickCatalogEntryDto?> GetByIdAsync(string brickId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(brickId)) return null;
        var all = await GetAllAsync(cancellationToken).ConfigureAwait(false);
        return all.FirstOrDefault(b => string.Equals(b.Id, brickId, StringComparison.OrdinalIgnoreCase));
    }
}
