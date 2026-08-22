namespace Ashlar.Commercial.GameDomain.Assets;

/// <summary>Animation transition record.</summary>
public sealed record AnimationTransition
{
    /// <summary>From state value.</summary>
    public string FromState { get; init; } = string.Empty;
    /// <summary>To state value.</summary>
    public string ToState { get; init; } = string.Empty;
    /// <summary>Transition duration value.</summary>
    public double TransitionDuration { get; init; } = 0.25;
    /// <summary>Exit time value.</summary>
    public double ExitTime { get; init; } = 1.0;
    /// <summary>Whether s exit time.</summary>
    public bool HasExitTime { get; init; } = true;
    /// <summary>Whether s fixed duration.</summary>
    public bool HasFixedDuration { get; init; } = true;
    /// <summary>Conditions value.</summary>
    public IReadOnlyList<TransitionCondition> Conditions { get; init; } = Array.Empty<TransitionCondition>();
}
