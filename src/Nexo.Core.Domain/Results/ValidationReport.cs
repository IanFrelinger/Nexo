using System;
using System.Collections.Generic;

namespace Nexo.Core.Domain.Results
{
    /// <summary>
    /// Result of validation operations
    /// </summary>
    public class ValidationReport
    {
        /// <summary>
        /// Whether the validation was successful
        /// </summary>
        public bool IsValid { get; set; }
        
        /// <summary>
        /// Error message if validation failed
        /// </summary>
        public string? ErrorMessage { get; set; }
        
        /// <summary>
        /// Exception if validation failed
        /// </summary>
        public Exception? Exception { get; set; }
        
        /// <summary>
        /// Validation errors
        /// </summary>
        public List<ValidationError> Errors { get; set; } = new();
        
        /// <summary>
        /// Validation warnings
        /// </summary>
        public List<ValidationWarning> Warnings { get; set; } = new();
        
        /// <summary>
        /// Validation duration
        /// </summary>
        public TimeSpan Duration { get; set; }
        
        /// <summary>
        /// Success message
        /// </summary>
        public string? SuccessMessage { get; set; }
        
        /// <summary>
        /// Timestamp when validation completed
        /// </summary>
        public DateTime CompletedAt { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// Creates a successful result
        /// </summary>
        public static ValidationReport Success(string? message = null)
        {
            return new ValidationReport
            {
                IsValid = true,
                SuccessMessage = message
            };
        }
        
        /// <summary>
        /// Creates a failed result
        /// </summary>
        public static ValidationReport Failure(string errorMessage, Exception? exception = null)
        {
            return new ValidationReport
            {
                IsValid = false,
                ErrorMessage = errorMessage,
                Exception = exception
            };
        }
        
        /// <summary>
        /// Adds validation error
        /// </summary>
        public void AddError(ValidationError error)
        {
            Errors.Add(error);
            IsValid = false;
        }
        
        /// <summary>
        /// Adds validation warning
        /// </summary>
        public void AddWarning(ValidationWarning warning)
        {
            Warnings.Add(warning);
        }
        
        /// <summary>
        /// Overall validation score
        /// </summary>
        public double OverallScore { get; set; }
    }
    
    /// <summary>
    /// Validation error
    /// </summary>
    public class ValidationError
    {
        /// <summary>
        /// Error message
        /// </summary>
        public string Message { get; set; } = string.Empty;
        
        /// <summary>
        /// Error code
        /// </summary>
        public string Code { get; set; } = string.Empty;
        
        /// <summary>
        /// Field or property that failed validation
        /// </summary>
        public string? Field { get; set; }
        
        /// <summary>
        /// Severity of the error
        /// </summary>
        public ValidationSeverity Severity { get; set; } = ValidationSeverity.Error;
        
        /// <summary>
        /// Additional data
        /// </summary>
        public Dictionary<string, object> Data { get; set; } = new();
    }
    
    /// <summary>
    /// Validation warning
    /// </summary>
    public class ValidationWarning
    {
        /// <summary>
        /// Warning message
        /// </summary>
        public string Message { get; set; } = string.Empty;
        
        /// <summary>
        /// Warning code
        /// </summary>
        public string Code { get; set; } = string.Empty;
        
        /// <summary>
        /// Field or property that generated the warning
        /// </summary>
        public string? Field { get; set; }
        
        /// <summary>
        /// Additional data
        /// </summary>
        public Dictionary<string, object> Data { get; set; } = new();
    }
    
    /// <summary>
    /// Validation severity levels
    /// </summary>
    public enum ValidationSeverity
    {
        Info,
        Warning,
        Error,
        Critical
    }
}
