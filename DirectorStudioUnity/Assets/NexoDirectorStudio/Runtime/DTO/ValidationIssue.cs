using System;

namespace NexoDirectorStudio.DTO
{
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
}
