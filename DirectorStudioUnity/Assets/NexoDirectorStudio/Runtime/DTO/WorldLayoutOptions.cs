using System;

namespace NexoDirectorStudio.DTO
{
    /// <summary>
    /// Options for world layout generation.
    /// </summary>
    public sealed record WorldLayoutOptions
    {
        /// <summary>
        /// Maximum number of rooms to generate.
        /// </summary>
        public int MaxRooms { get; init; } = 10;
        
        /// <summary>
        /// Minimum number of rooms to generate.
        /// </summary>
        public int MinRooms { get; init; } = 3;
        
        /// <summary>
        /// Whether to include vertical elements (stairs, platforms).
        /// </summary>
        public bool IncludeVerticalElements { get; init; } = true;
        
        /// <summary>
        /// Whether to include secret areas.
        /// </summary>
        public bool IncludeSecretAreas { get; init; } = false;
        
        /// <summary>
        /// Complexity level (1-5).
        /// </summary>
        public int ComplexityLevel { get; init; } = 3;
        
        /// <summary>
        /// Theme for the world layout.
        /// </summary>
        public string Theme { get; init; } = "default";
    }
}
