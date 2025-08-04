using System;
using System.Collections.Generic;
using Nexo.Shared.Models;

namespace Nexo.Feature.Pipeline.Models
{
    /// <summary>
    /// Represents the result of dependency validation.
    /// </summary>
    public class ExecutionValidationResult
    {
        /// <summary>
        /// Gets or sets whether the validation was successful.
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// Gets or sets the error message if validation failed.
        /// </summary>
        public string ErrorMessage { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the validation errors.
        /// </summary>
        public List<ValidationError> Errors { get; set; } = new List<ValidationError>();

        /// <summary>
        /// Gets or sets the validation warnings.
        /// </summary>
        public List<ValidationWarning> Warnings { get; set; } = new List<ValidationWarning>();
    }
} 