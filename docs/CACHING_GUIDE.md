# Tile Caching Guide

Nexo includes comprehensive disk caching functionality to reduce costs by avoiding re-downloading tiles. This guide explains how to configure and use caching.

## Overview

Caching saves downloaded tiles to disk so they can be reused in future operations, eliminating API costs for cached tiles. The system supports:

- **SRTM Elevation Tiles**: Cached to `{cacheRoot}/srtm/{tileId}.hgt`
- **Mapbox Vector Tiles**: Cached to `{cacheRoot}/mapbox/{tileset}/{z}/{x}/{y}.mvt`
- **Mapbox Raster Tiles**: Not yet cached (planned)

## Cache Configuration

### CLI Usage

#### Basic Caching

```bash
# Enable caching with default directory (~/.nexo/cache)
nexo geoterrain bounds-to-mesh \
  --bounds "37.0,-122.0,38.0,-121.0" \
  --elevation-provider http \
  --srtm-base-url https://example.com/srtm/ \
  --cache-root ~/.nexo/cache \
  --persist-cache \
  --output terrain.obj
```

#### Custom Cache Directory

```bash
# Use a custom cache directory
nexo geoterrain bounds-to-mesh \
  --bounds "37.0,-122.0,38.0,-121.0" \
  --elevation-provider http \
  --srtm-base-url https://example.com/srtm/ \
  --cache-root /data/nexo-cache \
  --persist-cache \
  --output terrain.obj
```

#### Disable Caching

```bash
# Disable disk caching (only in-memory cache)
nexo geoterrain bounds-to-mesh \
  --bounds "37.0,-122.0,38.0,-121.0" \
  --elevation-provider http \
  --srtm-base-url https://example.com/srtm/ \
  --cache-root ~/.nexo/cache \
  --persist-cache false \
  --output terrain.obj
```

#### Vector Tile Caching

```bash
# Cache Mapbox vector tiles
nexo geovector buildings-to-obj \
  --bounds "37.0,-122.0,38.0,-121.0" \
  --vector-provider mapbox \
  --mapbox-token YOUR_TOKEN \
  --cache-root ~/.nexo/cache \
  --persist-cache \
  --output buildings.obj
```

### API Usage

#### Request with Caching

```json
POST /api/v1/geoterrain/generate
{
  "bounds": "37.0,-122.0,38.0,-121.0",
  "elevationProvider": "http",
  "format": "obj",
  "cacheRoot": "/data/nexo-cache",
  "persistCache": true
}
```

#### Vector Extraction with Caching

```json
POST /api/v1/geovector/extract
{
  "bounds": "37.0,-122.0,38.0,-121.0",
  "vectorProvider": "mapbox",
  "featureKind": "building",
  "mapboxToken": "YOUR_TOKEN",
  "cacheRoot": "/data/nexo-cache",
  "persistCache": true
}
```

### SDK Usage

```csharp
using Nexo.SDK;
using Nexo.SDK.Estimation;

// Create elevation provider with caching
var cacheRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".nexo", "cache");
var elevationProvider = new SrtmHttpElevationProvider(
    httpClient,
    "https://example.com/srtm/",
    logger,
    cacheRoot: cacheRoot,
    persistCache: true);

var client = new GeoTerrainClient(elevationProvider, logger, estimator);

// First run: downloads tiles and caches them
var result1 = await client.GenerateMeshWithEstimationAsync(bounds);
Console.WriteLine($"Cost: ${result1.Actual?.CostUsd:F4}"); // Shows download cost

// Second run: uses cached tiles (zero cost)
var result2 = await client.GenerateMeshWithEstimationAsync(bounds);
Console.WriteLine($"Cost: ${result2.Actual?.CostUsd:F4}"); // Shows $0.0000 (cached)
```

## Cache Directory Structure

```
{cacheRoot}/
├── srtm/
│   ├── N37W122.hgt
│   ├── N37W121.hgt
│   └── ...
└── mapbox/
    └── mapbox.mapbox-streets-v8/
        ├── 15/
        │   ├── 5234/
        │   │   ├── 12663.mvt
        │   │   └── 12664.mvt
        │   └── ...
        └── ...
```

## Cost Savings

### Example: SRTM Tiles

**Without Caching:**
- 10 tiles × 2.9 MB = 29 MB
- Cost: ~$0.0003 per download
- 100 operations = $0.03

**With Caching:**
- First operation: $0.0003 (downloads)
- Subsequent 99 operations: $0.0000 (cached)
- Total: $0.0003 (99% savings)

### Example: Mapbox Vector Tiles

**Without Caching:**
- 50 tiles × $0.0005 = $0.025 per operation
- 100 operations = $2.50

**With Caching:**
- First operation: $0.025 (downloads)
- Subsequent 99 operations: $0.0000 (cached)
- Total: $0.025 (99% savings)

## Resource Estimation with Caching

