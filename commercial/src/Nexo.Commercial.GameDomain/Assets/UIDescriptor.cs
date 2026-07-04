namespace Nexo.Commercial.GameDomain.Assets;
/// <summary>
/// Data-only descriptor for a screen-space or world-space UI canvas and its child elements.
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
