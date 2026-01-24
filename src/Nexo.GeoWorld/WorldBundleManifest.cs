using Nexo.GeoTerrain;

namespace Nexo.GeoWorld;

/// <summary>
/// Describes an exported world bundle and its artifacts.
/// JSON is intended to be stable and engine-friendly (Unity-first).
/// </summary>
public sealed record WorldBundleManifest
{
    public required string Version { get; init; }
    public required DateTimeOffset CreatedAtUtc { get; init; }

    public required GeoBounds Bounds { get; init; }
    public required GeoPoint Origin { get; init; }

    /// <summary>
    /// Meters per world unit (Unity default is 1).
    /// </summary>
    public float MetersPerUnit { get; init; } = 1.0f;

    /// <summary>
    /// Coordinate frame / projection information describing how lat/lon inputs were mapped into local meters.
    /// Optional for backwards compatibility; Unity importer can ignore it.
    /// </summary>
    public WorldCoordinateFrame? CoordinateFrame { get; init; }

    public required IReadOnlyList<WorldArtifact> Artifacts { get; init; }
    public IReadOnlyDictionary<string, string>? ProviderProvenance { get; init; }
}

public sealed record WorldCoordinateFrame
{
    /// <summary>
    /// Projection identifier, e.g. "local_equirectangular", "webmercator", "utm", "auto".
    /// </summary>
    public required string ProjectionId { get; init; }

    /// <summary>
    /// Optional projection parameters (stringly-typed to keep schema stable across engines).
    /// Example: { "utmZone":"10", "hemisphere":"N" }.
    /// </summary>
    public IReadOnlyDictionary<string, string>? Parameters { get; init; }
}

public sealed record WorldArtifact
{
    public required string Kind { get; init; } // terrain|buildings|roads|water|instances|materials|...
    public required string Path { get; init; } // relative path within bundle
    public string? ContentType { get; init; }  // obj|json|geojson|...
    public WorldArtifactMetadata? Metadata { get; init; }
}

public sealed record WorldArtifactMetadata
{
    /// <summary>
    /// Optional stable ID for cross-references (e.g. parent/child LOD).
    /// </summary>
    public string? Id { get; init; }

    public string? ParentId { get; init; }

    /// <summary>
    /// Generic LOD marker; interpretation depends on artifact kind (e.g. grid factor or budget tier index).
    /// </summary>
    public int? LodLevel { get; init; }

    /// <summary>
    /// For grid-derived artifacts (terrain), describes the source sample window.
    /// </summary>
    public WorldGridChunkRef? GridChunk { get; init; }

    public int? VertexCount { get; init; }
    public int? TriangleCount { get; init; }
    public int? InstanceCount { get; init; }

    /// <summary>
    /// Axis-aligned bounds in local meters for the exported artifact.
    /// </summary>
    public WorldAabb? LocalBoundsMeters { get; init; }
}

public sealed record WorldGridChunkRef
{
    public required int X0 { get; init; }
    public required int Y0 { get; init; }
    public required int Width { get; init; }
    public required int Height { get; init; }
}

public sealed record WorldAabb
{
    public required float MinX { get; init; }
    public required float MinY { get; init; }
    public required float MinZ { get; init; }
    public required float MaxX { get; init; }
    public required float MaxY { get; init; }
    public required float MaxZ { get; init; }
}

