namespace Nexo.GameDomain.Assets;

/// <summary>
/// Data-only descriptor for a Unity UI canvas and its child elements.
/// </summary>
public sealed record UIDescriptor
{
    /// <summary>Stable identifier for this UI layout.</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>Human-readable display name.</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Canvas render mode.
    /// Accepted values: <c>"overlay"</c>, <c>"camera"</c>, <c>"worldspace"</c>.
    /// </summary>
    public string CanvasMode { get; init; } = "overlay";

    /// <summary>UI elements contained in this canvas.</summary>
    public IReadOnlyList<UIElement> Elements { get; init; } = [];
}

/// <summary>
/// A single element within a <see cref="UIDescriptor"/> canvas.
/// </summary>
public sealed record UIElement
{
    /// <summary>
    /// Element type.
    /// Accepted values: <c>"text"</c>, <c>"button"</c>, <c>"image"</c>,
    /// <c>"panel"</c>, <c>"slider"</c>, <c>"input"</c>.
    /// </summary>
    public string Type { get; init; } = "panel";

    /// <summary>Element name used for runtime lookup.</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>Anchored position within the canvas.</summary>
    public Vector3Descriptor Position { get; init; } = new();

    /// <summary>Width and height of the element.</summary>
    public Vector2Descriptor Size { get; init; } = new();

    /// <summary>Type-specific properties (e.g. text content, sprite path, font size).</summary>
    public IReadOnlyDictionary<string, object> Properties { get; init; } =
        new Dictionary<string, object>();
}
