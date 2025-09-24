using System.Collections.Generic;

namespace Nexo.Feature.AudioGeneration.Models;

/// <summary>
/// Request for audio generation
/// </summary>
public record AudioGenerationRequest
{
    /// <summary>
    /// Text prompt describing the audio to generate
    /// </summary>
    public string Prompt { get; init; } = string.Empty;
    
    /// <summary>
    /// Type of audio to generate
    /// </summary>
    public AudioType AudioType { get; init; } = AudioType.SoundEffect;
    
    /// <summary>
    /// Duration in seconds
    /// </summary>
    public float Duration { get; init; } = 5.0f;
    
    /// <summary>
    /// Sample rate (Hz)
    /// </summary>
    public int SampleRate { get; init; } = 44100;
    
    /// <summary>
    /// Number of channels (1 = mono, 2 = stereo)
    /// </summary>
    public int Channels { get; init; } = 2;
    
    /// <summary>
    /// Audio style or genre
    /// </summary>
    public string? Style { get; init; }
    
    /// <summary>
    /// Volume level (0.0 to 1.0)
    /// </summary>
    public float Volume { get; init; } = 0.8f;
    
    /// <summary>
    /// Additional parameters specific to the provider
    /// </summary>
    public Dictionary<string, object> Parameters { get; init; } = new();
}

/// <summary>
/// Result of audio generation
/// </summary>
public record AudioGenerationResult
{
    /// <summary>
    /// Whether the generation was successful
    /// </summary>
    public bool Success { get; init; }
    
    /// <summary>
    /// Generated audio data as byte array
    /// </summary>
    public byte[]? AudioData { get; init; }
    
    /// <summary>
    /// File path where audio was saved
    /// </summary>
    public string? FilePath { get; init; }
    
    /// <summary>
    /// Audio format (WAV, MP3, OGG, etc.)
    /// </summary>
    public string Format { get; init; } = "WAV";
    
    /// <summary>
    /// Duration of generated audio
    /// </summary>
    public float Duration { get; init; }
    
    /// <summary>
    /// Sample rate of generated audio
    /// </summary>
    public int SampleRate { get; init; }
    
    /// <summary>
    /// Number of channels
    /// </summary>
    public int Channels { get; init; }
    
    /// <summary>
    /// Error message if generation failed
    /// </summary>
    public string? Error { get; init; }
    
    /// <summary>
    /// Generation time in milliseconds
    /// </summary>
    public long GenerationTimeMs { get; init; }
}

/// <summary>
/// Types of audio that can be generated
/// </summary>
public enum AudioType
{
    /// <summary>
    /// Sound effects (gunshots, explosions, etc.)
    /// </summary>
    SoundEffect,
    
    /// <summary>
    /// Background music
    /// </summary>
    Music,
    
    /// <summary>
    /// Voice synthesis
    /// </summary>
    Voice,
    
    /// <summary>
    /// Ambient sounds
    /// </summary>
    Ambient,
    
    /// <summary>
    /// UI sounds (clicks, notifications, etc.)
    /// </summary>
    UI,
    
    /// <summary>
    /// Environmental audio (wind, rain, etc.)
    /// </summary>
    Environmental
}

/// <summary>
/// Audio generation model information
/// </summary>
public record AudioGenerationModel
{
    /// <summary>
    /// Unique identifier for the model
    /// </summary>
    public string Id { get; init; } = string.Empty;
    
    /// <summary>
    /// Human-readable name
    /// </summary>
    public string Name { get; init; } = string.Empty;
    
    /// <summary>
    /// Description of the model
    /// </summary>
    public string Description { get; init; } = string.Empty;
    
    /// <summary>
    /// Whether the model is currently available
    /// </summary>
    public bool IsAvailable { get; init; }
    
    /// <summary>
    /// Supported audio types
    /// </summary>
    public AudioType[] SupportedTypes { get; init; } = Array.Empty<AudioType>();
    
    /// <summary>
    /// Maximum duration in seconds
    /// </summary>
    public float MaxDuration { get; init; } = 60.0f;
    
    /// <summary>
    /// Maximum prompt length
    /// </summary>
    public int MaxPromptLength { get; init; } = 1000;
    
    /// <summary>
    /// Model capabilities
    /// </summary>
    public AudioGenerationCapabilities Capabilities { get; init; } = new();
}

/// <summary>
/// Audio generation model capabilities
/// </summary>
public record AudioGenerationCapabilities
{
    /// <summary>
    /// Supports style specification
    /// </summary>
    public bool SupportsStyle { get; init; }
    
    /// <summary>
    /// Supports duration specification
    /// </summary>
    public bool SupportsDuration { get; init; }
    
    /// <summary>
    /// Supports volume control
    /// </summary>
    public bool SupportsVolume { get; init; }
    
    /// <summary>
    /// Supports multiple channels
    /// </summary>
    public bool SupportsMultiChannel { get; init; }
    
    /// <summary>
    /// Supports real-time generation
    /// </summary>
    public bool SupportsRealTime { get; init; }
    
    /// <summary>
    /// Supports batch generation
    /// </summary>
    public bool SupportsBatchGeneration { get; init; }
}
