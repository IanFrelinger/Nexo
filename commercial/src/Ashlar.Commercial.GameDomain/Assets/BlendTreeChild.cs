namespace Ashlar.Commercial.GameDomain.Assets;

/// <summary>Blend tree child record.</summary>
public sealed record BlendTreeChild
{
    /// <summary>animation descriptor id value.</summary>
    public string AnimationDescriptorId { get; init; } = string.Empty;
    /// <summary>Threshold value.</summary>
    public double Threshold { get; init; }
    /// <summary>Threshold y value.</summary>
    public double ThresholdY { get; init; }
    /// <summary>Time scale value.</summary>
    public double TimeScale { get; init; } = 1.0;
}
