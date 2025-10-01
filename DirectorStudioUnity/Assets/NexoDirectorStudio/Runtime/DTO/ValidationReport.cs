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
    
    /// <summary>
    /// Represents a validation issue.
    /// </summary>
    public sealed record ValidationIssue
    {
        /// <summary>
        /// Unique identifier for this issue.
        /// </summary>
        public string Id { get; init; } = Guid.NewGuid().ToString();
        
        /// <summary>
        /// Type of the issue.
        /// </summary>
        public string IssueType { get; init; } = "";
        
        /// <summary>
        /// Category of the issue.
        /// </summary>
        public string Category { get; init; } = "";
        
        /// <summary>
        /// Severity of the issue.
        /// </summary>
        public ValidationSeverity Severity { get; init; } = ValidationSeverity.Warning;
        
        /// <summary>
        /// Title of the issue.
        /// </summary>
        public string Title { get; init; } = "";
        
        /// <summary>
        /// Description of the issue.
        /// </summary>
        public string Description { get; init; } = "";
        
        /// <summary>
        /// Location where the issue was found.
        /// </summary>
        public string Location { get; init; } = "";
        
        /// <summary>
        /// Suggested fix for the issue.
        /// </summary>
        public string SuggestedFix { get; init; } = "";
        
        /// <summary>
        /// Whether the issue can be auto-fixed.
        /// </summary>
        public bool CanAutoFix { get; init; }
    }
    
    /// <summary>
    /// Severity levels for validation issues.
    /// </summary>
    public enum ValidationSeverity
    {
        /// <summary>
        /// Information only, not an issue.
        /// </summary>
        Info = 0,
        
        /// <summary>
        /// Minor issue that doesn't prevent functionality.
        /// </summary>
        Warning = 1,
        
        /// <summary>
        /// Major issue that may affect functionality.
        /// </summary>
        Error = 2,
        
        /// <summary>
        /// Critical issue that prevents functionality.
        /// </summary>
        Critical = 3
    }
    
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
