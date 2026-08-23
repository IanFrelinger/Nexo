namespace Ashlar.Orchestration.Assets.Ports;

/// <summary>
/// Request for audio generation.
/// 
/// Contains:
/// - Text prompt for audio generation
/// - Audio type (music, sound effect, ambient)
/// - Duration in seconds
/// - Optional genre and BPM
/// 
/// Used by IAudioGenerator to generate audio.
/// </summary>
public sealed record AudioGenerationRequest
{
    /// <summary>Text prompt describing the desired audio.</summary>
    public required string Prompt { get; init; }

    /// <summary>Kind of audio to generate.</summary>
    public AudioType Type { get; init; } = AudioType.SoundEffect;

    /// <summary>Target duration in seconds.</summary>
    public int DurationSeconds { get; init; } = 10;

    /// <summary>Optional musical genre hint.</summary>
    public string? Genre { get; init; }

    /// <summary>Beats per minute, for music generation.</summary>
    public int? Bpm { get; init; }
}
