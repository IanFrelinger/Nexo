# Resource Estimation System

The SDK includes a comprehensive resource estimation system that provides cost and memory footprint estimates for downloaded and generated geospatial data.

## Overview

The estimation system provides:
- **Cost estimation**: Predicts API costs (Mapbox tiles, bandwidth, etc.)
- **Memory estimation**: Calculates memory footprint of data structures
- **Predictive estimates**: Before operations (to help with planning)
- **Actual measurements**: After operations (for verification)

## Key Components

### ResourceEstimate
A record containing:
- `CostUsd`: Estimated or actual cost in USD
- `MemoryBytes`: Estimated or actual memory in bytes
- `CostBreakdown`: Detailed cost by component
- `MemoryBreakdown`: Detailed memory by component
- `IsEstimate`: Whether this is an estimate (true) or actual (false)
- Helper properties: `MemoryMegabytes`, `MemoryGigabytes`

### IResourceEstimator
Interface for estimation services with methods for:
- SRTM tile downloads
- Mapbox vector/raster tile downloads
- Elevation grid memory
- Mesh memory
- Vector features memory
- OSM PBF downloads

### ResourceEstimationService
Default implementation with configurable cost models.

### MemoryCalculator
Utility for calculating actual memory footprint of data structures.

## Usage Examples

### Basic Usage with Estimation

```csharp
using Nexo.SDK;
using Nexo.SDK.Estimation;
using Nexo.GeoTerrain;

// Create estimator with default cost model
var estimator = new ResourceEstimationService();

// Create client with estimator
var client = new GeoTerrainClient(
    elevationProvider,
    logger,
    estimator);

// Generate mesh with estimation
var bounds = GeoBounds.Parse("37.0,-122.0,37.1,-121.9");
var result = await client.GenerateMeshWithEstimationAsync(bounds);

// Access results
var mesh = result.Result;
var estimate = result.Estimate;  // Before operation
var actual = result.Actual;       // After operation

Console.WriteLine($"Estimated cost: ${estimate?.CostUsd:F4}");
Console.WriteLine($"Estimated memory: {estimate?.MemoryMegabytes:F2} MB");
Console.WriteLine($"Actual cost: ${actual?.CostUsd:F4}");
Console.WriteLine($"Actual memory: {actual?.MemoryMegabytes:F2} MB");
```

### Custom Cost Model

```csharp
var customCostModel = new CostModelConfiguration
{
    MapboxVectorTileCostPerRequest = 0.001m,  // $0.001 per tile
    MapboxRasterTileCostPerRequest = 0.001m,
    SrtmBandwidthCostPerGb = 0.02m,          // $0.02 per GB
    OsmBandwidthCostPerGb = 0.02m
};

var estimator = new ResourceEstimationService(customCostModel);
```

### Estimating Before Operations

```csharp
var estimator = new ResourceEstimationService();
var bounds = GeoBounds.Parse("37.0,-122.0,37.1,-121.9");

// Estimate tile downloads
var tileIds = SrtmTileCoverage.TilesCovering(bounds);
var downloadEstimate = estimator.EstimateSrtmTileDownload(tileIds);

Console.WriteLine($"Will download {tileIds.Count} tiles");
Console.WriteLine($"Estimated cost: ${downloadEstimate.CostUsd:F4}");
Console.WriteLine($"Estimated size: {downloadEstimate.MemoryMegabytes:F2} MB");

// Estimate mesh memory
var gridEstimate = estimator.EstimateElevationGridMemory(1000, 1000);
var meshEstimate = estimator.EstimateMeshMemory(1_000_000, 2_000_000);

var totalEstimate = downloadEstimate + gridEstimate + meshEstimate;
Console.WriteLine($"Total estimated memory: {totalEstimate.MemoryGigabytes:F2} GB");
```

### Cost Breakdown Analysis

```csharp
var result = await client.GenerateMeshWithEstimationAsync(bounds);

if (result.Actual?.CostBreakdown != null)
{
    Console.WriteLine("Cost Breakdown:");
    foreach (var kvp in result.Actual.CostBreakdown)
    {
        Console.WriteLine($"  {kvp.Key}: ${kvp.Value:F4}");
    }
}

if (result.Actual?.MemoryBreakdown != null)
{
    Console.WriteLine("Memory Breakdown:");
    foreach (var kvp in result.Actual.MemoryBreakdown)
    {
        Console.WriteLine($"  {kvp.Key}: {kvp.Value / (1024.0 * 1024.0):F2} MB");
    }
}
```

## Cost Models

### Default Pricing (Approximate)

- **Mapbox Vector Tiles**: $0.0005 per tile request
- **Mapbox Raster Tiles**: $0.0005 per tile request
- **SRTM Downloads**: $0.01 per GB (bandwidth)
- **OSM PBF**: $0.01 per GB (bandwidth)

**Note**: Actual pricing varies by provider tier and usage. Update `CostModelConfiguration` with your actual pricing.

### SRTM Tile Sizes

- **SRTM-3**: ~2.9 MB uncompressed (1201×1201 samples)
- **SRTM-1**: ~26 MB uncompressed (3601×3601 samples)

## Memory Calculations

### ElevationGrid
- Formula: `width × height × 4 bytes` (float array)
- Example: 1000×1000 grid = 4 MB

### MeshData
- Vertices: `count × 12 bytes` (Vector3 = 3 floats)
- Indices: `count × 4 bytes` (int)
- Normals: `count × 12 bytes` (if present)
- TexCoords: `count × 8 bytes` (if present)
- Example: 1M vertices, 2M indices = ~32 MB

### Vector Features
- Approximate: ~500 bytes per feature
- More accurate with vertex count: `vertices × 8 bytes + features × 200 bytes`

## Best Practices

1. **Use estimates for planning**: Check estimates before large operations
2. **Compare estimates vs actuals**: Helps identify optimization opportunities
3. **Customize cost models**: Update with your actual provider pricing
4. **Monitor memory**: Use breakdowns to identify memory-heavy components
5. **Cache when possible**: Set `fromCache=true` for cached tile estimates

## Limitations

- Estimates are approximations based on typical data sizes
- Actual costs depend on provider pricing tiers and discounts
- Memory calculations don't include GC overhead or .NET object headers
- Vector feature memory is estimated (geometry complexity varies)
