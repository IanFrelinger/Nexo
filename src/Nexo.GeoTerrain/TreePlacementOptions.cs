namespace Nexo.GeoTerrain;

/// <summary>
/// Options for deterministic tree placement.
/// </summary>
public sealed record TreePlacementOptions
{
    /// <summary>
    /// Trees per square kilometer.
    /// </summary>
    public float TreesPerSquareKm { get; init; } = 200.0f;

    public int Seed { get; init; } = 1337;

    public bool TreatNoDataAsZero { get; init; }

    public float MinUniformScale { get; init; } = 0.8f;
    public float MaxUniformScale { get; init; } = 1.3f;
}

