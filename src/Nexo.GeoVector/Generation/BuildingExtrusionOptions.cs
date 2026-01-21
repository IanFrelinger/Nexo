namespace Nexo.GeoVector.Generation;

/// <summary>
/// Options for extruding building footprints into a 3D mesh.
/// </summary>
public sealed class BuildingExtrusionOptions
{
    public float DefaultHeightMeters { get; init; } = 10.0f;
    public float MinHeightMeters { get; init; } = 1.0f;
    public bool IncludeBottom { get; init; }
}

