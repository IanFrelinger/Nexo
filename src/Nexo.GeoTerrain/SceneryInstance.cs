namespace Nexo.GeoTerrain;

/// <summary>
/// A lightweight instance placement for a scenery asset (e.g., a tree prefab).
/// Coordinates are in local meters, consistent with mesh generation.
/// </summary>
public sealed record SceneryInstance
{
    public required string Kind { get; init; }
    public required Vector3 Position { get; init; }
    public float YawDegrees { get; init; }
    public float UniformScale { get; init; } = 1.0f;
}

