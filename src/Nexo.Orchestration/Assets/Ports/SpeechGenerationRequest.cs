namespace Nexo.Orchestration.Assets.Ports;

/// <summary>
/// Request for speech generation.
/// 
/// Contains:
/// - Text to synthesize
/// - Voice ID to use
/// - Speed and pitch parameters
/// 
/// Used by IAudioGenerator to generate speech.
/// </summary>
public sealed record SpeechGenerationRequest
{
    /// <summary>Text to synthesize into speech.</summary>
    public required string Text { get; init; }

    /// <summary>Voice profile identifier to use.</summary>
    public required string VoiceId { get; init; }

    /// <summary>Playback speed multiplier.</summary>
    public double Speed { get; init; } = 1.0;

    /// <summary>Pitch multiplier for the synthesized voice.</summary>
    public double Pitch { get; init; } = 1.0;
}
