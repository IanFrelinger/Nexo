// Forge map host sample entry point.
using System.Net.Http.Json;
using System.Text.Json;
using Nexo.Commercial.GameDomain.Mapping;

var baseUrl = (Environment.GetEnvironmentVariable("NEXO_API_BASE_URL") ?? "http://127.0.0.1:8080").TrimEnd('/');
var engineId = Environment.GetEnvironmentVariable("FORGE_ENGINE_ID") ?? "unity";
var mapboxToken = Environment.GetEnvironmentVariable("MAPBOX_ACCESS_TOKEN");
var tileset = Environment.GetEnvironmentVariable("MAPBOX_TILESET_ID") ?? "mapbox.mapbox-streets-v8";
var aestheticId = Environment.GetEnvironmentVariable("FORGE_AESTHETIC_ID") ?? "voxel";

Console.WriteLine("=== Nexo Forge host sample (terrain parity + material model option + engine packages) ===");
Console.WriteLine($"API base: {baseUrl}");
Console.WriteLine($"Engine id: {engineId}");
Console.WriteLine($"Aesthetic id (cache key): {aestheticId}");

using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(120) };

await RunManifestStep(http, baseUrl, engineId).ConfigureAwait(false);
await RunPyramidStep(http, baseUrl).ConfigureAwait(false);
await RunMaterialHintsStep(http, baseUrl).ConfigureAwait(false);

if (!string.IsNullOrWhiteSpace(mapboxToken))
{
    var lat = ParseDoubleEnv("SAMPLE_LAT", 37.7749);
    var lon = ParseDoubleEnv("SAMPLE_LON", -122.4194);
    var zoom = ParseIntEnv("SAMPLE_ZOOM", 14);
    var (x, y) = WebMercatorTileMath.LonLatToTileXY(lon, lat, zoom);
    var vectorUrl = VectorTileUrlBuilder.MapboxVectorTileUrl(tileset, zoom, x, y, mapboxToken);
    var terrainUrl = TerrainTileUrlBuilder.MapboxTerrainRgbTileUrl(zoom, x, y, mapboxToken);

    Console.WriteLine();
    Console.WriteLine("=== Tile orchestration (M2) ===");
    Console.WriteLine($"Sample lon/lat/zoom: {lon}, {lat}, z{zoom} → tile {zoom}/{x}/{y}");
    Console.WriteLine($"Vector URL (token redacted): https://api.mapbox.com/v4/{tileset}/{zoom}/{x}/{y}.vector.pbf?access_token=<secret>");
    Console.WriteLine($"Terrain-RGB URL (token redacted): https://api.mapbox.com/v4/mapbox.terrain-rgb/{zoom}/{x}/{y}.pngraw?access_token=<secret>");

    var cacheDir = Environment.GetEnvironmentVariable("NEXO_TILE_CACHE_DIR");
    await RunTileCacheStep(http, cacheDir, aestheticId, "mapbox", zoom, x, y, vectorUrl).ConfigureAwait(false);

    await RunPipelineStep(http, baseUrl, vectorUrl, terrainUrl, zoom, x, y).ConfigureAwait(false);

    Console.WriteLine();
    Console.WriteLine("=== Import hints (M3) ===");
    foreach (var line in MapHostImportHints.FromParseSummary(new VectorMapParseSummary("mvt",
               "placeholder", [])))
        Console.WriteLine($"  • {line}");
}
else
{
    Console.WriteLine();
    Console.WriteLine("Set MAPBOX_ACCESS_TOKEN to exercise tile cache, URL build, and pipeline run.");
}

Console.WriteLine();
Console.WriteLine("Engine bridge: docs/engine-bridge/README.md (Unity UPM + Godot addon folders).");
Console.WriteLine("Done.");
return;

static double ParseDoubleEnv(string name, double fallback)
{
    var s = Environment.GetEnvironmentVariable(name);
    return double.TryParse(s, System.Globalization.NumberStyles.Float,
        System.Globalization.CultureInfo.InvariantCulture, out var v)
        ? v
        : fallback;
}

static int ParseIntEnv(string name, int fallback)
{
    var s = Environment.GetEnvironmentVariable(name);
    return int.TryParse(s, System.Globalization.NumberStyles.Integer,
        System.Globalization.CultureInfo.InvariantCulture, out var v)
        ? v
        : fallback;
}

static async Task RunTileCacheStep(
    HttpClient http,
    string? cacheDir,
    string aestheticId,
    string providerId,
    int z,
    int x,
    int y,
    string vectorUrl)
{
    if (string.IsNullOrWhiteSpace(cacheDir))
    {
        Console.WriteLine();
        Console.WriteLine("(Tile cache) Set NEXO_TILE_CACHE_DIR to demo MapTileDiskCache.");
        return;
    }

    Console.WriteLine();
    Console.WriteLine("=== Tile disk cache (phase B) ===");
    var cache = new MapTileDiskCache(Path.GetFullPath(cacheDir.Trim()));
    var key = MapTileCacheKey.Create(aestheticId, providerId, z, x, y);
    var cached = await cache.TryReadAsync(key);
    if (cached is not null)
    {
        Console.WriteLine($"Cache hit: {cache.GetFullPath(key)} ({cached.Length} bytes)");
        return;
    }

    Console.WriteLine($"Cache miss → GET tile ({cache.GetFullPath(key)})");
    try
    {
        var bytes = await http.GetByteArrayAsync(vectorUrl);
        await cache.WriteAsync(key, bytes);
        Console.WriteLine($"Stored {bytes.Length} bytes.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Cache fetch failed: {ex.Message}");
    }
}

