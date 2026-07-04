namespace Nexo.Commercial.GameDomain.Assets;
/// <summary>
/// Groups related audio descriptors into a sound bank with playback
/// rules. Maps game events to audio responses. For example, a weapon
/// sound bank maps "fire", "reload", "empty_click", "equip" events
/// to specific AudioDescriptor IDs with randomization and cooldown.
/// </summary>
public sealed record SoundBankDescriptor
{
    /// <summary>id value.</summary>
    public string Id { get; init; } = string.Empty;
    /// <summary>Name value.</summary>
    public string Name { get; init; } = string.Empty;
    /// <summary>Category value.</summary>
    public string Category { get; init; } = "weapon";

    /// <summary>Events value.</summary>
    public IReadOnlyList<SoundEvent> Events { get; init; } = Array.Empty<SoundEvent>();
    /// <summary>Tags value.</summary>
    public IReadOnlyList<string> Tags { get; init; } = Array.Empty<string>();
}
