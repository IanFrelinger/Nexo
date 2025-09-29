using System.Text.Json.Serialization;

namespace NexoDirectorStudio.DTO
{
    /// <summary>
    /// Represents the high-level plan for the game slice generated from a DesignBrief.
    /// This is the output of the planning phase and input to the world building phase.
    /// </summary>
    public sealed record GamePlan
    {
        /// <summary>
        /// Unique identifier for this game plan.
        /// </summary>
        public string Id { get; init; } = Guid.NewGuid().ToString();
        
        /// <summary>
        /// The original design brief that generated this plan.
        /// </summary>
        public DesignBrief SourceBrief { get; init; } = new();
        
        /// <summary>
        /// Detected or selected genre for this plan.
        /// </summary>
        public string Genre { get; init; } = string.Empty;
        
        /// <summary>
        /// High-level description of the game slice.
        /// </summary>
        public string Description { get; init; } = string.Empty;
        
        /// <summary>
        /// Core mechanics that will be featured in this slice.
        /// </summary>
        public IReadOnlyList<string> CoreMechanics { get; init; } = Array.Empty<string>();
        
        /// <summary>
        /// Target player experience and emotional beats.
        /// </summary>
        public IReadOnlyList<string> PlayerExperience { get; init; } = Array.Empty<string>();
        
        /// <summary>
        /// Estimated duration in minutes.
        /// </summary>
        public int EstimatedDurationMinutes { get; init; }
        
        /// <summary>
        /// Difficulty progression throughout the slice.
        /// </summary>
        public IReadOnlyList<DifficultyBeat> DifficultyProgression { get; init; } = Array.Empty<DifficultyBeat>();
        
        /// <summary>
        /// Key narrative beats or objectives.
        /// </summary>
        public IReadOnlyList<string> NarrativeBeats { get; init; } = Array.Empty<string>();
        
        /// <summary>
        /// Required assets and their types.
        /// </summary>
        public IReadOnlyList<AssetRequirement> RequiredAssets { get; init; } = Array.Empty<AssetRequirement>();
        
        /// <summary>
        /// Seed used for deterministic generation.
        /// </summary>
        public int Seed { get; init; }
        
        /// <summary>
        /// Timestamp when the plan was generated.
        /// </summary>
        public DateTimeOffset GeneratedAt { get; init; } = DateTimeOffset.UtcNow;
        
        /// <summary>
        /// Hash of the plan for deterministic validation.
        /// </summary>
        public string Hash { get; init; } = string.Empty;
    }
    
    /// <summary>
    /// Represents a difficulty beat in the game progression.
    /// </summary>
    public sealed record DifficultyBeat
    {
        /// <summary>
        /// Time offset in seconds from the start.
        /// </summary>
        public float TimeOffsetSeconds { get; init; }
        
        /// <summary>
        /// Difficulty level (1-5 scale).
        /// </summary>
        public int DifficultyLevel { get; init; }
        
        /// <summary>
        /// Description of what makes this moment challenging.
        /// </summary>
        public string Description { get; init; } = string.Empty;
    }
    
    /// <summary>
    /// Represents an asset requirement for the game slice.
    /// </summary>
    public sealed record AssetRequirement
    {
        /// <summary>
        /// Type of asset (e.g., "Texture", "Audio", "Prefab", "Script").
        /// </summary>
        public string AssetType { get; init; } = string.Empty;
        
        /// <summary>
        /// Name or identifier for the asset.
        /// </summary>
        public string Name { get; init; } = string.Empty;
        
        /// <summary>
        /// Description of the asset's purpose.
        /// </summary>
        public string Description { get; init; } = string.Empty;
        
        /// <summary>
        /// Whether this asset is required or optional.
        /// </summary>
        public bool IsRequired { get; init; } = true;
        
        /// <summary>
        /// Priority level (1-5, higher is more important).
        /// </summary>
        public int Priority { get; init; } = 3;
    }
}
