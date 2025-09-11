using System;
using System.Collections.Generic;

namespace Nexo.Core.Domain.Results
{
    /// <summary>
    /// Result of domain logic operations
    /// </summary>
    public class DomainLogicResult
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
        public static DomainLogicResult Success(string? message = null)
        {
            return new DomainLogicResult
            {
                IsSuccess = true,
                SuccessMessage = message
            };
        }
        
        /// <summary>
        /// Creates a failed result
        /// </summary>
        public static DomainLogicResult Failure(string errorMessage, Exception? exception = null)
        {
            return new DomainLogicResult
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
        
        /// <summary>
        /// Aggregate roots generated
        /// </summary>
        public List<string> AggregateRoots { get; set; } = new();
        
        /// <summary>
        /// Domain events generated
        /// </summary>
        public List<string> DomainEvents { get; set; } = new();
        
        /// <summary>
        /// Repositories generated
        /// </summary>
        public List<string> Repositories { get; set; } = new();
        
        /// <summary>
        /// Factories generated
        /// </summary>
        public List<string> Factories { get; set; } = new();
        
        /// <summary>
        /// Specifications generated
        /// </summary>
        public List<string> Specifications { get; set; } = new();
        
        /// <summary>
        /// Business rules generated
        /// </summary>
        public List<string> BusinessRules { get; set; } = new();
        
        /// <summary>
        /// Domain entities generated
        /// </summary>
        public List<string> Entities { get; set; } = new();
        
        /// <summary>
        /// Value objects generated
        /// </summary>
        public List<string> ValueObjects { get; set; } = new();
        
        /// <summary>
        /// Domain services generated
        /// </summary>
        public List<string> DomainServices { get; set; } = new();
    }
}
