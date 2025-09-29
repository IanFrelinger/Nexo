using NexoDirectorStudio.DTO;

namespace NexoDirectorStudio.Validators
{
    /// <summary>
    /// Base interface for all validators in the Director Studio validation suite.
    /// </summary>
    public interface IValidator
    {
        /// <summary>
        /// Validates the given input and returns a validation result.
        /// </summary>
        /// <param name="input">The input to validate</param>
        /// <param name="ct">Cancellation token</param>
        /// <returns>Validation result</returns>
        ValueTask<ValidationResult> ValidateAsync(object input, CancellationToken ct);
    }
    
    /// <summary>
    /// Generic validator interface for type-safe validation.
    /// </summary>
    /// <typeparam name="T">The type of input to validate</typeparam>
    public interface IValidator<T> : IValidator
    {
        /// <summary>
        /// Validates the given input and returns a validation result.
        /// </summary>
        /// <param name="input">The input to validate</param>
        /// <param name="ct">Cancellation token</param>
        /// <returns>Validation result</returns>
        ValueTask<ValidationResult> ValidateAsync(T input, CancellationToken ct);
    }
    
    /// <summary>
    /// Represents the result of a validation operation.
    /// </summary>
    public sealed record ValidationResult
    {
        /// <summary>
        /// Whether the validation passed.
        /// </summary>
        public bool IsValid { get; init; }
        
        /// <summary>
        /// Validation score (0-100, where 100 is perfect).
        /// </summary>
        public int Score { get; init; }
        
        /// <summary>
        /// Human-readable message describing the validation result.
        /// </summary>
        public string Message { get; init; } = string.Empty;
        
        /// <summary>
        /// Detailed validation report.
        /// </summary>
        public string Details { get; init; } = string.Empty;
        
        /// <summary>
        /// List of issues found during validation.
        /// </summary>
        public IReadOnlyList<ValidationIssue> Issues { get; init; } = Array.Empty<ValidationIssue>();
        
        /// <summary>
        /// List of suggestions for improvement.
        /// </summary>
        public IReadOnlyList<ValidationSuggestion> Suggestions { get; init; } = Array.Empty<ValidationSuggestion>();
        
        /// <summary>
        /// Timestamp when the validation was performed.
        /// </summary>
        public DateTimeOffset ValidatedAt { get; init; } = DateTimeOffset.UtcNow;
    }
    
    /// <summary>
    /// Represents a validation issue found during validation.
    /// </summary>
    public sealed record ValidationIssue
    {
        /// <summary>
        /// Unique identifier for the issue.
        /// </summary>
        public string Id { get; init; } = Guid.NewGuid().ToString();
        
        /// <summary>
        /// Severity level of the issue.
        /// </summary>
        public ValidationSeverity Severity { get; init; }
        
        /// <summary>
        /// Category of the issue.
        /// </summary>
        public string Category { get; init; } = string.Empty;
        
        /// <summary>
        /// Short description of the issue.
        /// </summary>
        public string Title { get; init; } = string.Empty;
        
        /// <summary>
        /// Detailed description of the issue.
        /// </summary>
        public string Description { get; init; } = string.Empty;
        
        /// <summary>
        /// Location where the issue was found.
        /// </summary>
        public string Location { get; init; } = string.Empty;
        
        /// <summary>
        /// Suggested fix for the issue.
        /// </summary>
        public string SuggestedFix { get; init; } = string.Empty;
    }
    
    /// <summary>
    /// Represents a validation suggestion for improvement.
    /// </summary>
    public sealed record ValidationSuggestion
    {
        /// <summary>
        /// Unique identifier for the suggestion.
        /// </summary>
        public string Id { get; init; } = Guid.NewGuid().ToString();
        
        /// <summary>
        /// Category of the suggestion.
        /// </summary>
        public string Category { get; init; } = string.Empty;
        
        /// <summary>
        /// Short description of the suggestion.
        /// </summary>
        public string Title { get; init; } = string.Empty;
        
        /// <summary>
        /// Detailed description of the suggestion.
        /// </summary>
        public string Description { get; init; } = string.Empty;
        
        /// <summary>
        /// Priority of the suggestion (1-5, where 5 is highest).
        /// </summary>
        public int Priority { get; init; } = 3;
        
        /// <summary>
        /// Estimated effort to implement the suggestion.
        /// </summary>
        public string Effort { get; init; } = string.Empty;
    }
    
    /// <summary>
    /// Severity levels for validation issues.
    /// </summary>
    public enum ValidationSeverity
    {
        /// <summary>
        /// Information only, not an issue.
        /// </summary>
        Info,
        
        /// <summary>
        /// Minor issue that doesn't prevent functionality.
        /// </summary>
        Warning,
        
        /// <summary>
        /// Major issue that may affect functionality.
        /// </summary>
        Error,
        
        /// <summary>
        /// Critical issue that prevents functionality.
        /// </summary>
        Critical
    }
}
