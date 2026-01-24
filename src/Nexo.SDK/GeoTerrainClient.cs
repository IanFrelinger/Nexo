using Microsoft.Extensions.Logging;
using Nexo.GeoTerrain;
using Nexo.Orchestration.GeoTerrain.Ports;

namespace Nexo.SDK;

/// <summary>
/// Client for terrain generation operations.
/// Provides programmatic access to terrain mesh generation without shelling out to CLI.
/// </summary>
public class GeoTerrainClient
{
    private readonly IElevationProvider _elevationProvider;
    private readonly ILogger<GeoTerrainClient>? _logger;

    /// <summary>
    /// Initializes a new instance of the GeoTerrainClient.
    /// </summary>
    /// <param name="elevationProvider">Elevation data provider</param>
    /// <param name="logger">Optional logger</param>
    public GeoTerrainClient(IElevationProvider elevationProvider, ILogger<GeoTerrainClient>? logger = null)
    {
        _elevationProvider = elevationProvider ?? throw new ArgumentNullException(nameof(elevationProvider));
        _logger = logger;
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
