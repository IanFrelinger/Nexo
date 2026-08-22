namespace Ashlar.Commercial.GameDomain.Assets;

/// <summary>Animation event record.</summary>
public sealed record AnimationEvent
{
    /// <summary>Time value.</summary>
    public double Time { get; init; }
    /// <summary>Function name value.</summary>
    public string FunctionName { get; init; } = string.Empty;
    /// <summary>String parameter value.</summary>
    public string? StringParameter { get; init; }
    /// <summary>Float parameter value.</summary>
    public double FloatParameter { get; init; }
    /// <summary>Int parameter value.</summary>
    public int IntParameter { get; init; }
}
