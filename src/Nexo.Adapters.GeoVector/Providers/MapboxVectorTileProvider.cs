using System.Globalization;
using System.Net;
using Microsoft.Extensions.Logging;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO.VectorTiles;
using NetTopologySuite.IO.VectorTiles.Mapbox;
using Nexo.GeoTerrain;
using Nexo.GeoVector.Geometry;
using Nexo.GeoVector.Models;
using Nexo.GeoVector.Values;
using Nexo.Orchestration.GeoVector.Ports;

namespace Nexo.Adapters.GeoVector.Providers;

/// <summary>
/// Fetches vector features from Mapbox Vector Tiles API (MVT).
/// </summary>
public sealed class MapboxVectorTileProvider : IVectorProvider
{
    private readonly HttpClient _http;
    private readonly ILogger<MapboxVectorTileProvider> _logger;
    private readonly string _accessToken;
    private readonly string _tilesetId;
    private readonly int _zoom;
    private readonly string _formatExtension;
    private readonly string? _tileCacheRoot;
    private readonly bool _persistTileCache;

    public MapboxVectorTileProvider(
        HttpClient httpClient,
        string accessToken,
        string tilesetId = "mapbox.mapbox-streets-v8",
        int zoom = 15,
        string formatExtension = "mvt",
        string? tileCacheRoot = null,
        bool persistTileCache = true,
        ILogger<MapboxVectorTileProvider>? logger = null)
    {
        _http = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        if (string.IsNullOrWhiteSpace(accessToken)) throw new ArgumentException("Mapbox access token is required.", nameof(accessToken));
        if (string.IsNullOrWhiteSpace(tilesetId)) throw new ArgumentException("Mapbox tileset id is required.", nameof(tilesetId));
        if (zoom < 0 || zoom > 22) throw new ArgumentOutOfRangeException(nameof(zoom), "zoom must be within [0,22].");
        if (string.IsNullOrWhiteSpace(formatExtension)) throw new ArgumentException("formatExtension is required.", nameof(formatExtension));

        _accessToken = accessToken.Trim();
        _tilesetId = tilesetId.Trim();
        _zoom = zoom;
        _formatExtension = formatExtension.Trim().TrimStart('.');
        _tileCacheRoot = string.IsNullOrWhiteSpace(tileCacheRoot) ? null : tileCacheRoot.Trim();
        _persistTileCache = persistTileCache;
        _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<MapboxVectorTileProvider>.Instance;
    }

    public async Task<GeoFeatureSet> GetFeaturesAsync(GeoBounds bounds, FeatureKind kind, CancellationToken cancellationToken = default)
    {
        bounds.Validate();
        kind ??= FeatureKind.Building;

        var layerName = ResolveLayerName(kind);
        var tiles = ComputeTiles(bounds, _zoom);

        var features = new List<GeoFeature>(capacity: 256);
        foreach (var t in tiles)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var url = BuildTileUrl(_tilesetId, t.Z, t.X, t.Y, _formatExtension, _accessToken);
            byte[] tileBytes;
            try
            {
                tileBytes = await GetTileBytesAsync(url, t.Z, t.X, t.Y, cancellationToken);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning(ex, "Failed to download Mapbox tile {Z}/{X}/{Y}", t.Z, t.X, t.Y);
                continue;
            }

            foreach (var f in DecodeLayerFeatures(tileBytes, layerName, kind, t.Z, t.X, t.Y, cancellationToken))
            {
                features.Add(f);
            }
        }

