using System;
using System.Collections.Generic;

namespace Nexo.Core.Domain.Results
{
    /// <summary>
    /// Code generation optimization result
    /// </summary>
    public class CodeGenerationOptimizationResult
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
        /// Optimized code
        /// </summary>
        public string? OptimizedCode { get; set; }
        
        /// <summary>
        /// Original code
        /// </summary>
        public string? OriginalCode { get; set; }
        
        /// <summary>
        /// Optimization suggestions
        /// </summary>
        public List<string> Suggestions { get; set; } = new();
        
        /// <summary>
        /// Performance improvements
        /// </summary>
        public Dictionary<string, object> PerformanceImprovements { get; set; } = new();
        
        /// <summary>
        /// Quality improvements
        /// </summary>
        public Dictionary<string, object> QualityImprovements { get; set; } = new();
        
        /// <summary>
        /// Optimization warnings
        /// </summary>
        public List<string> Warnings { get; set; } = new();
        
        /// <summary>
        /// Success message
        /// </summary>
        public string? SuccessMessage { get; set; }
        
        /// <summary>
        /// Optimization duration
        /// </summary>
        public TimeSpan Duration { get; set; }
        
        /// <summary>
        /// Timestamp when optimization completed
        /// </summary>
        public DateTime CompletedAt { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// Creates a successful result
        /// </summary>
        public static CodeGenerationOptimizationResult Success(string? message = null)
        {
            return new CodeGenerationOptimizationResult
            {
                IsSuccess = true,
                SuccessMessage = message
            };
        }
        
        /// <summary>
        /// Creates a failed result
        /// </summary>
        public static CodeGenerationOptimizationResult Failure(string errorMessage, Exception? exception = null)
        {
            return new CodeGenerationOptimizationResult
            {
                IsSuccess = false,
                ErrorMessage = errorMessage,
                Exception = exception
            };
        }
        
        /// <summary>
        /// Adds suggestion
        /// </summary>
        public void AddSuggestion(string suggestion)
        {
            Suggestions.Add(suggestion);
        }
        
        /// <summary>
        /// Adds warning
        /// </summary>
        public void AddWarning(string warning)
        {
            Warnings.Add(warning);
        }
        
        /// <summary>
        /// Adds performance improvement
        /// </summary>
        public void AddPerformanceImprovement(string key, object value)
        {
            PerformanceImprovements[key] = value;
        }
        
        /// <summary>
        /// Adds quality improvement
        /// </summary>
        public void AddQualityImprovement(string key, object value)
        {
            QualityImprovements[key] = value;
        }
    }
}
