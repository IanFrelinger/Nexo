using System.ComponentModel.DataAnnotations;

namespace Nexo.API.Models;

/// <summary>
/// Request to generate terrain mesh.
/// </summary>
public class TerrainGenerationRequest
{
    /// <summary>
    /// Geographic bounds as "minLat,maxLat,minLon,maxLon"
    /// </summary>
    [Required]
    public string Bounds { get; set; } = string.Empty;

    /// <summary>
    /// Elevation data provider (srtm, mapbox-terrain-rgb, local, geotiff, ascii-grid)
    /// </summary>
    public string? ElevationProvider { get; set; }

    /// <summary>
    /// Output format (obj, gltf, glb, fbx, usd)
    /// </summary>
    public string Format { get; set; } = "obj";

    /// <summary>
    /// Mapbox access token (required for mapbox providers)
    /// </summary>
    public string? MapboxToken { get; set; }

    /// <summary>
    /// Local file path (for local/geotiff/ascii-grid providers)
    /// </summary>
    public string? LocalPath { get; set; }

    /// <summary>
    /// Mesh generation options
    /// </summary>
    public MeshOptions? MeshOptions { get; set; }

    /// <summary>
    /// Optional webhook URL for job completion notifications
    /// </summary>
    public string? WebhookUrl { get; set; }

    /// <summary>
    /// Directory root for tile cache (saves downloaded tiles to disk to avoid re-downloading)
    /// </summary>
    public string? CacheRoot { get; set; }

    /// <summary>
    /// Enable disk caching (default: true)
    /// </summary>
    public bool? PersistCache { get; set; }
}

/// <summary>
/// Request to extract vector features.
/// </summary>
public class VectorExtractionRequest
{
    /// <summary>
    /// Geographic bounds as "minLat,maxLat,minLon,maxLon"
    /// </summary>
    [Required]
    public string Bounds { get; set; } = string.Empty;

    /// <summary>
    /// Vector data provider (osm, mapbox, geojson, shapefile, hybrid)
    /// </summary>
    public string? VectorProvider { get; set; }

    /// <summary>
    /// Feature kind (building, road, water, vegetation, railway, etc.)
    /// </summary>
    [Required]
    public string FeatureKind { get; set; } = "building";

    /// <summary>
    /// Output format (json, geojson)
    /// </summary>
    public string Format { get; set; } = "json";

    /// <summary>
    /// Mapbox access token (required for mapbox provider)
    /// </summary>
    public string? MapboxToken { get; set; }

    /// <summary>
    /// OSM PBF file path (for osm/hybrid providers)
    /// </summary>
    public string? OsmPbfPath { get; set; }

    /// <summary>
    /// GeoJSON/Shapefile path (for geojson/shapefile providers)
    /// </summary>
    public string? VectorFilePath { get; set; }

    /// <summary>
    /// Optional webhook URL for job completion notifications
    /// </summary>
    public string? WebhookUrl { get; set; }

    /// <summary>
    /// Directory root for tile cache (saves downloaded tiles to disk to avoid re-downloading)
    /// </summary>
    public string? CacheRoot { get; set; }

    /// <summary>
    /// Enable disk caching (default: true)
    /// </summary>
    public bool? PersistCache { get; set; }
}

/// <summary>
/// Request to generate world bundle.
/// </summary>
public class WorldGenerationRequest
{
    /// <summary>
    /// Geographic bounds as "minLat,maxLat,minLon,maxLon"
    /// </summary>
    [Required]
    public string Bounds { get; set; } = string.Empty;

    /// <summary>
    /// Elevation data provider
    /// </summary>
    public string? ElevationProvider { get; set; }

    /// <summary>
    /// Vector data provider
    /// </summary>
    public string? VectorProvider { get; set; }

    /// <summary>
    /// Output format (obj, gltf, glb)
    /// </summary>
    public string Format { get; set; } = "obj";

    /// <summary>
    /// Mapbox access token
    /// </summary>
    public string? MapboxToken { get; set; }

    /// <summary>
    /// OSM PBF file path
    /// </summary>
    public string? OsmPbfPath { get; set; }

