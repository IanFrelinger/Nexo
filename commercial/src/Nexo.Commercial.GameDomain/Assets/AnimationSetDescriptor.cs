namespace Nexo.Commercial.GameDomain.Assets;
/// <summary>
/// Groups related animations into a set for a character or weapon.
/// Maps gameplay states to animation descriptors. For example, an
/// FPS character animation set maps idle, walk, run, jump, crouch,
/// slide, ADS, fire, reload to specific AnimationDescriptor IDs.
/// </summary>
public sealed record AnimationSetDescriptor
{
    /// <summary>id value.</summary>
    public string Id { get; init; } = string.Empty;
    /// <summary>Name value.</summary>
    public string Name { get; init; } = string.Empty;
    /// <summary>Category value.</summary>
    public string Category { get; init; } = "character";

    /// <summary>Mappings value.</summary>
    public IReadOnlyList<AnimationMapping> Mappings { get; init; } = Array.Empty<AnimationMapping>();
    /// <summary>Blend trees value.</summary>
    public IReadOnlyList<BlendTree> BlendTrees { get; init; } = Array.Empty<BlendTree>();
    /// <summary>Tags value.</summary>
    public IReadOnlyList<string> Tags { get; init; } = Array.Empty<string>();
}
