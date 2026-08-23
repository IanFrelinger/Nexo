namespace Ashlar.Commercial.GameDomain.Assets;

/// <summary>Animation parameter record.</summary>
public sealed record AnimationParameter
{
    /// <summary>Name value.</summary>
    public string Name { get; init; } = string.Empty;
    /// <summary>Type value.</summary>
    public string Type { get; init; } = "float";
    /// <summary>Default value value.</summary>
    public double DefaultValue { get; init; }
}
