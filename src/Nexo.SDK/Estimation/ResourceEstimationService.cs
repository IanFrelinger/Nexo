using Nexo.GeoTerrain;

namespace Nexo.SDK.Estimation;

/// <summary>
/// Service for estimating resource usage (cost and memory) for geospatial operations.
/// </summary>
public class ResourceEstimationService : IResourceEstimator
{
    private readonly CostModelConfiguration _costModel;

    /// <summary>
    /// Standard SRTM tile size: 1201x1201 samples = 2,884,802 bytes (uncompressed).
    /// SRTM-1 tiles are typically ~2.9 MB uncompressed, ~1.4 MB compressed.
    /// </summary>
    private const long SrtmTileSizeBytes = 2_884_802; // 1201 * 1201 * 2 bytes

    /// <summary>
    /// Standard SRTM-1 tile size: 3601x3601 samples = 25,934,402 bytes (uncompressed).
    /// </summary>
    private const long Srtm1TileSizeBytes = 25_934_402; // 3601 * 3601 * 2 bytes

    /// <summary>
    /// Average Mapbox vector tile size (varies by zoom and area, typically 10-100 KB).
    /// </summary>
    private const long AverageMapboxVectorTileSizeBytes = 50_000; // 50 KB average

    /// <summary>
    /// Average Mapbox raster tile size (varies by format, typically 50-200 KB).
    /// </summary>
    private const long AverageMapboxRasterTileSizeBytes = 100_000; // 100 KB average

    /// <summary>
    /// Size of a Vector3 (3 floats = 12 bytes).
    /// </summary>
    private const int Vector3SizeBytes = 12;

    /// <summary>
    /// Size of a Vector2 (2 floats = 8 bytes).
    /// </summary>
    private const int Vector2SizeBytes = 8;

    /// <summary>
    /// Size of an int (4 bytes).
    /// </summary>
    private const int IntSizeBytes = 4;

    /// <summary>
    /// Size of a float (4 bytes).
    /// </summary>
    private const int FloatSizeBytes = 4;

    /// <summary>
    /// Initializes a new instance of the ResourceEstimationService.
    /// </summary>
    /// <param name="costModel">Optional cost model configuration. Uses default if not provided.</param>
    public ResourceEstimationService(CostModelConfiguration? costModel = null)
    {
        _costModel = costModel ?? CostModels.Default;
    }

    /// <summary>
    /// Estimates cost and memory for downloading SRTM elevation tiles.
    /// </summary>
    /// <param name="tileIds">List of SRTM tile IDs to download</param>
    /// <param name="fromCache">Whether tiles are from cache (no cost if true)</param>
    /// <returns>Resource estimate for tile downloads</returns>
    public ResourceEstimate EstimateSrtmTileDownload(IReadOnlyList<SrtmTileId> tileIds, bool fromCache = false)
    {
        if (tileIds == null || tileIds.Count == 0)
            return new ResourceEstimate { CostUsd = 0, MemoryBytes = 0 };

        // Assume standard SRTM-3 tiles (1201x1201) unless we can detect SRTM-1
        // For estimation purposes, we'll use the standard size
        var totalBytesPerTile = SrtmTileSizeBytes;
        var totalBytes = (long)tileIds.Count * totalBytesPerTile;
        var totalGb = totalBytes / (1024.0 * 1024.0 * 1024.0);

        var cost = fromCache ? 0 : (decimal)totalGb * _costModel.SrtmBandwidthCostPerGb;

        return new ResourceEstimate
        {
            CostUsd = cost,
            MemoryBytes = totalBytes,
            CostBreakdown = new Dictionary<string, decimal>
            {
                ["srtm-downloads"] = cost
            },
            MemoryBreakdown = new Dictionary<string, long>
            {
                ["srtm-tiles"] = totalBytes
            },
            IsEstimate = true,
            Metadata = new Dictionary<string, object>
            {
                ["tile-count"] = tileIds.Count,
                ["from-cache"] = fromCache,
                ["bytes-per-tile"] = totalBytesPerTile
            }
        };
    }

