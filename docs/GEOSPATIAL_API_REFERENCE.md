# Geospatial API Reference

Programmatic API reference for Nexo's geospatial capabilities.

## Namespaces

- `Nexo.GeoTerrain` - Elevation grid and terrain mesh generation
- `Nexo.GeoVector` - Vector feature processing and mesh generation
- `Nexo.GeoWorld` - World bundle generation and composition
- `Nexo.Adapters.GeoTerrain` - Elevation data providers
- `Nexo.Adapters.GeoVector` - Vector data providers
- `Nexo.GeoTerrain.Validation` - Data integrity and quality validation

## Core Types

### ElevationGrid

Represents a regular grid of elevation samples.

```csharp
var grid = new ElevationGrid(
    width: 1000,
    height: 1000,
    bounds: new GeoBounds(...),
    spacing: new GridSpacing(0.0001), // degrees
    heightsMeters: heightArray
);

var elevation = grid.GetHeightMeters(x, y);
```

### MeshData

Triangle mesh representation.

```csharp
var mesh = new MeshData
{
    Vertices = vertices,
    Indices = indices,
    Normals = normals,
    TexCoords = texCoords
};
```

### GeoBounds

Geographic bounding box.

```csharp
var bounds = new GeoBounds(
    minLat: new Latitude(37.7),
    minLon: new Longitude(-122.5),
    maxLat: new Latitude(37.8),
    maxLon: new Longitude(-122.4)
);

bounds.Validate(); // Throws if invalid
bool contains = bounds.Contains(point);
bool intersects = bounds.Intersects(otherBounds);
```

### GeoFeature

Vector feature (building, road, water, etc.).

```csharp
var feature = new GeoFeature(
    id: "building-123",
    kind: FeatureKind.Building,
    geometry: polygon,
    properties: properties
);
```

## Elevation Providers

### IElevationProvider

Interface for elevation data sources.

```csharp
public interface IElevationProvider
{
    Task<ElevationTile> GetSrtmTileAsync(
        SrtmTileId tileId,
        CancellationToken cancellationToken = default);
}
```

### Available Providers

#### LocalFileElevationProvider

Loads from local SRTM HGT, GeoTIFF, or ASCII Grid files.

```csharp
var provider = new LocalFileElevationProvider(
    directoryPath: "/path/to/srtm",
    logger: logger
);
```

#### GeoTiffElevationProvider

Loads from GeoTIFF files.

```csharp
var provider = new GeoTiffElevationProvider(
    filePath: "/path/to/elevation.tif",
    boundsOverride: bounds, // optional
    scale: 1.0f,
    offset: 0.0f,
    logger: logger
);
```

#### AsciiGridElevationProvider

Loads from ASCII Grid files.

```csharp
var provider = new AsciiGridElevationProvider(
    filePath: "/path/to/elevation.asc",
    logger: logger
);
```

#### ResilientElevationProvider

Wraps a provider with retry logic, circuit breaker, and rate limiting.

```csharp
var inner = new SrtmHttpElevationProvider(httpClient, baseUrl, logger);
var provider = new ResilientElevationProvider(
    inner: inner,
    retryPolicy: new RetryPolicy(...),
    circuitBreaker: new CircuitBreaker(...),
    rateLimiter: new RateLimiter(...),
    logger: logger
);
```

#### PartialFailureElevationProvider

Supports partial failure handling for batch operations.

```csharp
var inner = new SrtmHttpElevationProvider(httpClient, baseUrl, logger);
var provider = new PartialFailureElevationProvider(inner, logger);

var tileIds = new[] { tile1, tile2, tile3 };
var result = await provider.GetTilesAsync(tileIds, cancellationToken);

if (result.Partial)
{
    Console.WriteLine($"Partial success: {result.Results.Count}/{result.TotalAttempted}");
    foreach (var failure in result.Failures)
    {
        Console.WriteLine($"Failed: {failure}");
    }
}
```

## Vector Providers

### IVectorProvider

Interface for vector data sources.

```csharp
public interface IVectorProvider
{
    Task<GeoFeatureSet> GetFeaturesAsync(
        GeoBounds bounds,
        FeatureKind kind,
        CancellationToken cancellationToken = default);
}
```

### Available Providers

#### GeoJsonVectorProvider

Loads from GeoJSON files.

```csharp
var provider = new GeoJsonVectorProvider(
    filePath: "/path/to/features.geojson",
    logger: logger
);
```

#### ShapefileVectorProvider

Loads from Shapefile format.

```csharp
var provider = new ShapefileVectorProvider(
    shapefilePath: "/path/to/features.shp",
    logger: logger
);
```

#### ResilientVectorProvider

Wraps a provider with resilience features.

```csharp
var inner = new MapboxVectorTileProvider(httpClient, token, logger);
var provider = new ResilientVectorProvider(
    inner: inner,
    retryPolicy: new RetryPolicy(...),
    circuitBreaker: new CircuitBreaker(...),
    rateLimiter: new RateLimiter(...),
    logger: logger
);
```

## Data Integrity Validation

### DataIntegrityChecker

Validates data integrity and detects corruption.

```csharp
// Compute checksum
var checksum = DataIntegrityChecker.ComputeChecksum(data);
bool isValid = DataIntegrityChecker.ValidateChecksum(data, expectedChecksum);

// Detect corruption
var report = DataIntegrityChecker.DetectCorruption(
    grid: elevationGrid,
    maxReasonableElevationMeters: 9000f,
    minReasonableElevationMeters: -500f,
    maxSuddenChangeMeters: 1000f,
    maxNoDataRatio: 0.5f
);

if (report.IsCorrupted)
{
    foreach (var issue in report.Issues)
    {
        Console.WriteLine($"Issue: {issue}");
    }
}

// Validate projection parameters
bool valid = DataIntegrityChecker.ValidateProjectionParameters(
    bounds: bounds,
    spacing: spacing,
    out var error
);
```

