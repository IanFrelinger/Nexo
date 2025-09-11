using System;
using System.Collections.Generic;
using Nexo.Core.Domain.Entities.AI;

namespace Nexo.Core.Domain.Results
{
    /// <summary>
    /// Code review result
    /// </summary>
    public class CodeReviewResult
    {
        /// <summary>
        /// Whether the review was successful
        /// </summary>
        public bool IsSuccess { get; set; }
        
        /// <summary>
        /// Error message if review failed
        /// </summary>
        public string? ErrorMessage { get; set; }
        
        /// <summary>
        /// Exception if review failed
        /// </summary>
        public Exception? Exception { get; set; }
        
        /// <summary>
        /// Review score (0-100)
        /// </summary>
        public int Score { get; set; }
        
        /// <summary>
        /// Quality score (0.0-1.0)
        /// </summary>
        public double QualityScore { get; set; }
        
        /// <summary>
        /// Code issues found during review
        /// </summary>
        public List<CodeIssue> Issues { get; set; } = new();
        
        /// <summary>
        /// Review comments
        /// </summary>
        public List<ReviewComment> Comments { get; set; } = new();
        
        /// <summary>
        /// Review suggestions
        /// </summary>
        public List<string> Suggestions { get; set; } = new();
        
        /// <summary>
        /// Review warnings
        /// </summary>
        public List<string> Warnings { get; set; } = new();
        
        /// <summary>
        /// Success message
        /// </summary>
        public string? SuccessMessage { get; set; }
        
        /// <summary>
        /// Review duration
        /// </summary>
        public TimeSpan Duration { get; set; }
        
        /// <summary>
        /// Timestamp when review completed
        /// </summary>
        public DateTime CompletedAt { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// Review time
        /// </summary>
        public DateTime ReviewTime { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// Engine type used for review
        /// </summary>
        public string EngineType { get; set; } = string.Empty;
        
        /// <summary>
        /// Creates a successful result
        /// </summary>
        public static CodeReviewResult Success(string? message = null)
        {
            return new CodeReviewResult
            {
                IsSuccess = true,
                SuccessMessage = message
            };
        }
        
        /// <summary>
        /// Creates a failed result
        /// </summary>
        public static CodeReviewResult Failure(string errorMessage, Exception? exception = null)
        {
            return new CodeReviewResult
            {
                IsSuccess = false,
                ErrorMessage = errorMessage,
                Exception = exception
            };
        }
        
        /// <summary>
        /// Adds comment
        /// </summary>
        public void AddComment(ReviewComment comment)
        {
            Comments.Add(comment);
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
    
    /// <summary>
    /// Review comment
    /// </summary>
    public class ReviewComment
    {
        /// <summary>
        /// Comment text
        /// </summary>
        public string Text { get; set; } = string.Empty;
        
        /// <summary>
        /// Comment type
        /// </summary>
        public string Type { get; set; } = string.Empty;
        
        /// <summary>
        /// Line number
        /// </summary>
        public int? LineNumber { get; set; }
        
        /// <summary>
        /// Column number
        /// </summary>
        public int? ColumnNumber { get; set; }
        
        /// <summary>
        /// Severity
        /// </summary>
        public string Severity { get; set; } = "Info";
        
        /// <summary>
        /// Additional data
        /// </summary>
        public Dictionary<string, object> Data { get; set; } = new();
    }
}
