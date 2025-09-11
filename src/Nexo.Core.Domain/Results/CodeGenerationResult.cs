using System;
using System.Collections.Generic;

namespace Nexo.Core.Domain.Results
{
    /// <summary>
    /// Code generation result
    /// </summary>
    public class CodeGenerationResult
    {
        /// <summary>
        /// Whether the generation was successful
        /// </summary>
        public bool IsSuccess { get; set; }
        
        /// <summary>
        /// Error message if generation failed
        /// </summary>
        public string? ErrorMessage { get; set; }
        
        /// <summary>
        /// Exception if generation failed
        /// </summary>
        public Exception? Exception { get; set; }
        
        /// <summary>
        /// Generated code
        /// </summary>
        public string? GeneratedCode { get; set; }
        
        /// <summary>
        /// Generation suggestions
        /// </summary>
        public List<string> Suggestions { get; set; } = new();
        
        /// <summary>
        /// Generation warnings
        /// </summary>
        public List<string> Warnings { get; set; } = new();
        
        /// <summary>
        /// Success message
        /// </summary>
        public string? SuccessMessage { get; set; }
        
        /// <summary>
        /// Generation duration
        /// </summary>
        public TimeSpan Duration { get; set; }
        
        /// <summary>
        /// Timestamp when generation completed
        /// </summary>
        public DateTime CompletedAt { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// Creates a successful result
        /// </summary>
        public static CodeGenerationResult Success(string? message = null)
        {
            return new CodeGenerationResult
            {
                IsSuccess = true,
                SuccessMessage = message
            };
        }
        
        /// <summary>
        /// Creates a failed result
        /// </summary>
        public static CodeGenerationResult Failure(string errorMessage, Exception? exception = null)
        {
            return new CodeGenerationResult
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
    }
}
