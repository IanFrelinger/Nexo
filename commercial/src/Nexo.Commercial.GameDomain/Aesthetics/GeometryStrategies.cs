namespace Nexo.Commercial.GameDomain.Aesthetics;
/// <summary>
/// Known <see cref="AestheticPack.GeometryStrategy"/> string identifiers for validation and tooling.
/// </summary>
public static class GeometryStrategies
{
    /// <summary>Constant value for voxel.</summary>
    public const string Voxel = "voxel";
    /// <summary>Constant value for low poly.</summary>
    public const string LowPoly = "low_poly";
    /// <summary>Constant value for pixel art.</summary>
    public const string PixelArt = "pixel_art";
    /// <summary>Constant value for pbr.</summary>
    public const string Pbr = "pbr";
    /// <summary>Constant value for wireframe.</summary>
    public const string Wireframe = "wireframe";
    /// <summary>Constant value for sketch.</summary>
    public const string Sketch = "sketch";

    /// <summary>Stable set used by <see cref="AestheticPackValidation"/>.</summary>
    public static IReadOnlySet<string> All { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        Voxel, LowPoly, PixelArt, Pbr, Wireframe, Sketch
    };
}
