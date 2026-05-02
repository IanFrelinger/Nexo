namespace Nexo.GameDomain.Assets;

/// <summary>
/// Data-only descriptor for a Unity scene, including root objects, lighting, and navigation areas.
/// </summary>
public sealed record SceneDescriptor
{
    /// <summary>Stable identifier for this scene.</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>Human-readable scene name.</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>Top-level game objects in the scene hierarchy.</summary>
    public IReadOnlyList<PrefabDescriptor> RootObjects { get; init; } = [];

    /// <summary>Ambient light colour as a hex string.</summary>
    public string AmbientLightColor { get; init; } = "#404040";

    /// <summary>Optional skybox material name or path.</summary>
    public string? SkyboxMaterial { get; init; }

    /// <summary>Navigation mesh areas defined in the scene.</summary>
    public IReadOnlyList<NavMeshArea> NavMeshAreas { get; init; } = [];
}

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
