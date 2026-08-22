namespace Ashlar.Orchestration.Assets.Ports;

/// <summary>
/// Port for audio generation services.
/// 
/// Defines the contract for audio generation adapters:
/// - Generate audio (music, sound effects) from text prompts
/// - Generate speech from text (TTS)
/// - Support multiple audio types and parameters
/// 
/// Implementations (BarkAudioGenerator, ElevenLabsAudioGenerator, etc.) provide
/// specific audio generation logic. Used by AudioAssetAgent.
/// </summary>
public interface IAudioGenerator
{
    /// <summary>
    /// Generates audio (music or sound effects) from a text prompt.
    /// </summary>
    Task<GeneratedAudio> GenerateAsync(
        AudioGenerationRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates speech from text.
    /// </summary>
    Task<GeneratedAudio> GenerateSpeechAsync(
        SpeechGenerationRequest request,
        CancellationToken cancellationToken = default);
}
