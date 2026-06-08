namespace Nexo.GameDomain.Assets;

/// <summary>
/// Data-only descriptor for a surface material (shader, colours, texture slots).
/// Host runtimes map these values to engine-specific materials and assets.
/// </summary>
public sealed record MaterialDescriptor
{
    /// <summary>Stable identifier for this material.</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>Human-readable display name.</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>Logical shader identifier or engine-specific shader path (e.g. built-in lit, URP lit).</summary>
    public string ShaderName { get; init; } = "Standard";

    /// <summary>Main colour as a hex string (e.g. "#FFFFFF").</summary>
    public string Color { get; init; } = "#FFFFFF";

    /// <summary>Metallic value in the range [0, 1].</summary>
    public double Metallic { get; init; }

    /// <summary>Smoothness value in the range [0, 1].</summary>
    public double Smoothness { get; init; }

    /// <summary>Optional emission colour as a hex string, or null if emission is disabled.</summary>
    public string? EmissionColor { get; init; }

    /// <summary>
    /// Render mode controlling transparency behaviour.
    /// Accepted values: <c>"Opaque"</c>, <c>"Cutout"</c>, <c>"Transparent"</c>.
    /// </summary>
    public string RenderMode { get; init; } = "Opaque";

    /// <summary>
    /// Mapping from texture slot name (e.g. "_MainTex", "_BumpMap") to texture asset path.
    /// </summary>
    public IReadOnlyDictionary<string, string> TextureSlots { get; init; } =
        new Dictionary<string, string>();
}
