namespace Ashlar.Commercial.GameDomain.Assets;

/// <summary>Sound event record.</summary>
public sealed record SoundEvent
{
    /// <summary>Event name value.</summary>
    public string EventName { get; init; } = string.Empty;
    /// <summary>audio descriptor ids value.</summary>
    public IReadOnlyList<string> AudioDescriptorIds { get; init; } = Array.Empty<string>();
    /// <summary>Selection mode value.</summary>
    public string SelectionMode { get; init; } = "random";
    /// <summary>Cooldown seconds value.</summary>
    public double CooldownSeconds { get; init; } = 0.0;
    /// <summary>Max concurrent value.</summary>
    public int MaxConcurrent { get; init; } = 3;
    /// <summary>Volume multiplier value.</summary>
    public double VolumeMultiplier { get; init; } = 1.0;
    /// <summary>Pitch multiplier value.</summary>
    public double PitchMultiplier { get; init; } = 1.0;
}
