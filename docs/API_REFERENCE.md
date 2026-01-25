# Nexo Geospatial API Reference

## Overview

The Nexo Geospatial API provides RESTful endpoints for terrain generation, vector feature extraction, and world bundle creation. This document describes all available endpoints, request/response formats, and usage examples.

**Base URL:** `https://api.nexo.example.com/api/v1`

**API Version:** v1

---

## Authentication

Currently, the API does not require authentication. Future versions will support API keys and OAuth2.

---

## Endpoints

### Terrain Generation

#### POST `/api/v1/geoterrain/generate`

Generate a terrain mesh from elevation data.

**Request Body:**
```json
{
  "bounds": "37.7749,37.8049,-122.4194,-122.3894",
  "elevationProvider": "mapbox-terrain-rgb",
  "format": "obj",
  "mapboxToken": "your_token_here",
  "meshOptions": {
    "treatNoDataAsZero": false,
    "verticalScale": 1.0,
    "generateNormals": true
  }
}
```

**Response:** `202 Accepted`
```json
{
  "jobId": "abc123def456",
  "status": "accepted"
}
```

**Parameters:**
- `bounds` (required): Geographic bounds as "minLat,maxLat,minLon,maxLon"
- `elevationProvider` (optional): Provider name (srtm, mapbox-terrain-rgb, local, geotiff, ascii-grid). Default: "srtm"
- `format` (optional): Output format (obj, gltf, glb, fbx, usd). Default: "obj"
- `mapboxToken` (optional): Mapbox access token (required for mapbox providers)
- `localPath` (optional): Local file path (for local/geotiff/ascii-grid providers)
- `meshOptions` (optional): Mesh generation options

**Example:**
```bash
curl -X POST https://api.nexo.example.com/api/v1/geoterrain/generate \
  -H "Content-Type: application/json" \
  -d '{
    "bounds": "37.7749,37.8049,-122.4194,-122.3894",
    "elevationProvider": "mapbox-terrain-rgb",
    "format": "glb",
    "mapboxToken": "pk.eyJ1..."
  }'
```

#### GET `/api/v1/geoterrain/jobs/{jobId}`

Get terrain generation job status.

**Response:** `200 OK`
```json
{
  "jobId": "abc123def456",
  "status": "completed",
  "progress": 100,
  "outputPath": "/tmp/nexo-api/geoterrain/abc123def456.glb",
  "createdAt": "2024-01-23T10:00:00Z",
  "completedAt": "2024-01-23T10:05:00Z"
}
```

**Status Values:**
- `pending`: Job is queued
- `processing`: Job is being processed
- `completed`: Job completed successfully
- `failed`: Job failed with error

#### GET `/api/v1/geoterrain/jobs/{jobId}/download`

Download generated terrain mesh.

**Query Parameters:**
- `format` (optional): Output format. Default: "obj"

**Response:** `200 OK` (file download)

**Example:**
```bash
curl -O https://api.nexo.example.com/api/v1/geoterrain/jobs/abc123def456/download?format=glb
```

---

### Vector Feature Extraction

#### POST `/api/v1/geovector/extract`

Extract vector features from geographic bounds.

**Request Body:**
```json
{
  "bounds": "37.7749,37.8049,-122.4194,-122.3894",
  "vectorProvider": "hybrid",
  "featureKind": "building",
  "format": "geojson",
  "mapboxToken": "your_token_here",
  "osmPbfPath": "/path/to/data.osm.pbf"
}
```

**Response:** `202 Accepted`
```json
{
  "jobId": "xyz789ghi012",
  "status": "accepted"
}
```