        // Filter by bounds (tiles can include geometry outside query bounds).
        var filtered = features.Where(f => f.Geometry.Points.Any(bounds.Contains)).ToList();
        return new GeoFeatureSet(filtered);
    }

    private async Task<byte[]> GetTileBytesAsync(string url, int z, int x, int y, CancellationToken ct)
    {
        var root = _tileCacheRoot;
        if (root != null)
        {
            var path = Path.Combine(root, "mapbox", _tilesetId, z.ToString(), x.ToString(), $"{y}.{_formatExtension}");
            if (File.Exists(path))
            {
                return await File.ReadAllBytesAsync(path, ct);
            }

            var bytes = await _http.GetByteArrayAsync(url, ct);
            if (_persistTileCache)
            {
                try
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                    await File.WriteAllBytesAsync(path, bytes, ct);
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Failed to persist tile cache at {Path}", path);
                }
            }
            return bytes;
        }

        return await _http.GetByteArrayAsync(url, ct);
    }

    private IEnumerable<GeoFeature> DecodeLayerFeatures(byte[] pbf, string layerName, FeatureKind kind, int z, int x, int y, CancellationToken ct)
    {
        // NetTopologySuite vector tile reader returns tile-local coordinates (0..extent).
        using var ms = new MemoryStream(pbf, writable: false);

        var reader = new MapboxTileReader();
        var tileDef = new NetTopologySuite.IO.VectorTiles.Tiles.Tile(x, y, z);
        var vectorTile = reader.Read(ms, tileDef);

        var layer = vectorTile.Layers.FirstOrDefault(l => string.Equals(l.Name, layerName, StringComparison.OrdinalIgnoreCase));
        if (layer == null) yield break;

        // Mapbox tiles commonly use extent=4096; NTS doesn't always expose extent per layer.
        const int extent = 4096;

        foreach (var feat in layer.Features)
        {
            ct.ThrowIfCancellationRequested();
            if (feat?.Geometry is null) continue;

            if (feat.Geometry is Polygon p)
            {
                foreach (var f in ConvertPolygonFeature(p, feat, kind, extent, z, x, y))
                    yield return f;
            }
            else if (feat.Geometry is MultiPolygon mp)
            {
                for (var i = 0; i < mp.NumGeometries; i++)
                {
                    var g = mp.GetGeometryN(i) as Polygon;
                    if (g == null) continue;
                    foreach (var f in ConvertPolygonFeature(g, feat, kind, extent, z, x, y))
                        yield return f;
                }
            }
            else if (feat.Geometry is LineString ls)
            {
                foreach (var f in ConvertLineStringFeature(ls, feat, kind, extent, z, x, y))
                    yield return f;
            }
            else if (feat.Geometry is MultiLineString mls)
            {
                for (var i = 0; i < mls.NumGeometries; i++)
                {
                    var g = mls.GetGeometryN(i) as LineString;
                    if (g == null) continue;
                    foreach (var f in ConvertLineStringFeature(g, feat, kind, extent, z, x, y))
                        yield return f;
                }
            }
        }
    }

    private IEnumerable<GeoFeature> ConvertPolygonFeature(Polygon poly, IFeature feat, FeatureKind kind, int extent, int z, int x, int y)
    {
        var coords = poly.ExteriorRing?.Coordinates;
        if (coords == null || coords.Length < 3) yield break;

        var geoRing = new List<GeoPoint>(coords.Length);
        for (var i = 0; i < coords.Length; i++)
        {
            var c = coords[i];
            var gp = TilePointToGeo(z, x, y, extent, (int)Math.Round(c.X), (int)Math.Round(c.Y));
            geoRing.Add(gp);
        }

        if (geoRing.Count < 3) yield break;
        if (geoRing[0].Latitude.Degrees != geoRing[^1].Latitude.Degrees || geoRing[0].Longitude.Degrees != geoRing[^1].Longitude.Degrees)
        {
            geoRing.Add(geoRing[0]);
        }

        var props = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        foreach (var name in feat.Attributes.GetNames())
        {
            props[name] = feat.Attributes[name] ?? "";
        }
        props["provider"] = "mapbox";
        props["tileset"] = _tilesetId;
        props["zxy"] = $"{z}/{x}/{y}";

        var id = props.TryGetValue("id", out var idObj) ? Convert.ToString(idObj, CultureInfo.InvariantCulture) : Guid.NewGuid().ToString("N");
        yield return new GeoFeature(
            id: $"{z}/{x}/{y}:{id}",
            kind: kind,
            geometry: new GeoPolygon(geoRing),
            properties: props);
    }

    private IEnumerable<GeoFeature> ConvertLineStringFeature(LineString line, IFeature feat, FeatureKind kind, int extent, int z, int x, int y)
    {
        var coords = line.Coordinates;
        if (coords == null || coords.Length < 2) yield break;

        var geoPts = new List<GeoPoint>(coords.Length);
        for (var i = 0; i < coords.Length; i++)
        {
            var c = coords[i];
            geoPts.Add(TilePointToGeo(z, x, y, extent, (int)Math.Round(c.X), (int)Math.Round(c.Y)));
        }

        var props = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        foreach (var name in feat.Attributes.GetNames())
        {
            props[name] = feat.Attributes[name] ?? "";
        }
        props["provider"] = "mapbox";
        props["tileset"] = _tilesetId;
        props["zxy"] = $"{z}/{x}/{y}";

        var id = props.TryGetValue("id", out var idObj) ? Convert.ToString(idObj, CultureInfo.InvariantCulture) : Guid.NewGuid().ToString("N");
        yield return new GeoFeature(
            id: $"{z}/{x}/{y}:{id}",
            kind: kind,
            geometry: new GeoPolyline(geoPts),
            properties: props);
    }

    private static string ResolveLayerName(FeatureKind kind)
    {
        // Mapbox Streets v8 commonly uses these layer names.
        if (kind.Equals(FeatureKind.Building)) return "building";
        if (kind.Equals(FeatureKind.Road)) return "road";
        if (kind.Equals(FeatureKind.Water)) return "water";
        if (kind.Equals(FeatureKind.Vegetation)) return "landuse"; // coarse fallback
        return kind.Value;
    }

    private static string BuildTileUrl(string tilesetId, int z, int x, int y, string ext, string token)
    {
        // Mapbox Vector Tiles API (v4):
        // https://api.mapbox.com/v4/{tileset}/{z}/{x}/{y}.mvt?access_token=...
        return $"https://api.mapbox.com/v4/{tilesetId}/{z}/{x}/{y}.{ext}?access_token={WebUtility.UrlEncode(token)}";
    }

    private static List<TileCoord> ComputeTiles(GeoBounds bounds, int z)
    {
        var (x0, y0) = LonLatToTile(bounds.MinLongitude.Degrees, bounds.MaxLatitude.Degrees, z);
        var (x1, y1) = LonLatToTile(bounds.MaxLongitude.Degrees, bounds.MinLatitude.Degrees, z);

        var minX = Math.Min(x0, x1);
        var maxX = Math.Max(x0, x1);
        var minY = Math.Min(y0, y1);
        var maxY = Math.Max(y0, y1);

        var tiles = new List<TileCoord>((maxX - minX + 1) * (maxY - minY + 1));
        for (var x = minX; x <= maxX; x++)
        {
            for (var y = minY; y <= maxY; y++)
            {
                tiles.Add(new TileCoord(z, x, y));
            }
        }
        return tiles;
    }

    private static (int x, int y) LonLatToTile(double lonDeg, double latDeg, int z)
    {
        latDeg = Math.Max(-85.05112878, Math.Min(85.05112878, latDeg));
        var n = Math.Pow(2.0, z);
        var x = (int)Math.Floor((lonDeg + 180.0) / 360.0 * n);

        var latRad = latDeg * Math.PI / 180.0;
        var y = (int)Math.Floor((1.0 - Math.Log(Math.Tan(latRad) + 1.0 / Math.Cos(latRad)) / Math.PI) / 2.0 * n);
        return (x, y);
    }

    private static GeoPoint TilePointToGeo(int z, int x, int y, int extent, int px, int py)
    {
        var n = Math.Pow(2.0, z);
        var gx = (x + (px / (double)extent)) / n;
        var gy = (y + (py / (double)extent)) / n;

        var lon = gx * 360.0 - 180.0;
        var latRad = Math.Atan(Math.Sinh(Math.PI * (1.0 - 2.0 * gy)));
        var lat = latRad * 180.0 / Math.PI;

        return new GeoPoint { Latitude = new Latitude(lat), Longitude = new Longitude(lon) };
    }

    private readonly record struct TileCoord(int Z, int X, int Y);
}

