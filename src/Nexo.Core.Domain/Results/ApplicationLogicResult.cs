using System;
using System.Collections.Generic;

namespace Nexo.Core.Domain.Results
{
    /// <summary>
    /// Result of application logic operations
    /// </summary>
    public class ApplicationLogicOperationResult
    {
        /// <summary>
        /// Whether the operation was successful
        /// </summary>
        public bool IsSuccess { get; set; }
        
        /// <summary>
        /// Error message if operation failed
        /// </summary>
        public string? ErrorMessage { get; set; }
        
        /// <summary>
        /// Exception if operation failed
        /// </summary>
        public Exception? Exception { get; set; }
        
        /// <summary>
        /// Additional data from the operation
        /// </summary>
        public Dictionary<string, object> Data { get; set; } = new();
        
        /// <summary>
        /// Validation errors
        /// </summary>
        public List<string> ValidationErrors { get; set; } = new();
        
        /// <summary>
        /// Warnings from the operation
        /// </summary>
        public List<string> Warnings { get; set; } = new();
        
        /// <summary>
        /// Success message
        /// </summary>
        public string? SuccessMessage { get; set; }
        
        /// <summary>
        /// Operation duration
        /// </summary>
        public TimeSpan Duration { get; set; }
        
        /// <summary>
        /// Timestamp when operation completed
        /// </summary>
        public DateTime CompletedAt { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// Creates a successful result
        /// </summary>
        public static ApplicationLogicOperationResult Success(string? message = null)
        {
            return new ApplicationLogicOperationResult
            {
                IsSuccess = true,
                SuccessMessage = message
            };
        }
        
        /// <summary>
        /// Creates a failed result
        /// </summary>
        public static ApplicationLogicOperationResult Failure(string errorMessage, Exception? exception = null)
        {
            return new ApplicationLogicOperationResult
            {
                IsSuccess = false,
                ErrorMessage = errorMessage,
                Exception = exception
            };
        }
        
        /// <summary>
        /// Adds validation error
        /// </summary>
        public void AddValidationError(string error)
        {
            ValidationErrors.Add(error);
            IsSuccess = false;
        }
        
        /// <summary>
        /// Adds warning
        /// </summary>
        public void AddWarning(string warning)
        {
            Warnings.Add(warning);
        }
        
        /// <summary>
        /// Adds data
        /// </summary>
        public void AddData(string key, object value)
        {
            Data[key] = value;
        }
    }
}
