using System;
using System.Collections.Generic;
using System.Linq;

namespace Nexo.Core.Domain.Composition
{
    /// <summary>
    /// Represents the result of a validation operation, containing any errors, warnings, and overall validation status.
    /// </summary>
    public class ValidationResult
    {
        private readonly List<ValidationError> _errors = new();
        private readonly List<ValidationWarning> _warnings = new();
        
        /// <summary>
        /// Gets whether the validation was successful (no errors).
        /// </summary>
        public bool IsValid => !_errors.Any();
        
        /// <summary>
        /// Gets whether the validation has any warnings.
        /// </summary>
        public bool HasWarnings => _warnings.Any();
        
        /// <summary>
        /// Gets all validation errors.
        /// </summary>
        public IReadOnlyList<ValidationError> Errors => _errors.AsReadOnly();
        
        /// <summary>
        /// Gets all validation warnings.
        /// </summary>
        public IReadOnlyList<ValidationWarning> Warnings => _warnings.AsReadOnly();
        
        /// <summary>
        /// Gets the total number of validation issues (errors + warnings).
        /// </summary>
        public int TotalIssues => _errors.Count + _warnings.Count;
        
        /// <summary>
        /// Gets a summary of all validation issues.
        /// </summary>
        public string Summary
        {
            get
            {
                if (IsValid && !HasWarnings)
                    return "Validation passed successfully.";
                
                var parts = new List<string>();
                if (_errors.Any())
                    parts.Add($"{_errors.Count} error(s)");
                if (_warnings.Any())
                    parts.Add($"{_warnings.Count} warning(s)");
                
                return $"Validation failed with {string.Join(" and ", parts)}.";
            }
        }
        
        /// <summary>
        /// Creates a successful validation result.
        /// </summary>
        /// <returns>A validation result indicating success</returns>
        public static ValidationResult Success() => new ValidationResult();
        
        /// <summary>
        /// Creates a failed validation result with the specified error message.
        /// </summary>
        /// <param name="errorMessage">The error message</param>
        /// <returns>A validation result indicating failure</returns>
        public static ValidationResult Failure(string errorMessage)
        {
            var result = new ValidationResult();
            result.AddError(errorMessage);
            return result;
        }
        
        /// <summary>
        /// Creates a failed validation result with the specified error.
        /// </summary>
        /// <param name="error">The validation error</param>
        /// <returns>A validation result indicating failure</returns>
        public static ValidationResult Failure(ValidationError error)
        {
            var result = new ValidationResult();
            result.AddError(error);
            return result;
        }
        
        /// <summary>
        /// Adds an error message to this validation result.
        /// </summary>
        /// <param name="message">The error message</param>
        /// <param name="property">The property that caused the error (optional)</param>
        /// <param name="code">The error code (optional)</param>
        public void AddError(string message, string property = null, string code = null)
        {
            _errors.Add(new ValidationError(message, property, code));
        }
        
        /// <summary>
        /// Adds a validation error to this validation result.
        /// </summary>
        /// <param name="error">The validation error to add</param>
        public void AddError(ValidationError error)
        {
            if (error == null) throw new ArgumentNullException(nameof(error));
            _errors.Add(error);
        }
        
        /// <summary>
        /// Adds a warning message to this validation result.
        /// </summary>
        /// <param name="message">The warning message</param>
        /// <param name="property">The property that caused the warning (optional)</param>
        /// <param name="code">The warning code (optional)</param>
        public void AddWarning(string message, string property = null, string code = null)
        {
            _warnings.Add(new ValidationWarning(message, property, code));
        }
        
        /// <summary>
        /// Adds a validation warning to this validation result.
        /// </summary>
        /// <param name="warning">The validation warning to add</param>
        public void AddWarning(ValidationWarning warning)
        {
            if (warning == null) throw new ArgumentNullException(nameof(warning));
            _warnings.Add(warning);
        }
        
        /// <summary>
        /// Merges another validation result into this one.
        /// </summary>
        /// <param name="other">The validation result to merge</param>
        public void Merge(ValidationResult other)
        {
            if (other == null) throw new ArgumentNullException(nameof(other));
            
            _errors.AddRange(other.Errors);
            _warnings.AddRange(other.Warnings);
        }
        
        /// <summary>
        /// Combines multiple validation results into a single result.
        /// </summary>
        /// <param name="results">The validation results to combine</param>
        /// <returns>A combined validation result</returns>
        public static ValidationResult Combine(params ValidationResult[] results)
        {
            var combined = new ValidationResult();
            foreach (var result in results)
            {
                if (result != null)
                    combined.Merge(result);
            }
            return combined;
        }
        
        /// <summary>
        /// Returns a string representation of this validation result.
        /// </summary>
        /// <returns>A string containing the validation summary and details</returns>
        public override string ToString()
        {
            var lines = new List<string> { Summary };
            
            if (_errors.Any())
            {
                lines.Add("Errors:");
                lines.AddRange(_errors.Select(e => $"  - {e}"));
            }
            
            if (_warnings.Any())
            {
                lines.Add("Warnings:");
                lines.AddRange(_warnings.Select(w => $"  - {w}"));
            }
            
            return string.Join(Environment.NewLine, lines);
        }
    }
} 