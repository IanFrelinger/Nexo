namespace Ashlar.Commercial.GameDomain.Assets;
/// <summary>
/// Data-only descriptor for a level or scene: root objects, lighting, and navigation areas.
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
