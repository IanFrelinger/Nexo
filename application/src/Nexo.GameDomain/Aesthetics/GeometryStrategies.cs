namespace Nexo.GameDomain.Aesthetics;

/// <summary>
/// Known <see cref="AestheticPack.GeometryStrategy"/> string identifiers for validation and tooling.
/// </summary>
public static class GeometryStrategies
{
    public const string Voxel = "voxel";
    public const string LowPoly = "low_poly";
    public const string PixelArt = "pixel_art";
    public const string Pbr = "pbr";
    public const string Wireframe = "wireframe";
    public const string Sketch = "sketch";

    /// <summary>Stable set used by <see cref="AestheticPackValidation"/>.</summary>
    public static IReadOnlySet<string> All { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        Voxel, LowPoly, PixelArt, Pbr, Wireframe, Sketch
    };
}
