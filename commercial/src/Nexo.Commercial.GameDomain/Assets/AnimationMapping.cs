namespace Nexo.Commercial.GameDomain.Assets;

/// <summary>Animation mapping record.</summary>
public sealed record AnimationMapping
{
    /// <summary>Gameplay state value.</summary>
    public string GameplayState { get; init; } = string.Empty;
    /// <summary>animation descriptor id value.</summary>
    public string AnimationDescriptorId { get; init; } = string.Empty;
    /// <summary>Crossfade duration value.</summary>
    public double CrossfadeDuration { get; init; } = 0.15;
}
