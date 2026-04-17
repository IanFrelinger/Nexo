namespace Nexo.GameDomain.Assets;

/// <summary>
/// Groups related audio descriptors into a sound bank with playback
/// rules. Maps game events to audio responses. For example, a weapon
/// sound bank maps "fire", "reload", "empty_click", "equip" events
/// to specific AudioDescriptor IDs with randomization and cooldown.
/// </summary>
public sealed record SoundBankDescriptor
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Category { get; init; } = "weapon";

    public IReadOnlyList<SoundEvent> Events { get; init; } = Array.Empty<SoundEvent>();
    public IReadOnlyList<string> Tags { get; init; } = Array.Empty<string>();
}

public sealed record SoundEvent
{
    public string EventName { get; init; } = string.Empty;
    public IReadOnlyList<string> AudioDescriptorIds { get; init; } = Array.Empty<string>();
    public string SelectionMode { get; init; } = "random";
    public double CooldownSeconds { get; init; } = 0.0;
    public int MaxConcurrent { get; init; } = 3;
    public double VolumeMultiplier { get; init; } = 1.0;
    public double PitchMultiplier { get; init; } = 1.0;
}
