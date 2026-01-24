using System.Net;
using Microsoft.Extensions.Logging;
using Nexo.Adapters.GeoTerrain.Validation;
using Nexo.Orchestration.Resilience;

namespace Nexo.Adapters.GeoTerrain.Providers;

/// <summary>
/// Downloads a single Mapbox raster tile (e.g. satellite imagery) with retry logic.
/// </summary>
public sealed class MapboxRasterTileDownloader
{
    private readonly HttpClient _http;
    private readonly ILogger<MapboxRasterTileDownloader> _logger;
    private readonly string _accessToken;
    private readonly RetryPolicy? _retryPolicy;

    public MapboxRasterTileDownloader(
        HttpClient httpClient,
        string accessToken,
        ILogger<MapboxRasterTileDownloader>? logger = null,
        RetryPolicy? retryPolicy = null)
    {
        _http = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        if (string.IsNullOrWhiteSpace(accessToken)) throw new ArgumentException("accessToken is required.", nameof(accessToken));
        _accessToken = accessToken.Trim();
        _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<MapboxRasterTileDownloader>.Instance;
        _retryPolicy = retryPolicy ?? new RetryPolicy(
            strategy: RetryStrategy.ExponentialBackoff,
            maxAttempts: 3,
            initialDelay: TimeSpan.FromSeconds(1),
            maxDelay: TimeSpan.FromMinutes(2),
            logger: null);
    }

    public async Task<byte[]> DownloadAsync(string tilesetId, int z, int x, int y, string format, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(tilesetId)) throw new ArgumentException("tilesetId is required.", nameof(tilesetId));
        if (string.IsNullOrWhiteSpace(format)) throw new ArgumentException("format is required.", nameof(format));

        var url = BuildUrl(tilesetId.Trim(), z, x, y, format.Trim(), _accessToken);

        byte[] bytes;
        if (_retryPolicy != null)
        {
            bytes = await _retryPolicy.ExecuteAsync(
                async () => await _http.GetByteArrayAsync(url, ct),
                shouldRetry: ex => ex is HttpRequestException || ex is TaskCanceledException || ex is TimeoutException,
                ct);
        }
        else
        {
            bytes = await _http.GetByteArrayAsync(url, ct);
        }

        // Validate data integrity
        if (bytes.Length == 0)
        {
            throw new InvalidOperationException($"Downloaded tile {z}/{x}/{y} is empty");
        }

        // Compute checksum for logging/debugging
        var checksum = DataIntegrityChecker.ComputeChecksum(bytes);
        _logger.LogInformation("Downloaded raster tile {Tileset} z={Z} x={X} y={Y} format={Format} bytes={Bytes} checksum={Checksum}", 
            tilesetId, z, x, y, format, bytes.Length, checksum);
        
        return bytes;
    }

    private static string BuildUrl(string tilesetId, int z, int x, int y, string format, string token)
    {
        // v4 endpoint:
        // https://api.mapbox.com/v4/{tileset}/{z}/{x}/{y}.{format}?access_token=...
        return $"https://api.mapbox.com/v4/{tilesetId}/{z}/{x}/{y}.{format}?access_token={WebUtility.UrlEncode(token)}";
    }
}

