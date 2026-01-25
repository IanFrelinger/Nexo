using Microsoft.Extensions.Logging;
using Nexo.GeoTerrain;
using Nexo.Orchestration.GeoTerrain.Ports;
using Nexo.SDK.Estimation;

namespace Nexo.SDK;

/// <summary>
/// Client for terrain generation operations.
/// Provides programmatic access to terrain mesh generation without shelling out to CLI.
/// </summary>
public class GeoTerrainClient
{
    private readonly IElevationProvider _elevationProvider;
    private readonly ILogger<GeoTerrainClient>? _logger;
    private readonly IResourceEstimator? _estimator;

    /// <summary>
    /// Initializes a new instance of the GeoTerrainClient.
    /// </summary>
    /// <param name="elevationProvider">Elevation data provider</param>
    /// <param name="logger">Optional logger</param>
    /// <param name="estimator">Optional resource estimator for cost and memory tracking</param>
    public GeoTerrainClient(
        IElevationProvider elevationProvider, 
        ILogger<GeoTerrainClient>? logger = null,
        IResourceEstimator? estimator = null)
    {
        _elevationProvider = elevationProvider ?? throw new ArgumentNullException(nameof(elevationProvider));
        _logger = logger;
        _estimator = estimator;
    }

    /// <summary>
    /// Generate a terrain mesh from elevation data.
    /// </summary>
    /// <param name="bounds">Geographic bounds</param>
    /// <param name="options">Mesh generation options</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Generated mesh data</returns>
    public async Task<MeshData> GenerateMeshAsync(
        GeoBounds bounds,
        MeshGenerationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        if (bounds == null)
            throw new ArgumentNullException(nameof(bounds));

        bounds.Validate();
        options ??= new MeshGenerationOptions();

        _logger?.LogInformation("Generating terrain mesh for bounds {Bounds}", bounds);

        // Get tiles covering the bounds
        var tileIds = SrtmTileCoverage.TilesCovering(bounds);
        if (tileIds.Count == 0)
        {
            throw new InvalidOperationException("No elevation tiles cover the requested bounds.");
        }

        _logger?.LogInformation("Fetching {Count} elevation tiles", tileIds.Count);

        // Fetch all tiles
        var hgtBytesByTile = new Dictionary<SrtmTileId, byte[]>(tileIds.Count);
        foreach (var tileId in tileIds)
        {
            var tile = await _elevationProvider.GetSrtmTileAsync(tileId, cancellationToken);
            hgtBytesByTile[tileId] = tile.HgtBytes;
        }

        _logger?.LogInformation("Building elevation grid from {Count} tiles", hgtBytesByTile.Count);

        // Build elevation grid from tiles
        var grid = SrtmMosaicBuilder.Build(hgtBytesByTile);

        _logger?.LogInformation("Generating mesh from grid ({Width}x{Height})", grid.Width, grid.Height);

        // Generate mesh from grid
        var mesh = GridMeshGenerator.Generate(grid, options);

        _logger?.LogInformation("Generated mesh with {VertexCount} vertices and {TriangleCount} triangles",
            mesh.Vertices.Count, mesh.Indices.Count / 3);

        return mesh;
    }

