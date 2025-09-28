using System;
using System.Collections.Generic;
using System.Linq;

namespace Nexo.Shared.Errors
{
    /// <summary>
    /// Factory for creating standardized error results
    /// </summary>
    public static class ErrorFactory
    {
        /// <summary>
        /// Creates a standardized error result for validation failures
        /// </summary>
        public static ErrorResult CreateValidationError(IEnumerable<string> validationErrors, string? context = null)
        {
            return new ErrorResult
            {
                ErrorType = "ValidationError",
                ErrorMessage = "Validation failed",
                ValidationErrors = validationErrors.ToList(),
                Context = context ?? string.Empty,
                Timestamp = DateTime.UtcNow,
                CorrelationId = Guid.NewGuid().ToString()
            };
        }

        /// <summary>
        /// Creates a standardized error result for business logic failures
        /// </summary>
        public static ErrorResult CreateBusinessError(string message, string? context = null, Dictionary<string, object>? metadata = null)
        {
            return new ErrorResult
            {
                ErrorType = "BusinessError",
                ErrorMessage = message,
                Context = context ?? string.Empty,
                Metadata = metadata ?? new Dictionary<string, object>(),
                Timestamp = DateTime.UtcNow,
                CorrelationId = Guid.NewGuid().ToString()
            };
        }

        /// <summary>
        /// Creates a standardized error result for system failures
        /// </summary>
        public static ErrorResult CreateSystemError(string message, Exception? exception = null, string? context = null, Dictionary<string, object>? metadata = null)
        {
            return new ErrorResult
            {
                ErrorType = "SystemError",
                ErrorMessage = message,
                Exception = exception,
                Context = context ?? string.Empty,
                Metadata = metadata ?? new Dictionary<string, object>(),
                Timestamp = DateTime.UtcNow,
                CorrelationId = Guid.NewGuid().ToString()
            };
        }
    }
}
