namespace Ashlar.Commercial.GameDomain.Assets;

/// <summary>Quality config record.</summary>
public sealed record QualityConfig
{
    /// <summary>V sync count value.</summary>
    public int VSyncCount { get; init; } = 1; // 0=off, 1=every vblank, 2=every other
    /// <summary>Target frame rate value.</summary>
    public int TargetFrameRate { get; init; } = -1; // -1=platform default
    /// <summary>Shadow quality value.</summary>
    public string ShadowQuality { get; init; } = "all"; // disable, hard_only, all
    /// <summary>Shadow resolution value.</summary>
    public int ShadowResolution { get; init; } = 2048;
    /// <summary>Shadow distance value.</summary>
    public double ShadowDistance { get; init; } = 150.0;
    /// <summary>Anti aliasing value.</summary>
    public string AntiAliasing { get; init; } = "4x"; // disabled, 2x, 4x, 8x
    /// <summary>Texture quality value.</summary>
    public string TextureQuality { get; init; } = "full"; // full, half, quarter, eighth
    /// <summary>Max lod value.</summary>
    public int MaxLod { get; init; } = 0; // 0=highest, 1, 2, etc.
}
