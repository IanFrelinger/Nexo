namespace Nexo.Orchestration.Assets.Ports;

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
    public required string Prompt { get; init; }
    public AudioType Type { get; init; } = AudioType.SoundEffect;
    public int DurationSeconds { get; init; } = 10;
    public string? Genre { get; init; }
    public int? Bpm { get; init; }
}

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
    public required string Text { get; init; }
    public required string VoiceId { get; init; }
    public double Speed { get; init; } = 1.0;
    public double Pitch { get; init; } = 1.0;
}

/// <summary>
/// Generated audio result.
/// 
/// Contains:
/// - File path where the audio is stored
/// - MIME type and duration in milliseconds
/// - Optional sample rate
/// - Optional metadata dictionary
/// 
/// Returned by IAudioGenerator after successful generation.
/// </summary>
public sealed record GeneratedAudio
{
    public required string FilePath { get; init; }
    public required string MimeType { get; init; }
    public required int DurationMs { get; init; }
    public int? SampleRate { get; init; }
    public IReadOnlyDictionary<string, object> Metadata { get; init; } =
        new Dictionary<string, object>();
}

/// <summary>
/// Types of audio that can be generated.
/// 
/// Defines audio categories:
/// - SoundEffect: Short audio clips
/// - Music: Background music
/// - Ambient: Ambient soundscapes
/// - Voice: Speech synthesis
/// 
/// Used by IAudioGenerator to specify audio type.
/// </summary>
public enum AudioType
{
    SoundEffect,
    Music,
    Ambient,
    Voice
}

