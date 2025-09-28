using System;
using System.Collections.Generic;
using System.Linq;

namespace Nexo.Shared.Results
{
    /// <summary>
    /// Factory for creating standardized results
    /// </summary>
    public static class ResultFactory
    {
        /// <summary>
        /// Creates a successful result with optional data
        /// </summary>
        public static OperationResult<T> Success<T>(T? data = default, string? message = null, Dictionary<string, object>? metadata = null)
        {
            return new OperationResult<T>
            {
                IsSuccess = true,
                Data = data,
                Message = message ?? "Operation completed successfully",
                Metadata = metadata ?? new Dictionary<string, object>(),
                Timestamp = DateTime.UtcNow
            };
        }

        /// <summary>
        /// Creates a failed result with error information
        /// </summary>
        public static OperationResult<T> Failure<T>(string errorMessage, Exception? exception = null, Dictionary<string, object>? metadata = null)
        {
            return new OperationResult<T>
            {
                IsSuccess = false,
                ErrorMessage = errorMessage,
                Exception = exception,
                Metadata = metadata ?? new Dictionary<string, object>(),
                Timestamp = DateTime.UtcNow
            };
        }

        /// <summary>
        /// Creates a validation failure result
        /// </summary>
        public static OperationResult<T> ValidationFailure<T>(IEnumerable<string> validationErrors, Dictionary<string, object>? metadata = null)
        {
            return new OperationResult<T>
            {
                IsSuccess = false,
                ErrorMessage = "Validation failed",
                ValidationErrors = validationErrors.ToList(),
                Metadata = metadata ?? new Dictionary<string, object>(),
                Timestamp = DateTime.UtcNow
            };
        }

        /// <summary>
        /// Creates a partial success result with warnings
        /// </summary>
        public static OperationResult<T> PartialSuccess<T>(T? data, IEnumerable<string> warnings, Dictionary<string, object>? metadata = null)
        {
            return new OperationResult<T>
            {
                IsSuccess = true,
                Data = data,
                Message = "Operation completed with warnings",
                Warnings = warnings.ToList(),
                Metadata = metadata ?? new Dictionary<string, object>(),
                Timestamp = DateTime.UtcNow
            };
        }
    }
}
