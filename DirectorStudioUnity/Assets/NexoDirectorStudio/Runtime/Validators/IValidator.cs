using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
        Task<ValidationResult> ValidateAsync(object input, CancellationToken ct);
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
        Task<ValidationResult> ValidateAsync(T input, CancellationToken ct);
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
    
    
}
