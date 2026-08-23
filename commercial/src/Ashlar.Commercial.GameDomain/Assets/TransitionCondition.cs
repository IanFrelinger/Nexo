namespace Ashlar.Commercial.GameDomain.Assets;

/// <summary>Transition condition record.</summary>
public sealed record TransitionCondition
{
    /// <summary>Parameter name value.</summary>
    public string ParameterName { get; init; } = string.Empty;
    /// <summary>Mode value.</summary>
    public string Mode { get; init; } = "equals";
    /// <summary>Threshold value.</summary>
    public double Threshold { get; init; }
}
