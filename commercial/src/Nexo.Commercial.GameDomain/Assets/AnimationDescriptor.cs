namespace Nexo.GameDomain.Assets;

/// <summary>
/// Animation asset descriptor: character, weapon, environment, and UI motion.
/// Hosts map this to animation clips, skeletal rigs, and state machine topology.
/// </summary>
public sealed record AnimationDescriptor
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Category { get; init; } = "character";

    // State machine
    public string DefaultState { get; init; } = "Idle";
    public IReadOnlyList<AnimationState> States { get; init; } = Array.Empty<AnimationState>();
    public IReadOnlyList<AnimationTransition> Transitions { get; init; } = Array.Empty<AnimationTransition>();
    public IReadOnlyList<AnimationParameter> Parameters { get; init; } = Array.Empty<AnimationParameter>();
    public IReadOnlyList<AnimationLayer> Layers { get; init; } = Array.Empty<AnimationLayer>();

    public IReadOnlyList<string> Tags { get; init; } = Array.Empty<string>();
}

public sealed record AnimationState
{
    public string Name { get; init; } = string.Empty;
    public string ClipName { get; init; } = string.Empty;
    public double Speed { get; init; } = 1.0;
    public bool Loop { get; init; }
    public bool Mirror { get; init; }
    public string? SpeedParameterName { get; init; }
    public IReadOnlyList<AnimationEvent> Events { get; init; } = Array.Empty<AnimationEvent>();
    public Vector2Descriptor Position { get; init; } = new();
}

public sealed record AnimationTransition
{
    public string FromState { get; init; } = string.Empty;
    public string ToState { get; init; } = string.Empty;
    public double TransitionDuration { get; init; } = 0.25;
    public double ExitTime { get; init; } = 1.0;
    public bool HasExitTime { get; init; } = true;
    public bool HasFixedDuration { get; init; } = true;
    public IReadOnlyList<TransitionCondition> Conditions { get; init; } = Array.Empty<TransitionCondition>();
}

public sealed record TransitionCondition
{
    public string ParameterName { get; init; } = string.Empty;
    public string Mode { get; init; } = "equals";
    public double Threshold { get; init; }
}

public sealed record AnimationParameter
{
    public string Name { get; init; } = string.Empty;
    public string Type { get; init; } = "float";
    public double DefaultValue { get; init; }
}

public sealed record AnimationLayer
{
    public string Name { get; init; } = "Base Layer";
    public double Weight { get; init; } = 1.0;
    public string BlendingMode { get; init; } = "override";
    public string? AvatarMask { get; init; }
    public bool IKPass { get; init; }
}

public sealed record AnimationEvent
{
    public double Time { get; init; }
    public string FunctionName { get; init; } = string.Empty;
    public string? StringParameter { get; init; }
    public double FloatParameter { get; init; }
    public int IntParameter { get; init; }
}
