namespace Nexo.Orchestration.Assets.Ports;

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
    /// <summary>Path to the generated audio file.</summary>
    public required string FilePath { get; init; }

    /// <summary>MIME type of the audio file.</summary>
    public required string MimeType { get; init; }

    /// <summary>Duration of the audio in milliseconds.</summary>
    public required int DurationMs { get; init; }

    /// <summary>Sample rate in hertz, if known.</summary>
    public int? SampleRate { get; init; }

    /// <summary>Additional generation metadata.</summary>
    public IReadOnlyDictionary<string, object> Metadata { get; init; } =
        new Dictionary<string, object>();
}
