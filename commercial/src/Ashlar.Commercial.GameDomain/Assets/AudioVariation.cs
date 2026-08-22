namespace Ashlar.Commercial.GameDomain.Assets;

/// <summary>Audio variation record.</summary>
public sealed record AudioVariation
{
    /// <summary>variant id value.</summary>
    public string VariantId { get; init; } = string.Empty;
    /// <summary>Pitch offset value.</summary>
    public double PitchOffset { get; init; }
    /// <summary>Volume offset value.</summary>
    public double VolumeOffset { get; init; }
    /// <summary>Clip suffix value.</summary>
    public string? ClipSuffix { get; init; }
}
