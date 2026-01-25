# SDK Resource Estimation Guide

## Overview

The Nexo SDK includes a comprehensive resource estimation system that provides cost and memory footprint estimates for downloaded and generated geospatial data. This helps developers:

- **Plan operations**: Estimate costs and memory before running operations
- **Optimize usage**: Identify expensive operations and optimize accordingly
- **Budget planning**: Understand resource requirements for large-scale operations
- **Debugging**: Compare estimates vs actuals to identify issues

## Architecture

### Design Approach

The estimation system uses a **three-phase approach**:

1. **Predictive Estimation** (Before Operation)
   - Estimates based on bounds, zoom levels, and typical data sizes
   - Helps with planning and decision-making
   - Uses configurable cost models

2. **Actual Measurement** (During Operation)
   - Tracks actual downloaded data sizes
   - Calculates actual memory footprint of generated structures
   - Provides accurate post-operation metrics

3. **Comparison & Analysis**
   - Compare estimates vs actuals
   - Identify discrepancies
   - Optimize based on real usage patterns

### Key Components

#### ResourceEstimate
Central data structure containing:
- `CostUsd`: Total cost in USD
- `MemoryBytes`: Total memory in bytes
- `CostBreakdown`: Per-component cost breakdown
- `MemoryBreakdown`: Per-component memory breakdown
- `IsEstimate`: Whether this is predictive (true) or actual (false)
- Helper properties: `MemoryMegabytes`, `MemoryGigabytes`

#### IResourceEstimator
Interface defining estimation methods:
- `EstimateSrtmTileDownload()`: SRTM elevation tile downloads
- `EstimateMapboxVectorTileDownload()`: Mapbox vector tiles
- `EstimateMapboxRasterTileDownload()`: Mapbox raster tiles
- `EstimateElevationGridMemory()`: In-memory elevation grids
- `EstimateMeshMemory()`: Triangle meshes
- `EstimateVectorFeaturesMemory()`: Vector feature collections
- `EstimateOsmPbfDownload()`: OSM PBF file downloads

#### ResourceEstimationService
Default implementation with:
- Configurable cost models
- Standard data size assumptions
- Tile count estimation algorithms

#### MemoryCalculator
Utility for calculating actual memory:
- `CalculateElevationGridMemory()`: Actual grid memory
- `CalculateMeshMemory()`: Actual mesh memory
- `CalculateVectorFeaturesMemory()`: Actual feature memory
- `CalculateTileDownloadMemory()`: Actual download size

## Usage Patterns

### Pattern 1: Estimate Before Operation

```csharp
var estimator = new ResourceEstimationService();
var bounds = GeoBounds.Parse("37.0,-122.0,37.1,-121.9");

// Get estimate before running operation
var estimate = EstimationHelpers.EstimateTerrainMeshGeneration(estimator, bounds);

if (estimate.MemoryGigabytes > 1.0)
{
    Console.WriteLine($"Warning: Operation will use {estimate.MemoryGigabytes:F2} GB");
}

if (estimate.CostUsd > 1.0m)
{
    Console.WriteLine($"Warning: Operation will cost ${estimate.CostUsd:F2}");
}
```

### Pattern 2: Get Estimate + Actual

```csharp
var estimator = new ResourceEstimationService();
var client = new GeoTerrainClient(elevationProvider, logger, estimator);

var result = await client.GenerateMeshWithEstimationAsync(bounds);

Console.WriteLine($"Before: {EstimationHelpers.FormatEstimate(result.Estimate)}");
Console.WriteLine($"After:  {EstimationHelpers.FormatEstimate(result.Actual)}");

// Compare
if (result.Estimate != null && result.Actual != null)
{
    var memoryDiff = result.Actual.MemoryBytes - result.Estimate.MemoryBytes;
    var memoryDiffPercent = (memoryDiff / (double)result.Estimate.MemoryBytes) * 100;
    Console.WriteLine($"Memory difference: {memoryDiffPercent:F1}%");
}
```

### Pattern 3: Cost Breakdown Analysis

```csharp
var result = await client.GenerateMeshWithEstimationAsync(bounds);

if (result.Actual?.CostBreakdown != null)
{
    Console.WriteLine("Cost Breakdown:");
    foreach (var component in result.Actual.CostBreakdown.OrderByDescending(kvp => kvp.Value))
    {
        Console.WriteLine($"  {component.Key}: ${component.Value:F4} ({component.Value / result.Actual.CostUsd * 100:F1}%)");
    }
}

if (result.Actual?.MemoryBreakdown != null)
{
    Console.WriteLine("Memory Breakdown:");
    foreach (var component in result.Actual.MemoryBreakdown.OrderByDescending(kvp => kvp.Value))
    {
        var mb = component.Value / (1024.0 * 1024.0);
        var percent = (component.Value / (double)result.Actual.MemoryBytes) * 100;
        Console.WriteLine($"  {component.Key}: {mb:F2} MB ({percent:F1}%)");
    }
}
```