    /// <summary>
    /// Generate a terrain mesh from elevation data with resource estimation.
    /// </summary>
    /// <param name="bounds">Geographic bounds</param>
    /// <param name="options">Mesh generation options</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Generated mesh data with resource estimates</returns>
    public async Task<EstimationResult<MeshData>> GenerateMeshWithEstimationAsync(
        GeoBounds bounds,
        MeshGenerationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        if (bounds == null)
            throw new ArgumentNullException(nameof(bounds));

        bounds.Validate();
        options ??= new MeshGenerationOptions();

        ResourceEstimate? estimate = null;
        ResourceEstimate? actual = null;

        // Get tiles covering the bounds
        var tileIds = SrtmTileCoverage.TilesCovering(bounds);
        if (tileIds.Count == 0)
        {
            throw new InvalidOperationException("No elevation tiles cover the requested bounds.");
        }

        // Generate estimate before operation
        if (_estimator != null)
        {
            // Check if we have a cache root from elevation provider metadata
            string? cacheRoot = null;
            var downloadEstimate = _estimator.EstimateSrtmTileDownload(tileIds, fromCache: false, cacheRoot: cacheRoot);
            
            // Estimate grid size (approximate based on bounds)
            var estimatedWidth = 1000; // Conservative estimate
            var estimatedHeight = 1000;
            var gridEstimate = _estimator.EstimateElevationGridMemory(estimatedWidth, estimatedHeight);
            
            // Estimate mesh size (approximate: 2 triangles per grid cell)
            var estimatedTriangles = (estimatedWidth - 1) * (estimatedHeight - 1) * 2;
            var estimatedVertices = estimatedWidth * estimatedHeight;
            var meshEstimate = _estimator.EstimateMeshMemory(estimatedVertices, estimatedTriangles * 3);
            
            estimate = downloadEstimate + gridEstimate + meshEstimate;
            
            _logger?.LogInformation("Estimated cost: ${Cost:F4}, memory: {Memory:F2} MB", 
                estimate.CostUsd, estimate.MemoryMegabytes);
        }

        // Perform actual operation and track resources
        _logger?.LogInformation("Generating terrain mesh for bounds {Bounds}", bounds);
        _logger?.LogInformation("Fetching {Count} elevation tiles", tileIds.Count);

        // Fetch all tiles and track actual download sizes
        var hgtBytesByTile = new Dictionary<SrtmTileId, byte[]>(tileIds.Count);
        long actualDownloadBytes = 0;
        foreach (var tileId in tileIds)
        {
            var tile = await _elevationProvider.GetSrtmTileAsync(tileId, cancellationToken);
            hgtBytesByTile[tileId] = tile.HgtBytes;
            actualDownloadBytes += tile.HgtBytes.Length;
        }

        _logger?.LogInformation("Building elevation grid from {Count} tiles", hgtBytesByTile.Count);

        // Build elevation grid from tiles
        var grid = SrtmMosaicBuilder.Build(hgtBytesByTile);

        _logger?.LogInformation("Generating mesh from grid ({Width}x{Height})", grid.Width, grid.Height);

        // Generate mesh from grid
        var mesh = GridMeshGenerator.Generate(grid, options);

        _logger?.LogInformation("Generated mesh with {VertexCount} vertices and {TriangleCount} triangles",
            mesh.Vertices.Count, mesh.Indices.Count / 3);

        // Calculate actual resource usage
        if (_estimator != null)
        {
            var actualDownload = new ResourceEstimate
            {
                CostUsd = _estimator.EstimateSrtmTileDownload(tileIds, fromCache: cachedCount == tileIds.Count, cacheRoot: null).CostUsd,
                MemoryBytes = actualDownloadBytes,
                IsEstimate = false,
                MemoryBreakdown = new Dictionary<string, long>
                {
                    ["srtm-tiles"] = actualDownloadBytes
                }
            };
            
            var actualGrid = new ResourceEstimate
            {
                CostUsd = 0,
                MemoryBytes = MemoryCalculator.CalculateElevationGridMemory(grid),
                IsEstimate = false,
                MemoryBreakdown = new Dictionary<string, long>
                {
                    ["elevation-grid"] = MemoryCalculator.CalculateElevationGridMemory(grid)
                }
            };
            
            var actualMesh = new ResourceEstimate
            {
                CostUsd = 0,
                MemoryBytes = MemoryCalculator.CalculateMeshMemory(mesh),
                IsEstimate = false,
                MemoryBreakdown = new Dictionary<string, long>
                {
                    ["mesh"] = MemoryCalculator.CalculateMeshMemory(mesh)
                }
            };
            
            actual = actualDownload + actualGrid + actualMesh;
            
            _logger?.LogInformation("Actual cost: ${Cost:F4}, memory: {Memory:F2} MB", 
                actual.CostUsd, actual.MemoryMegabytes);
        }

        return new EstimationResult<MeshData>
        {
            Result = mesh,
            Estimate = estimate,
            Actual = actual
        };
    }

    /// <summary>
    /// Export mesh to a file.
    /// </summary>
    /// <param name="mesh">Mesh data to export</param>
    /// <param name="filePath">Output file path</param>
    /// <param name="format">Export format (obj, gltf, glb, fbx, usd)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task ExportMeshAsync(
        MeshData mesh,
        string filePath,
        string format = "obj",
        CancellationToken cancellationToken = default)
    {
        if (mesh == null)
            throw new ArgumentNullException(nameof(mesh));
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("File path is required.", nameof(filePath));

        format = format.ToLowerInvariant();
        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        switch (format)
        {
            case "obj":
                var objContent = ObjMeshWriter.Write(mesh);
                await System.IO.File.WriteAllTextAsync(filePath, objContent, cancellationToken);
                break;
            case "gltf":
            case "glb":
                // TODO: Implement glTF/GLB export
                throw new NotImplementedException($"Export format {format} not yet implemented in SDK");
            case "fbx":
                // TODO: Implement FBX export
                throw new NotImplementedException($"Export format {format} not yet implemented in SDK");
            case "usd":
                // TODO: Implement USD export
                throw new NotImplementedException($"Export format {format} not yet implemented in SDK");
            default:
                throw new NotSupportedException($"Unsupported export format: {format}");
        }

        _logger?.LogInformation("Exported mesh to {FilePath} in {Format} format", filePath, format);
    }
}
