namespace Ashlar.Commercial.GameDomain.Assets;
/// <summary>
/// Data-only descriptor for an instantiable prefab or entity template: components, child hierarchy,
/// and transform values.
/// </summary>
public sealed record PrefabDescriptor
{
    /// <summary>Stable identifier for this prefab.</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>Human-readable display name.</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>Components attached to this game object.</summary>
    public IReadOnlyList<ComponentEntry> Components { get; init; } = [];

    /// <summary>Child game objects forming the prefab hierarchy.</summary>
    public IReadOnlyList<PrefabDescriptor> Children { get; init; } = [];

    /// <summary>Local position in world units.</summary>
    public Vector3Descriptor Position { get; init; } = new();

    /// <summary>Local Euler rotation in degrees.</summary>
    public Vector3Descriptor Rotation { get; init; } = new();

    /// <summary>Local scale.</summary>
    public Vector3Descriptor Scale { get; init; } = new(1, 1, 1);
}
