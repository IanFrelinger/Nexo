namespace Nexo.GameDomain.Descriptors;

/// <summary>
/// Data-only descriptor for a placeable map element such as a wall, hazard, or pickup.
/// <para>
/// The Unity level editor reads these descriptors to instantiate prefabs with the correct
/// scale, collision layers, material assignments, and interaction scripts.
/// </para>
/// </summary>
public sealed record MapElementDescriptor
{
    /// <summary>Stable identifier used to reference this element across sessions.</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>Human-readable display name shown in the editor palette.</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Functional category that drives placement rules and collision layers.
    /// Accepted values: <c>"structure"</c>, <c>"hazard"</c>, <c>"pickup"</c>,
    /// <c>"spawn"</c>, <c>"trigger"</c>.
    /// </summary>
    public string Category { get; init; } = "structure";

    /// <summary>Bounding-box dimensions in world units.</summary>
    public Dimensions Dimensions { get; init; } = new();

    /// <summary>
    /// Material class that controls physics and rendering properties.
    /// Accepted values: <c>"metal"</c>, <c>"stone"</c>, <c>"wood"</c>,
    /// <c>"organic"</c>, <c>"energy"</c>.
    /// </summary>
    public string MaterialClass { get; init; } = "stone";

    /// <summary>Whether players or projectiles can interact with this element at runtime.</summary>
    public bool IsInteractive { get; init; }

    /// <summary>Free-form tags for filtering, searching, and macro targeting.</summary>
    public IReadOnlyList<string> Tags { get; init; } = [];
}
