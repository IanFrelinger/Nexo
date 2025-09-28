using System;
using System.Collections.Generic;
using System.Linq;

namespace Nexo.Shared.Validation
{
    /// <summary>
    /// Centralized validation result
    /// </summary>
    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public List<ValidationError> Errors { get; set; } = new();
        public List<ValidationError> Warnings { get; set; } = new();

        /// <summary>
        /// Gets all error messages
        /// </summary>
        public string AllErrors => string.Join("; ", Errors.Select(e => e.ErrorMessage));

        /// <summary>
        /// Gets all warning messages
        /// </summary>
        public string AllWarnings => string.Join("; ", Warnings.Select(w => w.ErrorMessage));

        /// <summary>
        /// Gets errors by severity
        /// </summary>
        public List<ValidationError> GetErrorsBySeverity(ValidationSeverity severity)
        {
            return Errors.Where(e => e.Severity == severity).ToList();
        }
    }

    /// <summary>
    /// Validation error with severity
    /// </summary>
    public class ValidationError
    {
        public string PropertyName { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;
        public ValidationSeverity Severity { get; set; } = ValidationSeverity.Error;
        public string? Code { get; set; }
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