static async Task RunMaterialHintsStep(HttpClient http, string baseUrl)
{
    Console.WriteLine();
    Console.WriteLine("=== Material hints (phase A / M6) ===");
    var url = $"{baseUrl}/api/forge/map/material-hints?parseKind=mvt";
    Console.WriteLine($"GET {url}");
    try
    {
        using var resp = await http.GetAsync(url).ConfigureAwait(false);
        var body = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
        if (!resp.IsSuccessStatusCode)
        {
            Console.WriteLine($"HTTP {(int)resp.StatusCode}: {body}");
            return;
        }

        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;
        Console.WriteLine($"  summary={root.GetProperty("summary").GetString()}");
        var hints = root.GetProperty("hints");
        Console.WriteLine($"  hints={hints.GetArrayLength()}");
    }
    catch (HttpRequestException ex)
    {
        Console.WriteLine($"Request failed: {ex.Message}");
    }
}

static async Task RunPyramidStep(HttpClient http, string baseUrl)
{
    Console.WriteLine();
    Console.WriteLine("=== LOD tile pyramid (M4) ===");
    var finest = ParseIntEnv("PYRAMID_FINEST_ZOOM", 14);
    var url = $"{baseUrl}/api/forge/map/tile-pyramid?finestZoom={finest}";
    Console.WriteLine($"GET {url}");
    try
    {
        using var resp = await http.GetAsync(url).ConfigureAwait(false);
        var body = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
        if (!resp.IsSuccessStatusCode)
        {
            Console.WriteLine($"HTTP {(int)resp.StatusCode}: {body}");
            return;
        }

        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;
        var fz = root.GetProperty("finestZoom").GetInt32();
        var tiers = root.GetProperty("tiers");
        Console.WriteLine($"  finestZoom={fz}, tiers={tiers.GetArrayLength()}");
        foreach (var t in tiers.EnumerateArray())
        {
            var lod = t.GetProperty("lodLevel").GetInt32();
            var z = t.GetProperty("zoom").GetInt32();
            Console.WriteLine($"  LOD {lod}: zoom={z}, detailFactor={t.GetProperty("detailFactor").GetDouble()}");
        }
    }
    catch (HttpRequestException ex)
    {
        Console.WriteLine($"Request failed: {ex.Message}");
    }
}

static async Task RunManifestStep(HttpClient http, string baseUrl, string engineId)
{
    Console.WriteLine();
    Console.WriteLine("=== Engine aesthetic manifest (M1) ===");
    var url = $"{baseUrl}/api/forge/engine/{Uri.EscapeDataString(engineId)}/aesthetic-manifest";
    Console.WriteLine($"GET {url}");

    try
    {
        using var resp = await http.GetAsync(url).ConfigureAwait(false);
        var body = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
        if (!resp.IsSuccessStatusCode)
        {
            Console.WriteLine($"HTTP {(int)resp.StatusCode}: {body}");
            return;
        }

        using var outer = JsonDocument.Parse(body);
        if (!outer.RootElement.TryGetProperty("json", out var jsonProp))
        {
            Console.WriteLine("Unexpected body (missing 'json' property). Raw:");
            Console.WriteLine(body);
            return;
        }

        var inner = jsonProp.GetString();
        if (string.IsNullOrEmpty(inner))
        {
            Console.WriteLine("Empty manifest JSON string.");
            return;
        }

        using var manifest = JsonDocument.Parse(inner);
        var root = manifest.RootElement;
        var packId = root.TryGetProperty("aestheticPackId", out var p) ? p.GetString() : "?";
        var profile = root.TryGetProperty("mapRenderingProfile", out var m) ? m.GetString() : "?";
        var geom = root.TryGetProperty("geometryStrategy", out var g) ? g.GetString() : "?";
        var bindings = root.TryGetProperty("surfaceBindings", out var b) && b.ValueKind == JsonValueKind.Array
            ? b.GetArrayLength()
            : 0;

        Console.WriteLine($"  aestheticPackId={packId}");
        Console.WriteLine($"  mapRenderingProfile={profile}");
        Console.WriteLine($"  geometryStrategy={geom}");
        Console.WriteLine($"  surfaceBindings count={bindings}");
    }
    catch (HttpRequestException ex)
    {
        Console.WriteLine($"Request failed (is Nexo.API running?): {ex.Message}");
    }
}

static async Task RunPipelineStep(HttpClient http, string baseUrl, string vectorUrl, string terrainUrl, int z, int x, int y)
{
    Console.WriteLine();
    Console.WriteLine("=== Map pipeline run (live fetch: vector + terrain) ===");
    var url = $"{baseUrl}/api/forge/map/pipeline/run";
    var payload = new
    {
        dryRun = false,
        timeoutMs = 60_000,
        vectorDataUrl = vectorUrl,
        terrainDataUrl = terrainUrl,
        mvtTileZoom = z,
        mvtTileX = x,
        mvtTileY = y
    };

    Console.WriteLine($"POST {url}");
    try
    {
        using var resp = await http.PostAsJsonAsync(url, payload).ConfigureAwait(false);
        var body = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
        Console.WriteLine(resp.IsSuccessStatusCode ? "Response:" : $"HTTP {(int)resp.StatusCode}:");
        Console.WriteLine(body);

        if (!resp.IsSuccessStatusCode)
        {
            Console.WriteLine();
            Console.WriteLine("If fetch failed with validation errors, configure Nexo:ForgeSession:AllowedMapFetchHosts");
            Console.WriteLine("to include api.mapbox.com (see appsettings / secrets).");
        }
    }
    catch (HttpRequestException ex)
    {
        Console.WriteLine($"Request failed: {ex.Message}");
    }
}
