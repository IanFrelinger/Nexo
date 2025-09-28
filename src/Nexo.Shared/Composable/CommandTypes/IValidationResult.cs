using System;
using System.Collections.Generic;

namespace Nexo.Shared.Composable.CommandTypes
{
    /// <summary>
    /// Interface for validation results
    /// </summary>
    public interface IValidationResult
    {
        bool IsValid { get; }
        IReadOnlyList<IValidationError> Errors { get; }
        IReadOnlyList<IValidationWarning> Warnings { get; }
        IReadOnlyDictionary<string, object> Metadata { get; }
        DateTime ValidatedAt { get; }
    }
    
    /// <summary>
    /// Default implementation of validation result
    /// </summary>
    public class ValidationResult : IValidationResult
    {
        public bool IsValid { get; }
        public IReadOnlyList<IValidationError> Errors { get; }
        public IReadOnlyList<IValidationWarning> Warnings { get; }
        public IReadOnlyDictionary<string, object> Metadata { get; }
        public DateTime ValidatedAt { get; }
        
        public ValidationResult(
            bool isValid,
            IReadOnlyList<IValidationError>? errors = null,
            IReadOnlyList<IValidationWarning>? warnings = null,
            IReadOnlyDictionary<string, object>? metadata = null)
        {
            IsValid = isValid;
            Errors = errors ?? new List<IValidationError>();
            Warnings = warnings ?? new List<IValidationWarning>();
            Metadata = metadata ?? new Dictionary<string, object>();
            ValidatedAt = DateTime.UtcNow;
        }
        
        public static IValidationResult Valid() => new ValidationResult(true);
        
        public static IValidationResult Invalid(params IValidationError[] errors) => 
            new ValidationResult(false, errors);
            
        public static IValidationResult WithWarnings(params IValidationWarning[] warnings) => 
            new ValidationResult(true, warnings: warnings);
    }
}
