namespace Nexo.GeoWorld.Vegetation;

/// <summary>
/// Deterministic vegetation placement options.
/// </summary>
public sealed record VegetationPlacementOptions
{
    public int Seed { get; init; } = 12345;

    /// <summary>
    /// Overall density (instances per square kilometer).
    /// </summary>
    public float InstancesPerSquareKm { get; init; } = 250f;

    /// <summary>
    /// Minimum allowed distance between instances (meters).
    /// </summary>
    public float MinInstanceSpacingMeters { get; init; } = 2.0f;

    public float MinDistanceToRoadMeters { get; init; } = 2.0f;
    public float MinDistanceToBuildingMeters { get; init; } = 1.0f;
    public float MinDistanceToWaterMeters { get; init; }

    /// <summary>
    /// Reject placements on slopes steeper than this (degrees). Null = no slope rule.
    /// </summary>
    public float? MaxSlopeDegrees { get; init; } = 35f;

    public float MinUniformScale { get; init; } = 0.8f;
    public float MaxUniformScale { get; init; } = 1.2f;

    /// <summary>
    /// Safety cap to avoid huge rejection loops on very large/low-res grids.
    /// </summary>
    public int MaxInstances { get; init; } = 10_000;

    /// <summary>
    /// Safety cap for rejection sampling attempts.
    /// </summary>
    public int MaxAttempts { get; init; } = 200_000;

    /// <summary>
    /// Optional multi-kind placement. If provided, kind and scale will be selected from this list.
    /// </summary>
    public IReadOnlyList<VegetationSpecies>? Species { get; init; }

    /// <summary>
    /// Fallback kind when <see cref="Species"/> is not provided.
    /// </summary>
    public string Kind { get; init; } = "tree";
}

public sealed record VegetationSpecies
{
    public required string Kind { get; init; }
    public float Weight { get; init; } = 1.0f;
    public float MinUniformScale { get; init; } = 0.8f;
    public float MaxUniformScale { get; init; } = 1.2f;
}

