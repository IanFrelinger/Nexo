namespace Ashlar.Commercial.GameDomain.Assets;

/// <summary>
/// An axis-aligned navigation mesh area within a scene.
/// </summary>
public sealed record NavMeshArea
{
    /// <summary>Area name used for agent type filtering.</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>Centre position of the nav-mesh area.</summary>
    public Vector3Descriptor Center { get; init; } = new();

    /// <summary>Extents of the nav-mesh area.</summary>
    public Vector3Descriptor Size { get; init; } = new();
}