### Pattern 4: Custom Cost Model

```csharp
// Define your actual pricing
var costModel = new CostModelConfiguration
{
    MapboxVectorTileCostPerRequest = 0.0005m,  // Your Mapbox pricing
    MapboxRasterTileCostPerRequest = 0.0005m,
    SrtmBandwidthCostPerGb = 0.01m,            // Your bandwidth costs
    OsmBandwidthCostPerGb = 0.01m
};

var estimator = new ResourceEstimationService(costModel);
var client = new GeoTerrainClient(elevationProvider, logger, estimator);
```

## Data Size Reference

### SRTM Tiles
- **SRTM-3 (standard)**: 1201×1201 samples = 2,884,802 bytes (~2.9 MB)
- **SRTM-1 (high-res)**: 3601×3601 samples = 25,934,402 bytes (~26 MB)
- **Compressed**: Typically 50-70% of uncompressed size

### Mapbox Tiles
- **Vector tiles**: 10-100 KB average (varies by zoom and area)
- **Raster tiles**: 50-200 KB average (varies by format)
- **Tile count**: Depends on zoom level and bounds area

### Memory Structures
- **ElevationGrid**: `width × height × 4 bytes` (float array)
- **Mesh Vertices**: `count × 12 bytes` (Vector3 = 3 floats)
- **Mesh Indices**: `count × 4 bytes` (int)
- **Mesh Normals**: `count × 12 bytes` (if present)
- **Mesh TexCoords**: `count × 8 bytes` (if present)

## Estimation Accuracy

### Factors Affecting Accuracy

1. **Tile Sizes**: Actual tile sizes vary (compression, data density)
2. **Mesh Complexity**: Actual mesh size depends on terrain complexity
3. **Feature Count**: Vector features vary significantly by area
4. **Provider Pricing**: Actual costs depend on your provider tier

### Improving Accuracy

1. **Use actual tile counts**: Pre-calculate tile counts when possible
2. **Provide vertex counts**: For vector features, provide estimated vertex counts
3. **Customize cost models**: Update with your actual provider pricing
4. **Compare estimates vs actuals**: Learn from past operations

## Example: Complete Workflow

```csharp
using Nexo.SDK;
using Nexo.SDK.Estimation;
using Nexo.GeoTerrain;

// Setup
var costModel = new CostModelConfiguration
{
    MapboxVectorTileCostPerRequest = 0.0005m,
    SrtmBandwidthCostPerGb = 0.01m
};

var estimator = new ResourceEstimationService(costModel);
var elevationProvider = /* your provider */;
var logger = /* your logger */;

var client = new GeoTerrainClient(elevationProvider, logger, estimator);

// Estimate before operation
var bounds = GeoBounds.Parse("37.0,-122.0,37.1,-121.9");
var estimate = EstimationHelpers.EstimateTerrainMeshGeneration(estimator, bounds);

Console.WriteLine($"Estimated cost: ${estimate.CostUsd:F4}");
Console.WriteLine($"Estimated memory: {estimate.MemoryGigabytes:F2} GB");

// Perform operation with tracking
var result = await client.GenerateMeshWithEstimationAsync(bounds);

// Analyze results
Console.WriteLine($"\nActual cost: ${result.Actual?.CostUsd:F4}");
Console.WriteLine($"Actual memory: {result.Actual?.MemoryGigabytes:F2} GB");

if (result.Estimate != null && result.Actual != null)
{
    var costAccuracy = (result.Estimate.CostUsd / result.Actual.CostUsd) * 100;
    var memoryAccuracy = (result.Estimate.MemoryBytes / (double)result.Actual.MemoryBytes) * 100;
    
    Console.WriteLine($"\nEstimation Accuracy:");
    Console.WriteLine($"  Cost: {costAccuracy:F1}%");
    Console.WriteLine($"  Memory: {memoryAccuracy:F1}%");
}

// Use the mesh
var mesh = result.Result;
// ... use mesh ...
```

## Best Practices

1. **Always estimate first**: Use estimates to validate feasibility
2. **Set budgets**: Check estimates against budget constraints
3. **Monitor actuals**: Track actual usage over time
4. **Optimize based on data**: Use breakdowns to identify optimization targets
5. **Cache when possible**: Use cached estimates to reduce costs
6. **Customize pricing**: Update cost models with your actual provider pricing

## Limitations

- Estimates are approximations based on typical data patterns
- Actual costs depend on provider pricing tiers and discounts
- Memory calculations don't include .NET runtime overhead
- Vector feature memory is estimated (geometry complexity varies significantly)
- Tile sizes vary by compression and data density
