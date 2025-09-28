using System;
using System.Collections.Generic;
using System.Linq;

namespace Nexo.Shared.Errors
{
    /// <summary>
    /// Standardized error result
    /// </summary>
    public class ErrorResult
    {
        public string CorrelationId { get; set; } = string.Empty;
        public string ErrorType { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;
        public Exception? Exception { get; set; }
        public string Context { get; set; } = string.Empty;
        public List<string> ValidationErrors { get; set; } = new();
        public Dictionary<string, object> Metadata { get; set; } = new();
        public DateTime Timestamp { get; set; }
        public string? StackTrace { get; set; }
        public string? Source { get; set; }

        /// <summary>
        /// Gets the primary error message
        /// </summary>
        public string PrimaryMessage => Exception?.Message ?? ErrorMessage;

        /// <summary>
        /// Gets all error messages combined
        /// </summary>
        public string AllMessages => string.Join("; ", new[] { ErrorMessage, Exception?.Message }.Where(m => !string.IsNullOrEmpty(m)));

        /// <summary>
        /// Checks if this is a validation error
        /// </summary>
        public bool IsValidationError => ErrorType == "ValidationError" || ValidationErrors.Count > 0;

        /// <summary>
        /// Checks if this is a business error
        /// </summary>
        public bool IsBusinessError => ErrorType == "BusinessError";
    }
}
