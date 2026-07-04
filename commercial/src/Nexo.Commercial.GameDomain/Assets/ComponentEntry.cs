namespace Nexo.Commercial.GameDomain.Assets;

/// <summary>
/// A single component attached to a prefab's game object.
/// </summary>
public sealed record ComponentEntry
{
    /// <summary>Component type identifier (often a fully-qualified type name for the host runtime).</summary>
    public string TypeName { get; init; } = string.Empty;

    /// <summary>Serialized field values keyed by property name.</summary>
    public IReadOnlyDictionary<string, object> Properties { get; init; } =
        new Dictionary<string, object>();
}
