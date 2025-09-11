using System;
using System.Collections.Generic;

namespace Nexo.Core.Domain.Results
{
    /// <summary>
    /// Documentation result
    /// </summary>
    public class DocumentationResult
    {
        /// <summary>
        /// Whether the documentation was successful
        /// </summary>
        public bool IsSuccess { get; set; }
        
        /// <summary>
        /// Error message if documentation failed
        /// </summary>
        public string? ErrorMessage { get; set; }
        
        /// <summary>
        /// Exception if documentation failed
        /// </summary>
        public Exception? Exception { get; set; }
        
        /// <summary>
        /// Generated documentation
        /// </summary>
        public string? GeneratedDocumentation { get; set; }
        
        /// <summary>
        /// Documentation suggestions
        /// </summary>
        public List<string> Suggestions { get; set; } = new();
        
        /// <summary>
        /// Documentation warnings
        /// </summary>
        public List<string> Warnings { get; set; } = new();
        
        /// <summary>
        /// Success message
        /// </summary>
        public string? SuccessMessage { get; set; }
        
        /// <summary>
        /// Documentation duration
        /// </summary>
        public TimeSpan Duration { get; set; }
        
        /// <summary>
        /// Timestamp when documentation completed
        /// </summary>
        public DateTime CompletedAt { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// Creates a successful result
        /// </summary>
        public static DocumentationResult Success(string? message = null)
        {
            return new DocumentationResult
            {
                IsSuccess = true,
                SuccessMessage = message
            };
        }
        
        /// <summary>
        /// Creates a failed result
        /// </summary>
        public static DocumentationResult Failure(string errorMessage, Exception? exception = null)
        {
            return new DocumentationResult
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
