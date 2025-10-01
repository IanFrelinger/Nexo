using System;
using System.Collections.Generic;
using System.Threading;
using System;


namespace NexoDirectorStudio.DTO
{
    /// <summary>
    /// Represents a natural language brief describing the desired game slice.
    /// This is the primary input from the Director (non-programmer user).
    /// </summary>
    public sealed record DesignBrief(
        string Description,
        string? GenreHint = null,
        int TargetDurationMinutes = 5,
        int DifficultyLevel = 3,
        string? Constraints = null,
        int Seed = 0,
        DateTimeOffset CreatedAt = default,
        int Version = 1)
    {
        public DateTimeOffset CreatedAt { get; init; } = CreatedAt == default ? DateTimeOffset.UtcNow : CreatedAt;
    }
}