    /// <summary>
    /// Estimates cost and memory for downloading Mapbox vector tiles.
    /// </summary>
    /// <param name="bounds">Geographic bounds</param>
    /// <param name="zoomLevel">Zoom level for tiles</param>
    /// <param name="estimatedTileCount">Optional pre-calculated tile count</param>
    /// <returns>Resource estimate for vector tile downloads</returns>
    public ResourceEstimate EstimateMapboxVectorTileDownload(GeoBounds bounds, int zoomLevel, int? estimatedTileCount = null)
    {
        var tileCount = estimatedTileCount ?? EstimateTileCount(bounds, zoomLevel);
        var totalBytes = (long)tileCount * AverageMapboxVectorTileSizeBytes;
        var cost = (decimal)tileCount * _costModel.MapboxVectorTileCostPerRequest;

        return new ResourceEstimate
        {
            CostUsd = cost,
            MemoryBytes = totalBytes,
            CostBreakdown = new Dictionary<string, decimal>
            {
                ["mapbox-vector-tiles"] = cost
            },
            MemoryBreakdown = new Dictionary<string, long>
            {
                ["mapbox-vector-tiles"] = totalBytes
            },
            IsEstimate = true,
            Metadata = new Dictionary<string, object>
            {
                ["tile-count"] = tileCount,
                ["zoom-level"] = zoomLevel
            }
        };
    }

    /// <summary>
    /// Estimates cost and memory for downloading Mapbox raster tiles.
    /// </summary>
    /// <param name="bounds">Geographic bounds</param>
    /// <param name="zoomLevel">Zoom level for tiles</param>
    /// <param name="estimatedTileCount">Optional pre-calculated tile count</param>
    /// <returns>Resource estimate for raster tile downloads</returns>
    public ResourceEstimate EstimateMapboxRasterTileDownload(GeoBounds bounds, int zoomLevel, int? estimatedTileCount = null)
    {
        var tileCount = estimatedTileCount ?? EstimateTileCount(bounds, zoomLevel);
        var totalBytes = (long)tileCount * AverageMapboxRasterTileSizeBytes;
        var cost = (decimal)tileCount * _costModel.MapboxRasterTileCostPerRequest;

        return new ResourceEstimate
        {
            CostUsd = cost,
            MemoryBytes = totalBytes,
            CostBreakdown = new Dictionary<string, decimal>
            {
                ["mapbox-raster-tiles"] = cost
            },
            MemoryBreakdown = new Dictionary<string, long>
            {
                ["mapbox-raster-tiles"] = totalBytes
            },
            IsEstimate = true,
            Metadata = new Dictionary<string, object>
            {
                ["tile-count"] = tileCount,
                ["zoom-level"] = zoomLevel
            }
        };
    }

    /// <summary>
    /// Estimates memory footprint for an elevation grid.
    /// </summary>
    /// <param name="width">Grid width in samples</param>
    /// <param name="height">Grid height in samples</param>
    /// <returns>Resource estimate for elevation grid memory</returns>
    public ResourceEstimate EstimateElevationGridMemory(int width, int height)
    {
        // ElevationGrid stores: float[] heightsMeters = width * height * 4 bytes
        // Plus object overhead and metadata
        var heightsBytes = (long)width * height * FloatSizeBytes;
        var objectOverhead = 100; // Approximate object overhead

        return new ResourceEstimate
        {
            CostUsd = 0, // No cost for in-memory data
            MemoryBytes = heightsBytes + objectOverhead,
            MemoryBreakdown = new Dictionary<string, long>
            {
                ["elevation-grid"] = heightsBytes,
                ["object-overhead"] = objectOverhead
            },
            IsEstimate = true,
            Metadata = new Dictionary<string, object>
            {
                ["width"] = width,
                ["height"] = height,
                ["sample-count"] = width * height
            }
        };
    }

    /// <summary>
    /// Estimates memory footprint for a mesh.
    /// </summary>
    /// <param name="vertexCount">Number of vertices</param>
    /// <param name="indexCount">Number of indices</param>
    /// <param name="hasNormals">Whether normals are present</param>
    /// <param name="hasTexCoords">Whether texture coordinates are present</param>
    /// <returns>Resource estimate for mesh memory</returns>
    public ResourceEstimate EstimateMeshMemory(int vertexCount, int indexCount, bool hasNormals = true, bool hasTexCoords = true)
    {
        // MeshData memory breakdown:
        // - Vertices: vertexCount * Vector3SizeBytes
        // - Indices: indexCount * IntSizeBytes
        // - Normals: (hasNormals ? vertexCount * Vector3SizeBytes : 0)
        // - TexCoords: (hasTexCoords ? vertexCount * Vector2SizeBytes : 0)
        // - List overhead: ~50 bytes per list
        var verticesBytes = (long)vertexCount * Vector3SizeBytes;
        var indicesBytes = (long)indexCount * IntSizeBytes;
        var normalsBytes = hasNormals ? (long)vertexCount * Vector3SizeBytes : 0;
        var texCoordsBytes = hasTexCoords ? (long)vertexCount * Vector2SizeBytes : 0;
        var listOverhead = 200; // Approximate overhead for 4 lists

        var totalBytes = verticesBytes + indicesBytes + normalsBytes + texCoordsBytes + listOverhead;

        return new ResourceEstimate
        {
            CostUsd = 0, // No cost for in-memory data
            MemoryBytes = totalBytes,
            MemoryBreakdown = new Dictionary<string, long>
            {
                ["mesh-vertices"] = verticesBytes,
                ["mesh-indices"] = indicesBytes,
                ["mesh-normals"] = normalsBytes,
                ["mesh-texcoords"] = texCoordsBytes,
                ["object-overhead"] = listOverhead
            },
            IsEstimate = true,
            Metadata = new Dictionary<string, object>
            {
                ["vertex-count"] = vertexCount,
                ["index-count"] = indexCount,
                ["triangle-count"] = indexCount / 3,
                ["has-normals"] = hasNormals,
                ["has-texcoords"] = hasTexCoords
            }
        };
    }