### MeshQualityMetrics

Advanced mesh quality analysis.

```csharp
// Triangle quality
var triangleQuality = MeshQualityMetrics.ComputeTriangleQuality(mesh);
Console.WriteLine($"Aspect ratio: {triangleQuality.AverageAspectRatio}");
Console.WriteLine($"Sliver triangles: {triangleQuality.SliverTriangleCount}");

// Mesh accuracy
var accuracy = MeshQualityMetrics.ValidateMeshAccuracy(mesh, sourceGrid);
Console.WriteLine($"Max deviation: {accuracy.MaxDeviation}m");
Console.WriteLine($"RMS error: {accuracy.RmsError}m");
Console.WriteLine($"Acceptable: {accuracy.IsAcceptable}");

// Max slope validation
var slopeReport = MeshQualityMetrics.ValidateMaxSlope(mesh, maxAllowedSlopeDegrees: 60f);
Console.WriteLine($"Max slope: {slopeReport.MaxSlopeDegrees}°");
Console.WriteLine($"Valid: {slopeReport.IsValid}");

// Normal consistency
var normalConsistency = MeshQualityMetrics.ComputeNormalConsistency(mesh);
Console.WriteLine($"Consistency ratio: {normalConsistency.ConsistencyRatio}");
```

## Spatial Indexing

### Quadtree

Spatial index for efficient spatial queries.

```csharp
// Create quadtree
var bounds = new GeoBounds(...);
var quadtree = new Quadtree(bounds, maxDepth: 10, maxItemsPerNode: 10);

// Insert features
quadtree.InsertRange(features);

// Query by bounds
var results = quadtree.Query(queryBounds);

// Query by radius
var center = new GeoPoint { Latitude = ..., Longitude = ... };
var nearby = quadtree.QueryRadius(center, radiusMeters: 1000);

// Clear
quadtree.Clear();
```

## Mesh Generation

### GridMeshGenerator

Generates triangle mesh from elevation grid.

```csharp
var options = new MeshGenerationOptions
{
    // Configuration options
};

var mesh = GridMeshGenerator.Generate(grid, options);
```

### BuildingMeshGenerator

Generates 3D meshes from building polygons.

```csharp
var generator = new BuildingMeshGenerator();
var mesh = generator.Generate(buildingFeature, elevationGrid);
```

## World Bundle

### WorldBundleWriters

Writes world bundles in various formats.

```csharp
var manifest = new WorldBundleManifest { ... };
var writers = new WorldBundleWriters();

// Write OBJ format
await writers.WriteObjAsync(outputPath, manifest, meshes, cancellationToken);

// Write glTF format
await writers.WriteGltfAsync(outputPath, manifest, meshes, cancellationToken);

// Write GLB format
await writers.WriteGlbAsync(outputPath, manifest, meshes, cancellationToken);
```

## Error Handling

### Partial Results

Many operations support partial failure:

```csharp
var result = await provider.GetTilesAsync(tileIds, cancellationToken);

if (result.Success)
{
    // All tiles succeeded
}
else if (result.Partial)
{
    // Some tiles succeeded, some failed
    Console.WriteLine($"Success rate: {result.SuccessRate:P1}");
    // Use result.Results for successful tiles
    // Check result.Failures for error messages
}
else
{
    // All tiles failed
    foreach (var failure in result.Failures)
    {
        Console.WriteLine($"Failure: {failure}");
    }
}
```

## Logging

All providers support Microsoft.Extensions.Logging:

```csharp
using Microsoft.Extensions.Logging;

var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
var logger = loggerFactory.CreateLogger<MyClass>();

var provider = new LocalFileElevationProvider(
    directoryPath: "/path/to/srtm",
    logger: logger
);
```

## Cancellation

All async operations support cancellation:

```csharp
using var cts = new CancellationTokenSource();
cts.CancelAfter(TimeSpan.FromMinutes(5));

try
{
    var tile = await provider.GetSrtmTileAsync(tileId, cts.Token);
}
catch (OperationCanceledException)
{
    Console.WriteLine("Operation was cancelled");
}
```

## Examples

### Complete Example: Generate Terrain from GeoTIFF

```csharp
using Nexo.Adapters.GeoTerrain.Providers;
using Nexo.GeoTerrain;

var provider = new GeoTiffElevationProvider(
    filePath: "elevation.tif",
    logger: logger
);

var tileId = new SrtmTileId(37, -122);
var tile = await provider.GetSrtmTileAsync(tileId);

// Convert to elevation grid
var grid = ElevationGrid.FromTile(tile);

// Generate mesh
var mesh = GridMeshGenerator.Generate(grid, new MeshGenerationOptions());

// Validate quality
var quality = MeshQualityAnalyzer.AnalyzeAdvanced(grid, mesh);
Console.WriteLine($"Triangles: {quality.BasicMetrics.TriangleCount}");
Console.WriteLine($"Max slope: {quality.MaxSlope.MaxSlopeDegrees}°");
```

### Example: Query Vector Features with Spatial Index

```csharp
using Nexo.GeoVector.Spatial;
using Nexo.Adapters.GeoVector.Providers;

var provider = new GeoJsonVectorProvider("buildings.geojson", logger);
var bounds = new GeoBounds(...);
var features = await provider.GetFeaturesAsync(bounds, FeatureKind.Building);

// Build spatial index
var quadtree = new Quadtree(bounds);
quadtree.InsertRange(features.Features);

// Query specific area
var queryBounds = new GeoBounds(...);
var results = quadtree.Query(queryBounds);
Console.WriteLine($"Found {results.Count} features in query area");
```