**Parameters:**
- `bounds` (required): Geographic bounds as "minLat,maxLat,minLon,maxLon"
- `vectorProvider` (optional): Provider name (osm, mapbox, geojson, shapefile, hybrid). Default: "osm"
- `featureKind` (required): Feature type (building, road, water, vegetation, railway, power_line, administrative_boundary, land_use, point_of_interest, transportation_infrastructure)
- `format` (optional): Output format (json, geojson). Default: "json"
- `mapboxToken` (optional): Mapbox access token (required for mapbox provider)
- `osmPbfPath` (optional): OSM PBF file path (for osm/hybrid providers)
- `vectorFilePath` (optional): GeoJSON/Shapefile path (for geojson/shapefile providers)

**Example:**
```bash
curl -X POST https://api.nexo.example.com/api/v1/geovector/extract \
  -H "Content-Type: application/json" \
  -d '{
    "bounds": "37.7749,37.8049,-122.4194,-122.3894",
    "vectorProvider": "osm",
    "featureKind": "building",
    "format": "geojson",
    "osmPbfPath": "/data/california-latest.osm.pbf"
  }'
```

#### GET `/api/v1/geovector/jobs/{jobId}`

Get vector extraction job status.

**Response:** Same format as terrain job status.

#### GET `/api/v1/geovector/jobs/{jobId}/download`

Download extracted vector features.

**Query Parameters:**
- `format` (optional): Output format. Default: "json"

---

### World Bundle Generation

#### POST `/api/v1/world/generate`

Generate a complete world bundle with terrain, buildings, roads, water, and vegetation.

**Request Body:**
```json
{
  "bounds": "37.7749,37.8049,-122.4194,-122.3894",
  "elevationProvider": "mapbox-terrain-rgb",
  "vectorProvider": "hybrid",
  "format": "gltf",
  "mapboxToken": "your_token_here",
  "osmPbfPath": "/path/to/data.osm.pbf",
  "worldOptions": {
    "chunkTerrain": true,
    "chunkSizeMeters": 1000.0,
    "enableVegetation": true
  }
}
```

**Response:** `202 Accepted`
```json
{
  "jobId": "world123abc456",
  "status": "accepted"
}
```

**Parameters:**
- `bounds` (required): Geographic bounds
- `elevationProvider` (optional): Elevation data provider
- `vectorProvider` (optional): Vector data provider
- `format` (optional): Output format (obj, gltf, glb). Default: "obj"
- `mapboxToken` (optional): Mapbox access token
- `osmPbfPath` (optional): OSM PBF file path
- `worldOptions` (optional): World generation options

**Example:**
```bash
curl -X POST https://api.nexo.example.com/api/v1/world/generate \
  -H "Content-Type: application/json" \
  -d '{
    "bounds": "37.7749,37.8049,-122.4194,-122.3894",
    "elevationProvider": "mapbox-terrain-rgb",
    "vectorProvider": "hybrid",
    "format": "gltf",
    "mapboxToken": "pk.eyJ1...",
    "osmPbfPath": "/data/california-latest.osm.pbf"
  }'
```

#### GET `/api/v1/world/jobs/{jobId}`

Get world generation job status.

**Response:** Same format as terrain job status.

#### GET `/api/v1/world/jobs/{jobId}/download`

Download generated world bundle as ZIP archive.

**Response:** `200 OK` (ZIP file download)

#### POST `/api/v1/world/validate`

Validate a world bundle manifest.

**Request Body:**
```json
{
  "bundlePath": "/path/to/world/bundle"
}
```

**Response:** `200 OK`
```json
{
  "isValid": true,
  "issues": []
}
```

---

## Error Responses

All endpoints may return error responses in the following format:

**400 Bad Request:**
```json
{
  "message": "Invalid bounds format. Expected: minLat,maxLat,minLon,maxLon"
}
```

**404 Not Found:**
```json
{
  "message": "Job abc123def456 not found"
}
```

**500 Internal Server Error:**
```json
{
  "message": "An error occurred while processing your request"
}
```

---

## Rate Limiting

Rate limits are not currently enforced but will be added in future versions:
- **Free tier:** 100 requests/hour
- **Pro tier:** 1000 requests/hour
- **Enterprise:** Custom limits

