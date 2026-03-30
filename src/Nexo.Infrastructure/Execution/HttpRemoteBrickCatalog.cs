using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Nexo.BrickContracts;
using Nexo.BrickContracts.Capabilities;

namespace Nexo.Infrastructure.Execution;

/// <summary>
/// Fetches brick catalog from a remote host via GET /api/bricks and GET /api/bricks/{id}.
/// </summary>
public sealed class HttpRemoteBrickCatalog : IRemoteBrickCatalog
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<HttpRemoteBrickCatalog>? _logger;

    public HttpRemoteBrickCatalog(HttpClient httpClient, ILogger<HttpRemoteBrickCatalog>? logger = null)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger;
    }

    public string BaseUrl => _httpClient.BaseAddress?.ToString() ?? string.Empty;

    public async Task<IReadOnlyList<BrickCatalogEntryDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<BrickCatalogResponseDto>("/api/bricks", cancellationToken).ConfigureAwait(false);
            var bricks = response?.Bricks?.ToList() ?? [];
            var capabilities = await GetCapabilitiesAsync(cancellationToken).ConfigureAwait(false);
            if (capabilities is not null)
            {
                foreach (var entry in bricks)
                {
                    entry.HostCapabilities ??= capabilities;
                }
            }

            return bricks;
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to fetch brick catalog from {BaseUrl}", BaseUrl);
            return [];
        }
    }

    public async Task<BrickCatalogEntryDto?> GetByIdAsync(string brickId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(brickId)) return null;
        try
        {
            var url = $"/api/bricks/{Uri.EscapeDataString(brickId)}";
            var entry = await _httpClient.GetFromJsonAsync<BrickCatalogEntryDto>(url, cancellationToken).ConfigureAwait(false);
            if (entry is null)
            {
                return null;
            }

            entry.HostCapabilities ??= await GetCapabilitiesAsync(cancellationToken).ConfigureAwait(false);
            return entry;
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to fetch brick {BrickId} from {BaseUrl}", brickId, BaseUrl);
            return null;
        }
    }

    public async Task<NodeCapabilityManifestDto?> GetCapabilitiesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _httpClient
                .GetFromJsonAsync<NodeCapabilityManifestDto>("/api/capabilities", cancellationToken)
                .ConfigureAwait(false);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to fetch capabilities from {BaseUrl}", BaseUrl);
            return null;
        }
    }
}
