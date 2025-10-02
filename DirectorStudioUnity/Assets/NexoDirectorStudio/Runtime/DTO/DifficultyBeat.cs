namespace NexoDirectorStudio.DTO
{
    /// <summary>
    /// Represents a difficulty beat in the game progression.
    /// </summary>
    public sealed record DifficultyBeat(
        float TimeOffsetSeconds,
        int DifficultyLevel,
        string Description);
}
