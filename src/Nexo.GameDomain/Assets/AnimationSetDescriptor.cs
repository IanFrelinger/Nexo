namespace Nexo.GameDomain.Assets;

/// <summary>
/// Groups related animations into a set for a character or weapon.
/// Maps gameplay states to animation descriptors. For example, an
/// FPS character animation set maps idle, walk, run, jump, crouch,
/// slide, ADS, fire, reload to specific AnimationDescriptor IDs.
/// </summary>
public sealed record AnimationSetDescriptor
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Category { get; init; } = "character";

    public IReadOnlyList<AnimationMapping> Mappings { get; init; } = Array.Empty<AnimationMapping>();
    public IReadOnlyList<BlendTree> BlendTrees { get; init; } = Array.Empty<BlendTree>();
    public IReadOnlyList<string> Tags { get; init; } = Array.Empty<string>();
}

public sealed record AnimationMapping
{
    public string GameplayState { get; init; } = string.Empty;
    public string AnimationDescriptorId { get; init; } = string.Empty;
    public double CrossfadeDuration { get; init; } = 0.15;
}

public sealed record BlendTree
{
    public string Name { get; init; } = string.Empty;
    public string BlendParameter { get; init; } = string.Empty;
    public string? BlendParameterY { get; init; }
    public string BlendType { get; init; } = "1D";
    public IReadOnlyList<BlendTreeChild> Children { get; init; } = Array.Empty<BlendTreeChild>();
}

public sealed record BlendTreeChild
{
    public string AnimationDescriptorId { get; init; } = string.Empty;
    public double Threshold { get; init; }
    public double ThresholdY { get; init; }
    public double TimeScale { get; init; } = 1.0;
}
