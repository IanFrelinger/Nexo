# Geospatial User Guide

This guide provides practical instructions for using Nexo's geospatial features (GeoTerrain, GeoVector, and GeoWorld).

## Table of Contents

1. [Quick Start](#quick-start)
2. [Elevation Data](#elevation-data)
3. [Vector Data](#vector-data)
4. [World Generation](#world-generation)
5. [Configuration](#configuration)
6. [Troubleshooting](#troubleshooting)

## Quick Start

### Generate a Simple Terrain Mesh

```bash
# Download SRTM tile and generate OBJ mesh
nexo geoterrain tile-to-mesh --tile-id "N37W122" --output terrain.obj

# Generate mesh from bounds with Mapbox Terrain-RGB
nexo geoterrain bounds-to-mesh \
  --bounds "37.7749,37.8049,-122.4194,-122.3894" \
  --provider mapbox-terrain-rgb \
  --mapbox-token $MAPBOX_ACCESS_TOKEN \
  --output terrain.obj
```

### Extract Vector Features

```bash
# Extract buildings from OSM PBF
nexo geovector extract \
  --bounds "37.7749,37.8049,-122.4194,-122.3894" \
  --provider osm \
  --osm-pbf path/to/data.osm.pbf \
  --kind building \
  --output buildings.json

# Extract from GeoJSON file
nexo geovector extract \
  --bounds "37.7749,37.8049,-122.4194,-122.3894" \
  --provider geojson \
  --geojson-path path/to/buildings.geojson \
  --kind building \
  --output buildings.json
```

### Generate Complete World Bundle

```bash
nexo world generate \
  --bounds "37.7749,37.8049,-122.4194,-122.3894" \
  --output-dir ./world_output \
  --elevation-provider mapbox-terrain-rgb \
  --vector-provider hybrid \
  --mapbox-token $MAPBOX_ACCESS_TOKEN \
  --osm-pbf path/to/data.osm.pbf
```

## Elevation Data

### Supported Formats

- **SRTM HGT** (`.hgt`) - NASA Shuttle Radar Topography Mission data
- **Mapbox Terrain-RGB** - Raster tiles from Mapbox
- **GeoTIFF** (`.tif`, `.tiff`) - Standard GIS format
- **ASCII Grid** (`.asc`, `.txt`) - ESRI ASCII Grid format

### Local File Providers

For air-gapped environments, use local file providers:

```bash
# Using local SRTM HGT files
nexo geoterrain tile-to-mesh \
  --tile-id "N37W122" \
  --provider local \
  --local-root /path/to/srtm/tiles \
  --output terrain.obj

# Using GeoTIFF
nexo geoterrain bounds-to-mesh \
  --bounds "37.7749,37.8049,-122.4194,-122.3894" \
  --provider geotiff \
  --geotiff-path /path/to/elevation.tif \
  --output terrain.obj
```

### Mapbox Configuration

1. Get a Mapbox access token from [mapbox.com](https://www.mapbox.com)
2. Set environment variable: `export MAPBOX_ACCESS_TOKEN=your_token`
3. Or pass via CLI: `--mapbox-token your_token`

## Vector Data

### Supported Formats

- **OSM PBF** (`.osm.pbf`) - OpenStreetMap Protocol Buffer format
- **Mapbox Vector Tiles** (MVT) - Online tile service
- **GeoJSON** (`.geojson`) - JSON-based geospatial format
- **Shapefile** (`.shp`) - ESRI Shapefile format (with `.dbf` and `.shx`)

### Provider Options

- `echo` - Generates synthetic test data
- `osm` - Reads from OSM PBF file
- `mapbox` - Downloads from Mapbox Vector Tiles API
- `geojson` - Reads from GeoJSON file
- `shapefile` - Reads from Shapefile
- `hybrid` - Tries local (OSM) first, falls back to Mapbox

### Feature Kinds

- `building` - Building polygons
- `road` - Road networks (LineStrings)
- `water` - Water bodies (polygons)
- `vegetation` - Vegetation areas

### Example: Extract Buildings from OSM

```bash
nexo geovector extract \
  --bounds "37.7749,37.8049,-122.4194,-122.3894" \
  --provider osm \
  --osm-pbf california-latest.osm.pbf \
  --kind building \
  --output buildings.json
```

### Example: Use GeoJSON

```bash
nexo geovector extract \
  --bounds "37.7749,37.8049,-122.4194,-122.3894" \
  --provider geojson \
  --geojson-path buildings.geojson \
  --kind building \
  --output buildings.json
```

## World Generation

The `nexo world generate` command creates a complete world bundle with terrain, buildings, roads, water, and vegetation.

### Basic Usage

```bash
nexo world generate \
  --bounds "37.7749,37.8049,-122.4194,-122.3894" \
  --output-dir ./world \
  --elevation-provider mapbox-terrain-rgb \
  --vector-provider hybrid \
  --mapbox-token $MAPBOX_ACCESS_TOKEN \
  --osm-pbf data.osm.pbf
```

### Output Structure

```
world/
├── manifest.json          # World bundle metadata
├── materials.json        # Material assignments
├── instances.json        # Vegetation/object instances
├── terrain_chunks/       # Chunked terrain meshes
│   ├── terrain_0_0.obj
│   └── ...
├── buildings.obj         # Building meshes
├── roads.obj            # Road meshes
├── water.obj            # Water meshes
└── unity/               # Unity-specific import instructions
    └── IMPORT_INSTRUCTIONS.txt
```

### Export Formats

- **OBJ** - Text-based 3D format (default)
- **glTF 2.0** - Modern 3D format (`.gltf`)
- **GLB** - Binary glTF (`.glb`)

```bash
# Export as glTF
nexo world generate \
  --bounds "37.7749,37.8049,-122.4194,-122.3894" \
  --output-dir ./world \
  --format gltf \
  ...
```

## Configuration

### Environment Variables

- `MAPBOX_ACCESS_TOKEN` - Mapbox API access token
- `NEXO_CACHE_DIR` - Directory for caching downloaded tiles (optional)

### CLI Options

Common options across geospatial commands:

- `--bounds` - Geographic bounds as "minLat,maxLat,minLon,maxLon"
- `--provider` - Data provider (varies by command)
- `--output` / `--output-dir` - Output file or directory
- `--air-gapped` - Disable network access
- `--verbose` - Enable detailed logging

## Troubleshooting

### Network Issues

If downloads fail:

1. Check your internet connection
2. Verify Mapbox token is valid: `echo $MAPBOX_ACCESS_TOKEN`
3. Use `--air-gapped` mode with local files
4. Check firewall/proxy settings

### File Not Found Errors

- Ensure file paths are absolute or relative to current directory
- For Shapefiles, ensure `.shp`, `.dbf`, and `.shx` files are in same directory
- Check file permissions

### Out of Memory

For large areas:

1. Use chunked exports (automatic for world generation)
2. Reduce bounds size
3. Use lower zoom levels for Mapbox tiles
4. Process in smaller batches

### Invalid Geometry Errors

- Check that input data is valid (use validation tools)
- Ensure bounds are correct (min < max for lat/lon)
- Verify coordinate system matches (WGS84 expected)

### Performance Tips

1. **Use caching**: Downloaded tiles are cached automatically
2. **Spatial indexing**: Large datasets benefit from quadtree indexing (automatic)
3. **Batch processing**: Process multiple regions in parallel
4. **Local files**: Use local files instead of network downloads when possible

## Best Practices

1. **Start Small**: Test with small bounds before processing large areas
2. **Validate Inputs**: Check bounds and file paths before running
3. **Monitor Progress**: Use `--verbose` to see detailed progress
4. **Cache Strategically**: Keep downloaded tiles for reuse
5. **Error Handling**: Check logs for partial failures (some tiles may fail)

## API Reference

For programmatic usage, see [API_REFERENCE.md](./API_REFERENCE.md).

## Examples

See the `examples/` directory for complete working examples.
