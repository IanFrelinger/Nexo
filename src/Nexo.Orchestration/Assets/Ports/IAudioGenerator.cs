namespace Nexo.Orchestration.Assets.Ports;

/// <summary>
/// Port for audio generation services.
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
/// </summary>
public enum AudioType
{
    SoundEffect,
    Music,
    Ambient,
    Voice
}

