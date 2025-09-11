using System;
using System.Collections.Generic;

namespace Nexo.Core.Domain.Results
{
    /// <summary>
    /// Result of optimization operations
    /// </summary>
    public class OptimizationResult
    {
        /// <summary>
        /// Whether the optimization was successful
        /// </summary>
        public bool IsSuccess { get; set; }
        
        /// <summary>
        /// Error message if optimization failed
        /// </summary>
        public string? ErrorMessage { get; set; }
        
        /// <summary>
        /// Exception if optimization failed
        /// </summary>
        public Exception? Exception { get; set; }
        
        /// <summary>
        /// Optimization score
        /// </summary>
        public double Score { get; set; }
        
        /// <summary>
        /// Optimization duration
        /// </summary>
        public TimeSpan Duration { get; set; }
        
        /// <summary>
        /// Success message
        /// </summary>
        public string? SuccessMessage { get; set; }
        
        /// <summary>
        /// Timestamp when optimization completed
        /// </summary>
        public DateTime CompletedAt { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// Additional data from the optimization
        /// </summary>
        public Dictionary<string, object> Data { get; set; } = new();
        
        /// <summary>
        /// Creates a successful result
        /// </summary>
        public static OptimizationResult Success(string? message = null)
        {
            return new OptimizationResult
            {
                IsSuccess = true,
                SuccessMessage = message
            };
        }
        
        /// <summary>
        /// Creates a failed result
        /// </summary>
        public static OptimizationResult Failure(string errorMessage, Exception? exception = null)
        {
            return new OptimizationResult
            {
                IsSuccess = false,
                ErrorMessage = errorMessage,
                Exception = exception
            };
        }
    }
}