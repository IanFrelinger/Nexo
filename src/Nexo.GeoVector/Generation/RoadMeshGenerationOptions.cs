using Nexo.GeoTerrain;

namespace Nexo.GeoVector.Generation;

public sealed record RoadMeshGenerationOptions
{
    public float DefaultWidthMeters { get; init; } = 4.0f;
    public bool ConformToTerrain { get; init; } = true;
    public bool TreatNoDataAsZero { get; init; } = true;

    /// <summary>
    /// If true, detect intersections between road segments and create simple junction patches.
    /// </summary>
    public bool EnableIntersections { get; init; } = true;

    /// <summary>
    /// Junction disc radius. If 0, a default derived from width is used.
    /// </summary>
    public float JunctionRadiusMeters { get; init; }

    public bool GenerateTexCoords { get; init; } = true;
    public float UvMetersPerRepeat { get; init; } = 1.0f;

    public float? ConstantHeightMeters { get; init; }
}

