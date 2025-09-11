using System;
using System.Collections.Generic;
using Nexo.Core.Domain.Enums.AI;

namespace Nexo.Core.Domain.Common
{
    /// <summary>
    /// Base class for all domain results following CQRS pattern
    /// </summary>
    public abstract class BaseResult
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public bool IsSuccess { get; set; }
        public string? ErrorMessage { get; set; }
        public Exception? Exception { get; set; }
        public string? SuccessMessage { get; set; }
        public List<string> ValidationErrors { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
        public Dictionary<string, object> Data { get; set; } = new();
        public TimeSpan Duration { get; set; }
        public DateTime CompletedAt { get; set; } = DateTime.UtcNow;
        public string CompletedBy { get; set; } = string.Empty;

        /// <summary>
        /// Creates a successful result
        /// </summary>
        public static T Success<T>(string? message = null) where T : BaseResult, new()
        {
            return new T
            {
                IsSuccess = true,
                SuccessMessage = message
            };
        }

        /// <summary>
        /// Creates a failed result
        /// </summary>
        public static T Failure<T>(string errorMessage, Exception? exception = null) where T : BaseResult, new()
        {
            return new T
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

    /// <summary>
    /// Base class for AI operation results
    /// </summary>
    public abstract class AIOperationResult : BaseResult
    {
        public string GeneratedContent { get; set; } = string.Empty;
        public string Explanation { get; set; } = string.Empty;
        public AIConfidenceLevel Confidence { get; set; }
        public double ConfidenceScore { get; set; }
        public List<string> Suggestions { get; set; } = new();
        public AIEngineType EngineType { get; set; }
        public int QualityScore { get; set; }
        public int Coverage { get; set; }
        public List<string> Tags { get; set; } = new();
    }

    /// <summary>
    /// Base class for code generation results
    /// </summary>
    public abstract class CodeGenerationResult : AIOperationResult
    {
        public string GeneratedCode { get; set; } = string.Empty;
        public string OriginalCode { get; set; } = string.Empty;
        public string OptimizedCode { get; set; } = string.Empty;
        public double OptimizationScore { get; set; }
        public List<string> Improvements { get; set; } = new();
        public double PerformanceGain { get; set; }
    }
}
