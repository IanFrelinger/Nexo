# Geospatial User Guide

This guide provides practical examples and best practices for using Nexo's geospatial features (GeoTerrain, GeoVector, GeoWorld).

## Table of Contents

1. [Quick Start](#quick-start)
2. [Elevation Data Sources](#elevation-data-sources)
3. [Vector Data Sources](#vector-data-sources)
4. [World Generation](#world-generation)
5. [Data Format Support](#data-format-support)
6. [Best Practices](#best-practices)
7. [Troubleshooting](#troubleshooting)

## Quick Start

### Generate Terrain from SRTM Data

```bash
# Download SRTM tile and generate mesh
nexo geoterrain tile-to-mesh --tile N37W122 --output terrain.obj

# Generate terrain for a bounding box
nexo geoterrain bounds-to-mesh \
  --bounds "37.0,-122.0,38.0,-121.0" \
  --output terrain.obj
```

### Generate Vector Features

```bash
# Extract buildings from OSM PBF
nexo geovector extract \
  --provider osm \
  --input data.osm.pbf \
  --bounds "37.0,-122.0,38.0,-121.0" \
  --kind building \
  --output buildings.json

# Extract roads from Mapbox (requires token)
nexo geovector extract \
  --provider mapbox \
  --mapbox-token YOUR_TOKEN \
  --bounds "37.0,-122.0,38.0,-121.0" \
  --kind road \
  --output roads.json
```

### Generate Complete World

```bash
# Generate complete world bundle (terrain + vectors)
nexo world generate \
  --bounds "37.0,-122.0,38.0,-121.0" \
  --elevation-provider hybrid \
  --elevation-local-root ./srtm \
  --vector-provider osm \
  --vector-input data.osm.pbf \
  --output ./world_bundle
```

## Elevation Data Sources

### Supported Formats

- **SRTM HGT** (`.hgt` files) - NASA Shuttle Radar Topography Mission data
- **GeoTIFF** (`.tif`, `.tiff`) - Standard GIS raster format
- **ASCII Grid** (`.asc`, `.txt`) - ESRI ASCII Grid format
- **Mapbox Terrain-RGB** - Online raster tiles (requires token)

### Local File Provider

```bash
# Use local SRTM files
nexo geoterrain bounds-to-mesh \
  --provider local \
  --local-root ./srtm_data \
  --bounds "37.0,-122.0,38.0,-121.0" \
  --output terrain.obj
```

### Hybrid Provider (Local + HTTP)

```bash
# Try local first, download if missing
nexo geoterrain bounds-to-mesh \
  --provider hybrid \
  --local-root ./srtm_data \
  --srtm-base-url https://e4ftl01.cr.usgs.gov/MEASURES/SRTMGL1.003/2000.02.11 \
  --persist-downloads \
  --bounds "37.0,-122.0,38.0,-121.0" \
  --output terrain.obj
```

### GeoTIFF Support

```bash
# Use GeoTIFF elevation data
nexo geoterrain bounds-to-mesh \
  --provider geotiff \
  --geotiff-path ./elevation.tif \
  --bounds "37.0,-122.0,38.0,-121.0" \
  --output terrain.obj
```

## Vector Data Sources

### Supported Formats

- **OSM PBF** (`.osm.pbf`) - OpenStreetMap Protocol Buffer format
- **GeoJSON** (`.geojson`, `.json`) - Standard JSON-based format
- **Shapefile** (`.shp`) - ESRI Shapefile format
- **Mapbox Vector Tiles** - Online vector tiles (requires token)

### OSM PBF Provider

```bash
# Extract features from OSM PBF
nexo geovector extract \
  --provider osm \
  --input ./california.osm.pbf \
  --bounds "37.0,-122.0,38.0,-121.0" \
  --kind building \
  --output buildings.json
```

### GeoJSON Provider

```bash
# Use GeoJSON file
nexo geovector extract \
  --provider geojson \
  --input ./features.geojson \
  --bounds "37.0,-122.0,38.0,-121.0" \
  --kind building \
  --output buildings.json
```

### Shapefile Provider

```bash
# Use Shapefile (requires .shp, .shx, .dbf files in same directory)
nexo geovector extract \
  --provider shapefile \
  --input ./buildings.shp \
  --bounds "37.0,-122.0,38.0,-121.0" \
  --kind building \
  --output buildings.json
```

### Mapbox Vector Tiles

```bash
# Use Mapbox online tiles
nexo geovector extract \
  --provider mapbox \
  --mapbox-token YOUR_TOKEN \
  --bounds "37.0,-122.0,38.0,-121.0" \
  --kind building \
  --output buildings.json
```

## World Generation

### Complete World Bundle

```bash
nexo world generate \
  --bounds "37.0,-122.0,38.0,-121.0" \
  --elevation-provider hybrid \
  --elevation-local-root ./srtm \
  --vector-provider osm \
  --vector-input ./california.osm.pbf \
  --output ./world_bundle
```

This generates:
- `terrain_chunks/` - Chunked terrain meshes
- `buildings.obj` - Building meshes
- `roads.obj` - Road meshes
- `water.obj` - Water surface meshes
- `instances.json` - Vegetation instances
- `materials.json` - Material assignments
- `manifest.json` - World bundle manifest

### Export Formats

#### OBJ Format
```bash
nexo world generate --output ./world --format obj
```

#### glTF/GLB Format
```bash
nexo world generate --output ./world --format gltf
nexo world generate --output ./world --format glb
```

### Mesh Quality Validation

```bash
# Validate mesh quality
nexo geoterrain validate-mesh \
  --input terrain.obj \
  --grid-path elevation.tif
```

Reports:
- Triangle quality metrics (aspect ratio, area distribution)
- Mesh accuracy (deviation from source grid)
- Slope validation
- Corruption detection

## Data Format Support

### Elevation Formats

| Format | Extension | Provider | Notes |
|--------|-----------|----------|-------|
| SRTM HGT | `.hgt` | `local`, `http`, `hybrid` | NASA SRTM data |
| GeoTIFF | `.tif`, `.tiff` | `geotiff` | Standard GIS format |
| ASCII Grid | `.asc`, `.txt` | `ascii-grid` | ESRI ASCII Grid |
| Mapbox Terrain-RGB | - | `mapbox-terrain-rgb` | Online tiles |

### Vector Formats

| Format | Extension | Provider | Notes |
|--------|-----------|----------|-------|
| OSM PBF | `.osm.pbf` | `osm` | OpenStreetMap format |
| GeoJSON | `.geojson`, `.json` | `geojson` | Standard JSON format |
| Shapefile | `.shp` | `shapefile` | ESRI Shapefile |
| Mapbox Vector Tiles | `.mvt` | `mapbox` | Online tiles |

### Export Formats

| Format | Extension | Use Case |
|--------|-----------|----------|
| OBJ | `.obj` | Universal 3D format |
| glTF 2.0 | `.gltf` | Modern web/Unity |
| GLB | `.glb` | Binary glTF |

## Best Practices

### 1. Use Hybrid Providers for Production

Always use hybrid providers that try local files first, then download if missing:

```bash
--elevation-provider hybrid \
--elevation-local-root ./cache \
--persist-downloads
```

This provides:
- Fast local access
- Automatic fallback to network
- Caching for future runs

### 2. Validate Data Integrity

Enable data integrity checks:

```bash
nexo geoterrain bounds-to-mesh \
  --validate-integrity \
  --bounds "..." \
  --output terrain.obj
```

### 3. Use Spatial Indexing for Large Datasets

For large vector datasets, spatial indexing improves performance:

```csharp
var quadtree = new Quadtree(bounds, maxDepth: 10, maxItemsPerNode: 10);
quadtree.InsertRange(features);
var results = quadtree.Query(queryBounds);
```

### 4. Handle Partial Failures Gracefully

Providers now report partial successes when some tiles fail:

```csharp
// MapboxVectorTileProvider logs partial success
// Example: "Partial success: 8/10 tiles downloaded successfully"
```

### 5. Configure Retry Logic

Network providers include automatic retry with exponential backoff:

```csharp
var retryPolicy = new RetryPolicy(
    strategy: RetryStrategy.ExponentialBackoff,
    maxAttempts: 3,
    initialDelay: TimeSpan.FromSeconds(1),
    maxDelay: TimeSpan.FromMinutes(2));
```

## Troubleshooting

### Issue: "Tile not found" errors

**Solution**: Ensure local files are in the correct format and location:
- SRTM files: `N37W122.hgt` format
- GeoTIFF: Valid GeoTIFF with georeferencing tags
- ASCII Grid: Valid ESRI ASCII Grid format

### Issue: "Network timeout" errors

**Solution**: 
1. Check network connectivity
2. Providers include automatic retry logic
3. Use `--persist-downloads` to cache tiles locally

### Issue: "Corrupted elevation data" warnings

**Solution**: 
1. Re-download the tile
2. Check file integrity with checksum validation
3. Use `--validate-integrity` flag

### Issue: "Partial success" warnings

**Solution**: This is normal for large-area processing. Check logs to see which tiles failed:
- Some tiles may be outside coverage area
- Network issues may cause temporary failures
- Partial data is still usable

### Issue: Poor mesh quality

**Solution**:
1. Check mesh quality report: `nexo geoterrain validate-mesh`
2. Look for thin triangles (high aspect ratio)
3. Consider using LOD generation for better quality
4. Check source elevation data quality

## Configuration

### Mapbox Token Setup

Set environment variable:
```bash
export MAPBOX_ACCESS_TOKEN=your_token_here
```

Or pass via CLI:
```bash
--mapbox-token your_token_here
```

### Air-Gapped Mode

For offline environments:
```bash
nexo world generate \
  --air-gapped \
  --elevation-provider local \
  --vector-provider osm \
  --vector-input ./data.osm.pbf \
  --output ./world
```

## API Reference

For detailed API documentation, see [API_REFERENCE.md](./API_REFERENCE.md).

## Examples

See the `examples/` directory for complete working examples.