Rate limit headers will be included in responses:
- `X-RateLimit-Limit`: Maximum requests per hour
- `X-RateLimit-Remaining`: Remaining requests in current window
- `X-RateLimit-Reset`: Unix timestamp when limit resets

---

## SDK Usage

### C# SDK

#### Basic Usage

```csharp
using Nexo.SDK;
using Nexo.GeoTerrain;

// Create terrain client
var elevationProvider = new SrtmHttpElevationProvider(httpClient);
var terrainClient = new GeoTerrainClient(elevationProvider, logger);

// Generate mesh
var bounds = GeoBounds.Parse("37.7749,37.8049,-122.4194,-122.3894");
var mesh = await terrainClient.GenerateMeshAsync(bounds);

// Export to file
await terrainClient.ExportMeshAsync(mesh, "terrain.obj", "obj");
```

#### Resource Estimation

```csharp
using Nexo.SDK;
using Nexo.SDK.Estimation;

// Create estimator with custom cost model
var costModel = new CostModelConfiguration
{
    MapboxVectorTileCostPerRequest = 0.0005m,
    SrtmBandwidthCostPerGb = 0.01m
};
var estimator = new ResourceEstimationService(costModel);

// Create client with estimator
var terrainClient = new GeoTerrainClient(
    elevationProvider, 
    logger, 
    estimator);

// Generate mesh with estimation
var bounds = GeoBounds.Parse("37.7749,37.8049,-122.4194,-122.3894");
var result = await terrainClient.GenerateMeshWithEstimationAsync(bounds);

// Access results
var mesh = result.Result;
Console.WriteLine($"Estimated: ${result.Estimate?.CostUsd:F4}, {result.Estimate?.MemoryMegabytes:F2} MB");
Console.WriteLine($"Actual: ${result.Actual?.CostUsd:F4}, {result.Actual?.MemoryMegabytes:F2} MB");

// Cost breakdown
if (result.Actual?.CostBreakdown != null)
{
    foreach (var component in result.Actual.CostBreakdown)
    {
        Console.WriteLine($"  {component.Key}: ${component.Value:F4}");
    }
}
```

#### Vector Feature Extraction

```csharp
using Nexo.SDK;
using Nexo.GeoVector.Values;

var vectorProvider = /* your provider */;
var vectorClient = new GeoVectorClient(vectorProvider, logger, estimator);

var bounds = GeoBounds.Parse("37.7749,37.8049,-122.4194,-122.3894");
var result = await vectorClient.ExtractFeaturesWithEstimationAsync(
    bounds, 
    FeatureKind.Building);

Console.WriteLine($"Extracted {result.Result.Features.Count} features");
Console.WriteLine($"Memory: {result.Actual?.MemoryMegabytes:F2} MB");
```

See [SDK Resource Estimation Guide](SDK_RESOURCE_ESTIMATION.md) for complete documentation.

### Python SDK (Future)

```python
from nexo_sdk import GeoTerrainClient

client = GeoTerrainClient()

bounds = {
    "minLat": 37.7749,
    "maxLat": 37.8049,
    "minLon": -122.4194,
    "maxLon": -122.3894
}

mesh = client.generate_mesh(bounds)
client.export_mesh(mesh, "terrain.obj", format="obj")
```

---

## Best Practices

1. **Use async/await** for all API calls
2. **Poll job status** every 1-2 seconds for long-running operations
3. **Handle errors gracefully** - check status codes and error messages
4. **Cache results** when possible to reduce API calls
5. **Use appropriate formats** - GLB for web, OBJ for offline, glTF for modern engines
6. **Set timeouts** appropriately for large-area processing

---

## Support

For API support, please visit:
- Documentation: https://github.com/IanFrelinger/Nexo/docs
- Issues: https://github.com/IanFrelinger/Nexo/issues
- Discussions: https://github.com/IanFrelinger/Nexo/discussions
