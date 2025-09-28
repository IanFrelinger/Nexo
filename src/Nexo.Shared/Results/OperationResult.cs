using System;
using System.Collections.Generic;
using System.Linq;

namespace Nexo.Shared.Results
{
    /// <summary>
    /// Unified result type that replaces all specific result classes
    /// </summary>
    public class OperationResult<T>
    {
        public bool IsSuccess { get; set; }
        public T? Data { get; set; }
        public string? Message { get; set; }
        public string? ErrorMessage { get; set; }
        public Exception? Exception { get; set; }
        public List<string> ValidationErrors { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
        public Dictionary<string, object> Metadata { get; set; } = new();
        public DateTime Timestamp { get; set; }
        public TimeSpan Duration { get; set; }

        /// <summary>
        /// Gets the primary error message (exception or error message)
        /// </summary>
        public string? PrimaryError => Exception?.Message ?? ErrorMessage;

        /// <summary>
        /// Gets all error messages combined
        /// </summary>
        public string AllErrors => string.Join("; ", new[] { ErrorMessage, Exception?.Message }.Where(e => !string.IsNullOrEmpty(e)));

        /// <summary>
        /// Checks if the result has any warnings
        /// </summary>
        public bool HasWarnings => Warnings.Count > 0;

        /// <summary>
        /// Checks if the result has validation errors
        /// </summary>
        public bool HasValidationErrors => ValidationErrors.Count > 0;
    }

    /// <summary>
    /// Non-generic version for operations that don't return data
    /// </summary>
    public class OperationResult : OperationResult<object>
    {
        public static OperationResult Success(string? message = null, Dictionary<string, object>? metadata = null)
        {
            return new OperationResult
            {
                IsSuccess = true,
                Message = message ?? "Operation completed successfully",
                Metadata = metadata ?? new Dictionary<string, object>(),
                Timestamp = DateTime.UtcNow
            };
        }

        public static OperationResult Failure(string errorMessage, Exception? exception = null, Dictionary<string, object>? metadata = null)
        {
            return new OperationResult
            {
                IsSuccess = false,
                ErrorMessage = errorMessage,
                Exception = exception,
                Metadata = metadata ?? new Dictionary<string, object>(),
                Timestamp = DateTime.UtcNow
            };
        }
    }
}
