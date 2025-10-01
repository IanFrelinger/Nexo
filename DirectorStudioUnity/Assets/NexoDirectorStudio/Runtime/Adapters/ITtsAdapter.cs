using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading;
using System.Threading.Tasks;

namespace NexoDirectorStudio.Adapters
{
    /// <summary>
    /// Interface for Piper TTS adapter.
    /// Provides offline text-to-speech capabilities for game audio.
    /// </summary>
    public interface ITtsAdapter : IAdapter
    {
        /// <summary>
        /// Synthesizes speech from text lines.
        /// </summary>
        /// <param name="lines">Text lines to synthesize</param>
        /// <param name="voice">Voice to use for synthesis</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Generated audio clips</returns>
        Task<AudioClip[]> SynthesizeAsync(string[] lines, string voice, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Synthesizes a single text line to audio.
        /// </summary>
        /// <param name="text">Text to synthesize</param>
        /// <param name="voice">Voice to use for synthesis</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Generated audio clip</returns>
        Task<AudioClip> SynthesizeLineAsync(string text, string voice, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Gets available voices for synthesis.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of available voices</returns>
        Task<IReadOnlyList<Voice>> GetAvailableVoicesAsync(CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Generates audio for game slice dialogue and narration.
        /// </summary>
        /// <param name="dialogue">Dialogue content to synthesize</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Generated audio collection</returns>
        Task<AudioCollection> GenerateGameAudioAsync(DialogueContent dialogue, CancellationToken cancellationToken = default);
    }
    
    /// <summary>
    /// Represents an audio clip.
    /// </summary>
    public sealed record AudioClip
    {
        /// <summary>
        /// Unique identifier for this audio clip.
        /// </summary>
        public string Id { get; init; } = Guid.NewGuid().ToString();
        
        /// <summary>
        /// Name of the audio clip.
        /// </summary>
        public string Name { get; init; } = "";
        
        /// <summary>
        /// Description of the audio clip.
        /// </summary>
        public string Description { get; init; } = "";
        
        /// <summary>
        /// File path to the audio file.
        /// </summary>
        public string FilePath { get; init; } = "";
        
        /// <summary>
        /// Duration of the audio clip in seconds.
        /// </summary>
        public float DurationSeconds { get; init; }
        
        /// <summary>
        /// Sample rate of the audio in Hz.
        /// </summary>
        public int SampleRate { get; init; } = 44100;
        
        /// <summary>
        /// Number of audio channels.
        /// </summary>
        public int Channels { get; init; } = 1;
        
        /// <summary>
        /// Format of the audio file (WAV, MP3, etc.).
        /// </summary>
        public string Format { get; init; } = "WAV";
        
        /// <summary>
        /// Size of the audio file in bytes.
        /// </summary>
        public long FileSizeBytes { get; init; }
        
        /// <summary>
        /// Text that was synthesized.
        /// </summary>
        public string Text { get; init; } = "";
        
        /// <summary>
        /// Voice used for synthesis.
        /// </summary>
        public string Voice { get; init; } = "";
        
        /// <summary>
        /// Timestamp when the clip was generated.
        /// </summary>
        public DateTime GeneratedAt { get; init; } = DateTime.UtcNow;
    }
    
    /// <summary>
    /// Represents a voice for TTS synthesis.
    /// </summary>
    public sealed record Voice
    {
        /// <summary>
        /// Unique identifier for this voice.
        /// </summary>
        public string Id { get; init; } = Guid.NewGuid().ToString();
        
        /// <summary>
        /// Name of the voice.
        /// </summary>
        public string Name { get; init; } = "";
        
        /// <summary>
        /// Description of the voice.
        /// </summary>
        public string Description { get; init; } = "";
        
        /// <summary>
        /// Language of the voice.
        /// </summary>
        public string Language { get; init; } = "en";
        
        /// <summary>
        /// Gender of the voice.
        /// </summary>
        public string Gender { get; init; } = "neutral";
        
        /// <summary>
        /// Age range of the voice.
        /// </summary>
        public string AgeRange { get; init; } = "adult";
        
        /// <summary>
        /// Whether this voice is available.
        /// </summary>
        public bool IsAvailable { get; init; }
        
        /// <summary>
        /// Quality rating of the voice (1-5).
        /// </summary>
        public int QualityRating { get; init; } = 3;
    }
    
    /// <summary>
    /// Represents dialogue content for synthesis.
    /// </summary>
    public sealed record DialogueContent
    {
        /// <summary>
        /// Unique identifier for this dialogue content.
        /// </summary>
        public string Id { get; init; } = Guid.NewGuid().ToString();
        
        /// <summary>
        /// Name of the dialogue content.
        /// </summary>
        public string Name { get; init; } = "";
        
        /// <summary>
        /// Description of the dialogue content.
        /// </summary>
        public string Description { get; init; } = "";
        
        /// <summary>
        /// List of dialogue lines.
        /// </summary>
        public IReadOnlyList<DialogueLine> Lines { get; init; } = Array.Empty<DialogueLine>();
        
        /// <summary>
        /// Total number of lines.
        /// </summary>
        public int TotalLines { get; init; }
        
        /// <summary>
        /// Estimated total duration in seconds.
        /// </summary>
        public float EstimatedDurationSeconds { get; init; }
    }
    
    /// <summary>
    /// Represents a single dialogue line.
    /// </summary>
    public sealed record DialogueLine
    {
        /// <summary>
        /// Unique identifier for this dialogue line.
        /// </summary>
        public string Id { get; init; } = Guid.NewGuid().ToString();
        
        /// <summary>
        /// Text content of the line.
        /// </summary>
        public string Text { get; init; } = "";
        
        /// <summary>
        /// Character speaking the line.
        /// </summary>
        public string Character { get; init; } = "";
        
        /// <summary>
        /// Voice to use for this line.
        /// </summary>
        public string Voice { get; init; } = "";
        
        /// <summary>
        /// Emotion or tone of the line.
        /// </summary>
        public string Emotion { get; init; } = "neutral";
        
        /// <summary>
        /// Priority of this line (1-5).
        /// </summary>
        public int Priority { get; init; } = 3;
    }
    
    /// <summary>
    /// Represents a collection of audio clips.
    /// </summary>
    public sealed record AudioCollection
    {
        /// <summary>
        /// Unique identifier for this collection.
        /// </summary>
        public string Id { get; init; } = Guid.NewGuid().ToString();
        
        /// <summary>
        /// Name of the collection.
        /// </summary>
        public string Name { get; init; } = "";
        
        /// <summary>
        /// Description of the collection.
        /// </summary>
        public string Description { get; init; } = "";
        
        /// <summary>
        /// List of audio clips in this collection.
        /// </summary>
        public IReadOnlyList<AudioClip> AudioClips { get; init; } = Array.Empty<AudioClip>();
        
        /// <summary>
        /// Total number of audio clips.
        /// </summary>
        public int TotalClips { get; init; }
        
        /// <summary>
        /// Total duration of all clips in seconds.
        /// </summary>
        public float TotalDurationSeconds { get; init; }
        
        /// <summary>
        /// Total size of all clips in bytes.
        /// </summary>
        public long TotalSizeBytes { get; init; }
        
        /// <summary>
        /// Timestamp when the collection was generated.
        /// </summary>
        public DateTime GeneratedAt { get; init; } = DateTime.UtcNow;
    }
}
