namespace Nexo.Commercial.GameDomain.Assets;

/// <summary>Animation state record.</summary>
public sealed record AnimationState
{
    /// <summary>Name value.</summary>
    public string Name { get; init; } = string.Empty;
    /// <summary>Clip name value.</summary>
    public string ClipName { get; init; } = string.Empty;
    /// <summary>Speed value.</summary>
    public double Speed { get; init; } = 1.0;
    /// <summary>Loop value.</summary>
    public bool Loop { get; init; }
    /// <summary>Mirror value.</summary>
    public bool Mirror { get; init; }
    /// <summary>Speed parameter name value.</summary>
    public string? SpeedParameterName { get; init; }
    /// <summary>Events value.</summary>
    public IReadOnlyList<AnimationEvent> Events { get; init; } = Array.Empty<AnimationEvent>();
    /// <summary>Position value.</summary>
    public Vector2Descriptor Position { get; init; } = new();
}