    /// <summary>
    /// Estimates memory footprint for vector features.
    /// </summary>
    /// <param name="featureCount">Number of features</param>
    /// <param name="estimatedVertexCount">Optional estimated total vertex count for more accurate calculation</param>
    /// <returns>Resource estimate for vector features memory</returns>
    public ResourceEstimate EstimateVectorFeaturesMemory(int featureCount, int? estimatedVertexCount = null)
    {
        // GeoFeature memory is harder to estimate precisely, but we can approximate:
        // - Each feature has: Id (string ~50 bytes), Kind, Geometry, Properties
        // - Geometry varies: polygons can have many vertices
        // - Average feature: ~500 bytes (geometry + properties)
        // - If vertex count provided, use more accurate calculation
        long totalBytes;
        if (estimatedVertexCount.HasValue)
        {
            // More accurate: vertices + feature overhead
            var verticesBytes = (long)estimatedVertexCount.Value * Vector2SizeBytes; // GeoPolygon uses Vector2
            var featureOverhead = (long)featureCount * 500; // Per-feature overhead
            totalBytes = verticesBytes + featureOverhead;
        }
        else
        {
            // Conservative estimate: 500 bytes per feature
            totalBytes = (long)featureCount * 500;
        }

        return new ResourceEstimate
        {
            CostUsd = 0, // No cost for in-memory data
            MemoryBytes = totalBytes,
            MemoryBreakdown = new Dictionary<string, long>
            {
                ["vector-features"] = totalBytes
            },
            IsEstimate = true,
            Metadata = new Dictionary<string, object>
            {
                ["feature-count"] = featureCount,
                ["estimated-vertex-count"] = estimatedVertexCount ?? 0
            }
        };
    }

    /// <summary>
    /// Estimates cost and memory for downloading OSM PBF data.
    /// </summary>
    public ResourceEstimate EstimateOsmPbfDownload(long fileSizeBytes)
    {
        var totalGb = fileSizeBytes / (1024.0 * 1024.0 * 1024.0);
        var cost = (decimal)totalGb * _costModel.OsmBandwidthCostPerGb;

        return new ResourceEstimate
        {
            CostUsd = cost,
            MemoryBytes = fileSizeBytes,
            CostBreakdown = new Dictionary<string, decimal>
            {
                ["osm-download"] = cost
            },
            MemoryBreakdown = new Dictionary<string, long>
            {
                ["osm-pbf"] = fileSizeBytes
            },
            IsEstimate = true,
            Metadata = new Dictionary<string, object>
            {
                ["file-size-bytes"] = fileSizeBytes
            }
        };
    }

    /// <summary>
    /// Estimates the number of tiles needed to cover the given bounds at the specified zoom level.
    /// Uses Web Mercator tile math.
    /// </summary>
    private static int EstimateTileCount(GeoBounds bounds, int zoom)
    {
        // Simple estimation: calculate approximate tile coverage
        // More accurate would use WebMercatorTileMath, but this is a reasonable approximation
        var latRange = bounds.MaxLatitude.Degrees - bounds.MinLatitude.Degrees;
        var lonRange = bounds.MaxLongitude.Degrees - bounds.MinLongitude.Degrees;

        // At zoom level z, there are 2^z tiles in each direction
        var tilesPerDegreeLat = Math.Pow(2, zoom) / 180.0;
        var tilesPerDegreeLon = Math.Pow(2, zoom) / 360.0;

        var latTiles = (int)Math.Ceiling(latRange * tilesPerDegreeLat);
        var lonTiles = (int)Math.Ceiling(lonRange * tilesPerDegreeLon);

        return Math.Max(1, latTiles * lonTiles);
    }
}
