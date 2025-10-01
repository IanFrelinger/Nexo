using NexoDirectorStudio.DTO;

namespace NexoDirectorStudio.Profiles
{
    /// <summary>
    /// Configuration data for RPG genre profile
    /// </summary>
    public static class RPGConfiguration
    {
        public static PerformanceBudgets PerformanceBudgets => new()
        {
            MaxTriangles = 120000, // Moderate geometry for detailed environments
            MaxDrawCalls = 100, // Moderate draw calls
            MaxTextureMemoryMB = 768, // High texture quality for detailed environments
            MaxAudioMemoryMB = 512, // Rich audio for dialogue and music
            MaxActiveLights = 10, // Atmospheric lighting
            MaxParticles = 800, // Moderate particles for effects
            MaxPhysicsObjects = 150, // Many interactive objects
            MaxAIAgents = 25 // Many NPCs and enemies
        };
        
        public static PacingConfiguration PacingConfiguration => new()
        {
            TargetBPM = 100.0f, // Lower intensity, more thoughtful
            MinEventInterval = 15.0f, // Longer quiet periods
            MaxEventInterval = 45.0f, // Much longer quiet periods
            TargetInteractionDensity = 5.0f, // Lower interaction rate
            BreathingRoomRatio = 0.6f, // More quiet time for exploration
            DifficultyRampRate = 0.05f, // Very gradual difficulty curve
            PacingCurveType = "logarithmic" // Slow, steady progression
        };
        
        public static AccessibilityDefaults AccessibilityDefaults => new()
        {
            ColorContrastRatio = 4.5f, // Standard contrast
            TextSizeMultiplier = 1.3f, // Larger text for dialogue
            CanReduceMotion = true, // Motion can be reduced
            CanSubstituteAudio = true, // Audio cues can be visual
            CanSubstituteColor = true, // Color can be substituted
            DifficultyOptions = new[] { "Story", "Normal", "Hard", "Expert" },
            SupportsOneHandedPlay = true // Can be played with one hand
        };
        
        public static DifficultyProgressionModel DifficultyProgression => new()
        {
            StartingDifficulty = 1, // Start very easy
            PeakDifficulty = 3, // Moderate peak difficulty
            TimeToPeakMinutes = 15.0f, // Very gradual ramp-up
            DifficultyCurveType = "logarithmic", // Slow progression
            AllowDifficultyDecrease = true, // Can have easier sections
            MinDifficultyChangeInterval = 120.0f // Very slow changes
        };
        
        public static readonly string[] Keywords = new[]
        {
            "rpg", "role-playing", "character", "story", "quest", "level", "experience", "skill",
            "inventory", "dialogue", "narrative", "adventure", "fantasy", "medieval", "magic"
        };
        
        public static readonly string[] CoreMechanics = new[]
        {
            "Talk", "Quest", "Level Up", "Inventory", "Combat", "Explore", "Craft", "Trade", "Dialogue"
        };
        
        public static readonly string[] RequiredValidators = new[]
        {
            "PlayabilityValidator", "MechanicsValidator", "PerformanceValidator", "PacingValidator"
        };
    }
}