    /// <summary>
    /// World generation options
    /// </summary>
    public WorldOptions? WorldOptions { get; set; }

    /// <summary>
    /// Optional webhook URL for job completion notifications
    /// </summary>
    public string? WebhookUrl { get; set; }

    /// <summary>
    /// Directory root for tile cache (saves downloaded tiles to disk to avoid re-downloading)
    /// </summary>
    public string? CacheRoot { get; set; }

    /// <summary>
    /// Enable disk caching (default: true)
    /// </summary>
    public bool? PersistCache { get; set; }
}

/// <summary>
/// Request to validate world bundle.
/// </summary>
public class WorldValidationRequest
{
    /// <summary>
    /// Path to world bundle directory
    /// </summary>
    [Required]
    public string BundlePath { get; set; } = string.Empty;
}

/// <summary>
/// Mesh generation options.
/// </summary>
public class MeshOptions
{
    /// <summary>
    /// Treat no-data values as zero
    /// </summary>
    public bool TreatNoDataAsZero { get; set; } = false;

    /// <summary>
    /// Vertical scale factor
    /// </summary>
    public float VerticalScale { get; set; } = 1.0f;

    /// <summary>
    /// Generate vertex normals
    /// </summary>
    public bool GenerateNormals { get; set; } = true;
}

/// <summary>
/// World generation options.
/// </summary>
public class WorldOptions
{
    /// <summary>
    /// Enable terrain chunking
    /// </summary>
    public bool ChunkTerrain { get; set; } = true;

    /// <summary>
    /// Terrain chunk size in meters
    /// </summary>
    public float ChunkSizeMeters { get; set; } = 1000.0f;

    /// <summary>
    /// Enable vegetation placement
    /// </summary>
    public bool EnableVegetation { get; set; } = true;
}

/// <summary>
/// Job submission response.
/// </summary>
public class JobResponse
{
    /// <summary>
    /// Job identifier
    /// </summary>
    public required string JobId { get; init; }

    /// <summary>
    /// Job status
    /// </summary>
    public required string Status { get; init; }
}

/// <summary>
/// Job status response.
/// </summary>
public sealed record JobStatusResponse
{
    /// <summary>
    /// Job identifier
    /// </summary>
    public required string JobId { get; init; }

    /// <summary>
    /// Job status (pending, processing, completed, failed)
    /// </summary>
    public required string Status { get; init; }

    /// <summary>
    /// Progress percentage (0-100)
    /// </summary>
    public int Progress { get; init; }

    /// <summary>
    /// Error message if failed
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Output file path if completed
    /// </summary>
    public string? OutputPath { get; init; }

    /// <summary>
    /// Job creation timestamp
    /// </summary>
    public DateTime CreatedAt { get; init; }

    /// <summary>
    /// Job completion timestamp
    /// </summary>
    public DateTime? CompletedAt { get; init; }
}

/// <summary>
/// Validation response.
/// </summary>
public class ValidationResponse
{
    /// <summary>
    /// Whether validation passed
    /// </summary>
    public required bool IsValid { get; init; }

    /// <summary>
    /// List of validation issues
    /// </summary>
    public required IReadOnlyList<string> Issues { get; init; }
}

/// <summary>
/// Error response.
/// </summary>
public class ErrorResponse
{
    /// <summary>
    /// Error message
    /// </summary>
    public required string Message { get; init; }
}

/// <summary>
/// Request to validate mesh quality.
/// </summary>
public class MeshValidationRequest
{
    /// <summary>
    /// Path to mesh file
    /// </summary>
    [Required]
    public string MeshPath { get; set; } = string.Empty;

    /// <summary>
    /// Optional path to source elevation grid for accuracy validation
    /// </summary>
    public string? SourceGridPath { get; set; }
}

/// <summary>
/// Request to validate data integrity.
/// </summary>
public class IntegrityValidationRequest
{
    /// <summary>
    /// Geographic bounds as "minLat,minLon,maxLat,maxLon"
    /// </summary>
    [Required]
    public string Bounds { get; set; } = string.Empty;
}