The resource estimator automatically detects cached tiles and reports zero cost:

```csharp
var estimator = new ResourceEstimationService();
var bounds = GeoBounds.Parse("37.0,-122.0,38.0,-121.0");
var tileIds = SrtmTileCoverage.TilesCovering(bounds);

// Check cache before estimating
var estimate = estimator.EstimateSrtmTileDownload(tileIds, fromCache: false, cacheRoot: "/data/cache");

Console.WriteLine($"Cached: {estimate.Metadata?["cached-count"]}");
Console.WriteLine($"To download: {estimate.Metadata?["download-count"]}");
Console.WriteLine($"Estimated cost: ${estimate.CostUsd:F4}");
```

## Cache Management

### Cache Size

Cache directories can grow large:
- **SRTM tiles**: ~2.9 MB each
- **Mapbox tiles**: ~50 KB each (varies by zoom)

For a typical region:
- 100 SRTM tiles = ~290 MB
- 1000 Mapbox tiles = ~50 MB

### Cache Cleanup

```bash
# Manual cleanup
rm -rf ~/.nexo/cache/srtm/*.hgt
rm -rf ~/.nexo/cache/mapbox/*

# Or use a cache size limit (not yet implemented)
```

### Cache Sharing

Cache directories can be shared across:
- Multiple CLI invocations
- API service instances
- SDK clients
- Different projects

Simply point all instances to the same `--cache-root` directory.

## Best Practices

1. **Use a persistent cache directory**: Set `--cache-root` to a location that persists across sessions
2. **Enable by default**: Use `--persist-cache` (default: true) to save costs
3. **Monitor cache size**: Regularly check cache directory size
4. **Share caches**: Use the same cache directory for all operations in a region
5. **Backup important caches**: For air-gapped deployments, backup cache directories

## Troubleshooting

### Cache Not Working

**Problem**: Tiles are still being downloaded despite cache directory set.

**Solutions**:
1. Check cache directory permissions: `ls -la {cacheRoot}`
2. Verify cache path: Ensure `--cache-root` is set correctly
3. Check logs: Look for "Loading SRTM tile from cache" messages

### Cache Directory Full

**Problem**: Disk space exhausted.

**Solutions**:
1. Clean old tiles: `rm -rf {cacheRoot}/srtm/*`
2. Use a different cache directory with more space
3. Implement cache size limits (future feature)

### Cache Corruption

**Problem**: Cached tiles are corrupted.

**Solutions**:
1. Delete corrupted tiles: `rm {cacheRoot}/srtm/{tileId}.hgt`
2. Re-download: Run operation again (will re-download missing tiles)
3. Validate cache: Use integrity validation (future feature)

## Configuration Reference

### CLI Options

| Option | Description | Default |
|--------|-------------|---------|
| `--cache-root` | Directory root for tile cache | None (disabled) |
| `--persist-cache` | Enable disk caching | `true` |
| `--terrain-cache-root` | Terrain-specific cache directory | Same as `--cache-root` |
| `--terrain-persist-cache` | Enable terrain disk caching | `true` |

### API Request Properties

| Property | Type | Description | Default |
|----------|------|-------------|---------|
| `cacheRoot` | string? | Directory root for tile cache | null |
| `persistCache` | bool? | Enable disk caching | true |

### SDK Constructor Parameters

| Parameter | Type | Description | Default |
|-----------|------|-------------|---------|
| `cacheRoot` | string? | Directory root for tile cache | null |
| `persistCache` | bool | Enable disk caching | true |

## Implementation Details

### SRTM Caching

- **Location**: `{cacheRoot}/srtm/{tileId}.hgt`
- **Format**: Raw HGT files (uncompressed)
- **Size**: ~2.9 MB per tile
- **Check**: Before downloading, checks if file exists
- **Save**: After downloading, saves to cache directory

### Mapbox Vector Tile Caching

- **Location**: `{cacheRoot}/mapbox/{tileset}/{z}/{x}/{y}.mvt`
- **Format**: Mapbox Vector Tile (MVT) format
- **Size**: ~50 KB per tile (varies)
- **Check**: Before downloading, checks if file exists
- **Save**: After downloading, saves to cache directory

### Cache Detection in Resource Estimation

The resource estimator checks the cache directory before estimating costs:

```csharp
// Checks cache directory for existing tiles
var estimate = estimator.EstimateSrtmTileDownload(tileIds, cacheRoot: "/data/cache");

// Returns metadata with cached vs download counts
var cachedCount = estimate.Metadata?["cached-count"]; // e.g., 5
var downloadCount = estimate.Metadata?["download-count"]; // e.g., 3
```

## Future Enhancements

- [ ] Cache size limits and eviction policies
- [ ] Cache validation and integrity checks
- [ ] Cache statistics and monitoring
- [ ] Cache compression (gzip)
- [ ] Cache expiration (TTL)
- [ ] Distributed cache support
