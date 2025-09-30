using NexoDirectorStudio.DTO;
using NexoDirectorStudio.Validators;

namespace NexoDirectorStudio.Profiles
{
    /// <summary>
    /// Interface for genre-specific profiles that define rules, budgets, and validation criteria.
    /// Each genre has its own profile that configures the Director Studio pipeline.
    /// </summary>
    public interface IGenreProfile
    {
        /// <summary>
        /// Unique identifier for this genre profile.
        /// </summary>
        string Id { get; }
        
        /// <summary>
        /// Human-readable name of the genre.
        /// </summary>
        string Name { get; }
        
        /// <summary>
        /// Description of the genre and its characteristics.
        /// </summary>
        string Description { get; }
        
        /// <summary>
        /// Keywords that help identify this genre from brief descriptions.
        /// </summary>
        IReadOnlyList<string> Keywords { get; }
        
        /// <summary>
        /// Core mechanics that are essential for this genre.
        /// </summary>
        IReadOnlyList<string> CoreMechanics { get; }
        
        /// <summary>
        /// Performance budgets for this genre.
        /// </summary>
        PerformanceBudgets PerformanceBudgets { get; }
        
        /// <summary>
        /// Pacing configuration for this genre.
        /// </summary>
        PacingConfiguration PacingConfiguration { get; }
        
        /// <summary>
        /// Accessibility defaults for this genre.
        /// </summary>
        AccessibilityDefaults AccessibilityDefaults { get; }
        
        /// <summary>
        /// Validators that should be used for this genre.
        /// </summary>
        IReadOnlyList<string> RequiredValidators { get; }
        
        /// <summary>
        /// Asset requirements specific to this genre.
        /// </summary>
        IReadOnlyList<AssetRequirement> AssetRequirements { get; }
        
        /// <summary>
        /// Difficulty progression model for this genre.
        /// </summary>
        DifficultyProgressionModel DifficultyProgression { get; }
        
        /// <summary>
        /// Detects if a design brief matches this genre profile.
        /// </summary>
        /// <param name="brief">The design brief to analyze</param>
        /// <returns>Confidence score (0-100) that this brief matches the genre</returns>
        int DetectGenre(DesignBrief brief);
        
        /// <summary>
        /// Gets genre-specific suggestions for improving a game plan.
        /// </summary>
        /// <param name="plan">The game plan to analyze</param>
        /// <returns>List of genre-specific suggestions</returns>
        IReadOnlyList<ValidationSuggestion> GetSuggestions(GamePlan plan);
        
        /// <summary>
        /// Validates that a game plan conforms to this genre's requirements.
        /// </summary>
        /// <param name="plan">The game plan to validate</param>
        /// <returns>Validation result specific to this genre</returns>
        ValidationResult ValidateGenreRequirements(GamePlan plan);
    }
    
    /// <summary>
    /// Performance budgets for a genre.
    /// </summary>
    public sealed record PerformanceBudgets
    {
        /// <summary>
        /// Maximum number of triangles per scene.
        /// </summary>
        public int MaxTriangles { get; init; } = 100000;
        
        /// <summary>
        /// Maximum number of draw calls per frame.
        /// </summary>
        public int MaxDrawCalls { get; init; } = 100;
        
        /// <summary>
        /// Maximum texture memory usage in MB.
        /// </summary>
        public int MaxTextureMemoryMB { get; init; } = 512;
        
        /// <summary>
        /// Maximum audio memory usage in MB.
        /// </summary>
        public int MaxAudioMemoryMB { get; init; } = 128;
        
        /// <summary>
        /// Maximum number of active lights.
        /// </summary>
        public int MaxActiveLights { get; init; } = 8;
        
        /// <summary>
        /// Maximum number of particles.
        /// </summary>
        public int MaxParticles { get; init; } = 1000;
        
        /// <summary>
        /// Maximum number of physics objects.
        /// </summary>
        public int MaxPhysicsObjects { get; init; } = 100;
        
        /// <summary>
        /// Maximum number of AI agents.
        /// </summary>
        public int MaxAIAgents { get; init; } = 20;
    }
    
    /// <summary>
    /// Pacing configuration for a genre.
    /// </summary>
    public sealed record PacingConfiguration
    {
        /// <summary>
        /// Target beats per minute for this genre.
        /// </summary>
        public float TargetBPM { get; init; } = 120.0f;
        
        /// <summary>
        /// Minimum time between major events in seconds.
        /// </summary>
        public float MinEventInterval { get; init; } = 5.0f;
        
        /// <summary>
        /// Maximum time between major events in seconds.
        /// </summary>
        public float MaxEventInterval { get; init; } = 30.0f;
        
        /// <summary>
        /// Target interaction density (interactions per minute).
        /// </summary>
        public float TargetInteractionDensity { get; init; } = 10.0f;
        
        /// <summary>
        /// Breathing room ratio (quiet time / active time).
        /// </summary>
        public float BreathingRoomRatio { get; init; } = 0.3f;
        
        /// <summary>
        /// Difficulty ramp rate (difficulty increase per minute).
        /// </summary>
        public float DifficultyRampRate { get; init; } = 0.1f;
        
        /// <summary>
        /// Pacing curve type (linear, exponential, logarithmic).
        /// </summary>
        public string PacingCurveType { get; init; } = "linear";
    }
    
    /// <summary>
    /// Accessibility defaults for a genre.
    /// </summary>
    public sealed record AccessibilityDefaults
    {
        /// <summary>
        /// Default color contrast ratio.
        /// </summary>
        public float ColorContrastRatio { get; init; } = 4.5f;
        
        /// <summary>
        /// Default text size multiplier.
        /// </summary>
        public float TextSizeMultiplier { get; init; } = 1.0f;
        
        /// <summary>
        /// Whether motion can be reduced.
        /// </summary>
        public bool CanReduceMotion { get; init; } = true;
        
        /// <summary>
        /// Whether audio can be substituted with visual cues.
        /// </summary>
        public bool CanSubstituteAudio { get; init; } = true;
        
        /// <summary>
        /// Whether color can be substituted with other indicators.
        /// </summary>
        public bool CanSubstituteColor { get; init; } = true;
        
        /// <summary>
        /// Default difficulty options available.
        /// </summary>
        public IReadOnlyList<string> DifficultyOptions { get; init; } = new[] { "Easy", "Normal", "Hard" };
        
        /// <summary>
        /// Whether the genre supports one-handed play.
        /// </summary>
        public bool SupportsOneHandedPlay { get; init; } = false;
    }
    
    /// <summary>
    /// Difficulty progression model for a genre.
    /// </summary>
    public sealed record DifficultyProgressionModel
    {
        /// <summary>
        /// Starting difficulty level (1-5).
        /// </summary>
        public int StartingDifficulty { get; init; } = 2;
        
        /// <summary>
        /// Peak difficulty level (1-5).
        /// </summary>
        public int PeakDifficulty { get; init; } = 4;
        
        /// <summary>
        /// Time to reach peak difficulty in minutes.
        /// </summary>
        public float TimeToPeakMinutes { get; init; } = 5.0f;
        
        /// <summary>
        /// Difficulty curve type (linear, exponential, logarithmic).
        /// </summary>
        public string DifficultyCurveType { get; init; } = "linear";
        
        /// <summary>
        /// Whether difficulty can decrease during gameplay.
        /// </summary>
        public bool AllowDifficultyDecrease { get; init; } = false;
        
        /// <summary>
        /// Minimum time between difficulty changes in seconds.
        /// </summary>
        public float MinDifficultyChangeInterval { get; init; } = 30.0f;
    }
}
