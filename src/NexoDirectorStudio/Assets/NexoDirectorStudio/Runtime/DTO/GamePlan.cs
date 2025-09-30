using System.Text.Json.Serialization;

namespace NexoDirectorStudio.DTO
{
    /// <summary>
    /// Represents the high-level plan for the game slice generated from a DesignBrief.
    /// This is the output of the planning phase and input to the world building phase.
    /// </summary>
    public sealed record GamePlan(
        string Id,
        DesignBrief SourceBrief,
        string Genre,
        string Description,
        IReadOnlyList<string> CoreMechanics,
        IReadOnlyList<string> PlayerExperience,
        int EstimatedDurationMinutes,
        IReadOnlyList<DifficultyBeat> DifficultyProgression,
        IReadOnlyList<string> NarrativeBeats,
        IReadOnlyList<AssetRequirement> RequiredAssets,
        int Seed,
        DateTimeOffset GeneratedAt,
        string Hash)
    {
        public string Id { get; init; } = Id;
        public DesignBrief SourceBrief { get; init; } = SourceBrief;
        public string Genre { get; init; } = Genre;
        public string Description { get; init; } = Description;
        public IReadOnlyList<string> CoreMechanics { get; init; } = CoreMechanics;
        public IReadOnlyList<string> PlayerExperience { get; init; } = PlayerExperience;
        public int EstimatedDurationMinutes { get; init; } = EstimatedDurationMinutes;
        public IReadOnlyList<DifficultyBeat> DifficultyProgression { get; init; } = DifficultyProgression;
        public IReadOnlyList<string> NarrativeBeats { get; init; } = NarrativeBeats;
        public IReadOnlyList<AssetRequirement> RequiredAssets { get; init; } = RequiredAssets;
        public int Seed { get; init; } = Seed;
        public DateTimeOffset GeneratedAt { get; init; } = GeneratedAt;
        public string Hash { get; init; } = Hash;
    }
    
    /// <summary>
    /// Represents a difficulty beat in the game progression.
    /// </summary>
    public sealed record DifficultyBeat(
        float TimeOffsetSeconds,
        int DifficultyLevel,
        string Description);
    
    /// <summary>
    /// Represents an asset requirement for the game slice.
    /// </summary>
    public sealed record AssetRequirement(
        string AssetType,
        string Name,
        string Description,
        bool IsRequired = true,
        int Priority = 3);
}
