namespace Ashlar.Commercial.GameDomain.Assets;
/// <summary>
/// Animation asset descriptor: character, weapon, environment, and UI motion.
/// Hosts map this to animation clips, skeletal rigs, and state machine topology.
/// </summary>
public sealed record AnimationDescriptor
{
    /// <summary>id value.</summary>
    public string Id { get; init; } = string.Empty;
    /// <summary>Name value.</summary>
    public string Name { get; init; } = string.Empty;
    /// <summary>Category value.</summary>
    public string Category { get; init; } = "character";

    // State machine
    /// <summary>Default state value.</summary>
    public string DefaultState { get; init; } = "Idle";
    /// <summary>States value.</summary>
    public IReadOnlyList<AnimationState> States { get; init; } = Array.Empty<AnimationState>();
    /// <summary>Transitions value.</summary>
    public IReadOnlyList<AnimationTransition> Transitions { get; init; } = Array.Empty<AnimationTransition>();
    /// <summary>Parameters value.</summary>
    public IReadOnlyList<AnimationParameter> Parameters { get; init; } = Array.Empty<AnimationParameter>();
    /// <summary>Layers value.</summary>
    public IReadOnlyList<AnimationLayer> Layers { get; init; } = Array.Empty<AnimationLayer>();

    /// <summary>Tags value.</summary>
    public IReadOnlyList<string> Tags { get; init; } = Array.Empty<string>();
}
