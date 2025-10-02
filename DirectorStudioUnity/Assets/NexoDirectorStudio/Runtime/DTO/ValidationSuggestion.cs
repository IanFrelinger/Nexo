using System;

namespace NexoDirectorStudio.DTO
{
    /// <summary>
    /// Represents a validation suggestion.
    /// </summary>
    public sealed record ValidationSuggestion
    {
        /// <summary>
        /// Unique identifier for this suggestion.
        /// </summary>
        public string Id { get; init; } = Guid.NewGuid().ToString();
        
        /// <summary>
        /// Category of the suggestion.
        /// </summary>
        public string Category { get; init; } = "";
        
        /// <summary>
        /// Title of the suggestion.
        /// </summary>
        public string Title { get; init; } = "";
        
        /// <summary>
        /// Description of the suggestion.
        /// </summary>
        public string Description { get; init; } = "";
        
        /// <summary>
        /// Priority of the suggestion (1-5).
        /// </summary>
        public int Priority { get; init; } = 3;
        
        /// <summary>
        /// Estimated effort to implement the suggestion.
        /// </summary>
        public string Effort { get; init; } = "";
        
        /// <summary>
        /// Location where the suggestion applies.
        /// </summary>
        public string Location { get; init; } = "";
    }
}
