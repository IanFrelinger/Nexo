using Microsoft.Extensions.Logging;
using System.IO.Compression;
using Nexo.GeoTerrain;
using Nexo.Orchestration.GeoTerrain.Ports;

namespace Nexo.Adapters.GeoTerrain.Providers;

/// <summary>
/// Downloads SRTM tiles over HTTP. Intended for non-air-gapped environments.
/// Supports optional disk caching to avoid re-downloading tiles.
/// </summary>
public sealed class SrtmHttpElevationProvider : IElevationProvider
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<SrtmHttpElevationProvider> _logger;
    private readonly string _baseUrl;
    private readonly string? _cacheRoot;
    private readonly bool _persistCache;

    public SrtmHttpElevationProvider(
        HttpClient httpClient, 
        string? baseUrl, 
        ILogger<SrtmHttpElevationProvider> logger,
        string? cacheRoot = null,
        bool persistCache = true)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        if (string.IsNullOrWhiteSpace(baseUrl))
            throw new ArgumentException("SRTM base URL must be configured for HTTP provider.", nameof(baseUrl));
        _baseUrl = baseUrl!;
        _cacheRoot = string.IsNullOrWhiteSpace(cacheRoot) ? null : cacheRoot.Trim();
        _persistCache = persistCache;
    }

    public async Task<ElevationTile> GetSrtmTileAsync(SrtmTileId tileId, CancellationToken cancellationToken = default)
    {
        // Check cache first if enabled
        if (_cacheRoot != null)
        {
            var cachePath = Path.Combine(_cacheRoot, "srtm", $"{tileId}.hgt");
            if (File.Exists(cachePath))
            {
                _logger.LogInformation("Loading SRTM tile {Tile} from cache: {Path}", tileId.ToString(), cachePath);
                var cachedBytes = await File.ReadAllBytesAsync(cachePath, cancellationToken);
                var cachedSummary = SrtmHgtParser.Analyze(cachedBytes);
                return new ElevationTile
                {
                    TileId = tileId,
                    HgtBytes = cachedBytes,
                    Size = cachedSummary.Size,
                    MinMeters = cachedSummary.MinMeters,
                    MaxMeters = cachedSummary.MaxMeters,
                    NoDataSamples = cachedSummary.NoDataSamples,
                    Metadata = new Dictionary<string, object>
                    {
                        ["provider"] = "srtm-http",
                        ["cached"] = true,
                        ["cache-path"] = cachePath
                    }
                };
            }
        }

        // Expect a direct .hgt (or compressed) endpoint. Example:
        // baseUrl = https://example.com/srtm/
        // -> https://example.com/srtm/N37W122.hgt (or .hgt.zip / .hgt.gz)
        var baseUrl = _baseUrl;
        if (!baseUrl.EndsWith("/")) baseUrl += "/";

        var fileName = tileId + ".hgt";
        var url = baseUrl + fileName;

        _logger.LogInformation("Downloading SRTM tile {Tile} from {Url}", tileId.ToString(), url);
        var payload = await _httpClient.GetByteArrayAsync(url, cancellationToken);

        // Optional: Validate checksum if ETag or Content-MD5 header is available
        // (This would require checking response headers, which GetByteArrayAsync doesn't expose)
        // For now, we rely on the parser to detect corruption

        // Support common compressions: zip/gzip. If payload is already HGT, ExtractHgtBytes returns as-is.
        var bytes = ExtractHgtBytes(payload, fileName);

        // Save to cache if enabled
        if (_cacheRoot != null && _persistCache)
        {
            try
            {
                var cachePath = Path.Combine(_cacheRoot, "srtm", $"{tileId}.hgt");
                Directory.CreateDirectory(Path.GetDirectoryName(cachePath)!);
                await File.WriteAllBytesAsync(cachePath, bytes, cancellationToken);
                _logger.LogInformation("Cached SRTM tile {Tile} to {Path}", tileId.ToString(), cachePath);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Failed to cache tile {Tile} (continuing)", tileId.ToString());
            }
        }

        var summary = SrtmHgtParser.Analyze(bytes);
        return new ElevationTile
        {
            TileId = tileId,
            HgtBytes = bytes,
            Size = summary.Size,
            MinMeters = summary.MinMeters,
            MaxMeters = summary.MaxMeters,
            NoDataSamples = summary.NoDataSamples,
            Metadata = new Dictionary<string, object>
            {
                ["provider"] = "srtm-http",
                ["url"] = url,
                ["cached"] = false
            }
        };
    }

    private static byte[] ExtractHgtBytes(byte[] payload, string expectedFileName)
    {
        if (payload.Length >= 2 && payload[0] == (byte)'P' && payload[1] == (byte)'K')
        {
            // ZIP archive
            using var ms = new MemoryStream(payload, writable: false);
            using var zip = new ZipArchive(ms, ZipArchiveMode.Read, leaveOpen: false);
            var entry = zip.Entries.FirstOrDefault(e =>
                e.Name.EndsWith(".hgt", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(e.Name, expectedFileName, StringComparison.OrdinalIgnoreCase));

            if (entry == null)
                throw new InvalidOperationException("ZIP did not contain an .hgt entry.");

            using var es = entry.Open();
            using var outMs = new MemoryStream();
            es.CopyTo(outMs);
            return outMs.ToArray();
        }

        if (payload.Length >= 2 && payload[0] == 0x1F && payload[1] == 0x8B)
        {
            // GZIP stream
            using var ms = new MemoryStream(payload, writable: false);
            using var gz = new GZipStream(ms, CompressionMode.Decompress, leaveOpen: false);
            using var outMs = new MemoryStream();
            gz.CopyTo(outMs);
            return outMs.ToArray();
        }

        // Assume raw .hgt.
        return payload;
    }
}

