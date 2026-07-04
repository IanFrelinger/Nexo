using Nexo.Core.Application.Environments;

namespace Nexo.Commercial.GameDomain.Environments;
/// <summary>
/// Describes a multi-resolution voxel environment suitable for streaming game worlds—conceptually
/// a 3D analogue of a tiled map pyramid: each <see cref="LodTiers"/> entry is one zoom level with
/// explicit voxel pitch and chunk shape. The Unity (or other) host reads this at the adaptation
/// boundary to decide which layers to generate, cache, or swap while immutable gameplay rules stay
/// in the Nexo adaptation/core split.
/// </summary>
public sealed record VoxelEnvironmentManifest
{
    /// <summary>Schema version for forward-compatible deserialization.</summary>
    public string SchemaVersion { get; init; } = "1.0";

    /// <summary>Stable identifier for this environment definition.</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>Optional human-readable label.</summary>
    public string DisplayName { get; init; } = string.Empty;

    /// <summary>
    /// Coordinate reference for geographic sources (e.g. <c>EPSG:4326</c>). Omit when using a
    /// purely engine-local frame described by <see cref="OriginWorldX"/> / origin fields.
    /// </summary>
    public string? SpatialReferenceId { get; init; }

    /// <summary>Optional WGS84 origin for GIS-derived environments.</summary>
    public double? OriginLatitude { get; init; }

    /// <summary>Optional WGS84 origin for GIS-derived environments.</summary>
    public double? OriginLongitude { get; init; }

    /// <summary>Engine world-space origin X (meters or native units).</summary>
    public double OriginWorldX { get; init; }

    /// <summary>Engine world-space origin Y.</summary>
    public double OriginWorldY { get; init; }

    /// <summary>Engine world-space origin Z.</summary>
    public double OriginWorldZ { get; init; }

    /// <summary>
    /// Ordered from finest to coarsest tier (index <c>0</c> = highest spatial resolution).
    /// </summary>
    public IReadOnlyList<VoxelLodTier> LodTiers { get; init; } = [];

    /// <summary>
    /// Host-specific hints at the adaptation boundary (e.g. Unity prefab root, brick id, material
    /// palette key). Keys are conventional strings; values are opaque to Nexo core.
    /// </summary>
    public IReadOnlyDictionary<string, string> AdaptationHints { get; init; }
        = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Optional binding for vector map data (OSM extracts, GeoJSON services). When null, hosts pull
    /// from defaults or generate offline. Use <see cref="MapDataSourceBinding.BaseUrl"/> for a
    /// self-hosted Overpass mirror or internal GIS API.
    /// </summary>
    public MapDataSourceBinding? VectorData { get; init; }

    /// <summary>
    /// Optional binding for terrain / elevation (DEM, AWS Terrain Tiles, or self-hosted terrain API).
    /// </summary>
    public MapDataSourceBinding? TerrainData { get; init; }

    /// <summary>
    /// Optional binding for prebuilt voxel chunk storage (HTTP tile store, S3, local path via
    /// <see cref="MapDataSourceKinds.File"/> options).
    /// </summary>
    public MapDataSourceBinding? VoxelChunkStore { get; init; }

/// <summary>
/// Optional stages for embedded AI / verification (vector cleanup, material prompts, tiered QA).
/// Host reads flags and invokes vector/material/verification services when registered.
/// </summary>
    public MapIntelligenceOptions? Intelligence { get; init; }
}
