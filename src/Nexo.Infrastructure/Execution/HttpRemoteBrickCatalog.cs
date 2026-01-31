using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Nexo.BrickContracts;

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
            return response?.Bricks ?? [];
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
            return await _httpClient.GetFromJsonAsync<BrickCatalogEntryDto>(url, cancellationToken).ConfigureAwait(false);
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
}
