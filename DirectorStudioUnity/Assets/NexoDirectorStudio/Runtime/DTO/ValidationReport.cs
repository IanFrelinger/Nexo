using System;
using System.Collections.Generic;

namespace NexoDirectorStudio.DTO
{
    /// <summary>
    /// Represents a validation report for a game slice.
    /// </summary>
    public sealed record ValidationReport
    {
        /// <summary>
        /// Unique identifier for this report.
        /// </summary>
        public string Id { get; init; } = Guid.NewGuid().ToString();
        
        /// <summary>
        /// ID of the content bundle being validated.
        /// </summary>
        public string ContentBundleId { get; init; } = "";
        
        /// <summary>
        /// Overall validation status.
        /// </summary>
        public bool OverallPassed { get; init; }
        
        /// <summary>
        /// Overall validation score (0-100).
        /// </summary>
        public int OverallScore { get; init; }
        
        /// <summary>
        /// List of validation issues found.
        /// </summary>
        public IReadOnlyList<ValidationIssue> Issues { get; init; } = Array.Empty<ValidationIssue>();
        
        /// <summary>
        /// List of validation suggestions.
        /// </summary>
        public IReadOnlyList<ValidationSuggestion> Suggestions { get; init; } = Array.Empty<ValidationSuggestion>();
        
        /// <summary>
        /// Timestamp when the validation was performed.
        /// </summary>
        public DateTime ValidatedAt { get; init; } = DateTime.UtcNow;
    }
}