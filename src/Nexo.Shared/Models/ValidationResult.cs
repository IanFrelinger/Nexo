using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Nexo.Shared.Enums;

namespace Nexo.Shared.Models
{
    /// <summary>
    /// Represents the result of a validation operation.
    /// </summary>
    public sealed class ValidationResult
    {
        /// <summary>
        /// Gets a value indicating whether the validation passed.
        /// </summary>
        public bool IsValid { get; private set; }

        /// <summary>
        /// Gets the validation errors.
        /// </summary>
        public IReadOnlyList<ValidationError> Errors { get; private set; }

        /// <summary>
        /// Gets the first error message, if any.
        /// </summary>
        public string? ErrorMessage { get { return Errors.Count > 0 ? Errors[0].Message : null; } }

        /// <summary>
        /// Initializes a new instance of the <see cref="ValidationResult"/> class.
        /// </summary>
        public ValidationResult(bool isValid, IEnumerable<ValidationError>? errors = null)
        {
            IsValid = isValid;
            var errorList = errors != null ? new List<ValidationError>(errors) : new List<ValidationError>();
            Errors = new ReadOnlyCollection<ValidationError>(errorList);
        }

        /// <summary>
        /// Creates a successful validation result.
        /// </summary>
        public static ValidationResult Success() { return new ValidationResult(true); }

        /// <summary>
        /// Creates a failed validation result with a single error.
        /// </summary>
        public static ValidationResult Failure(string errorMessage)
        {
            return new ValidationResult(false, new List<ValidationError> { new ValidationError(errorMessage, ValidationSeverity.Error) });
        }

        /// <summary>
        /// Creates a failed validation result with multiple errors.
        /// </summary>
        public static ValidationResult Failure(params ValidationError[] errors)
        {
            return new ValidationResult(false, errors);
        }

        /// <summary>
        /// Checks if there are any errors with the specified severity.
        /// </summary>
        public bool HasErrorsWithSeverity(ValidationSeverity severity)
        {
            return Errors.Any(e => e.Severity == severity);
        }
    }
} 