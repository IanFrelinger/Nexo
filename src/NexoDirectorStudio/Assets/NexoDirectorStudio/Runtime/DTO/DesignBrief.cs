using System.Text.Json.Serialization;

namespace NexoDirectorStudio.DTO
{
    /// <summary>
    /// Represents a natural language brief describing the desired game slice.
    /// This is the primary input from the Director (non-programmer user).
    /// </summary>
    public sealed record DesignBrief
    {
        /// <summary>
        /// The natural language description of the game slice.
        /// </summary>
        public string Description { get; init; } = string.Empty;
        
        /// <summary>
        /// Optional genre hint to help with auto-detection.
        /// </summary>
        public string? GenreHint { get; init; }
        
        /// <summary>
        /// Target duration in minutes for the game slice.
        /// </summary>
        public int TargetDurationMinutes { get; init; } = 5;
        
        /// <summary>
        /// Difficulty level (1-5 scale).
        /// </summary>
        public int DifficultyLevel { get; init; } = 3;
        
        /// <summary>
        /// Additional constraints or requirements.
        /// </summary>
        public string? Constraints { get; init; }
        
        /// <summary>
        /// Seed for deterministic generation.
        /// </summary>
        public int Seed { get; init; }
        
        /// <summary>
        /// Timestamp when the brief was created.
        /// </summary>
        public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
        
        /// <summary>
        /// Version of the brief for tracking changes.
        /// </summary>
        public int Version { get; init; } = 1;
    }
}
