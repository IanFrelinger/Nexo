using System;
using System.Collections.Generic;
using Nexo.Core.Domain.Enums.AI;

namespace Nexo.Core.Domain.Results
{
    /// <summary>
    /// Testing result
    /// </summary>
    public class TestingResult
    {
        /// <summary>
        /// Whether the testing was successful
        /// </summary>
        public bool IsSuccess { get; set; }
        
        /// <summary>
        /// Error message if testing failed
        /// </summary>
        public string? ErrorMessage { get; set; }
        
        /// <summary>
        /// Exception if testing failed
        /// </summary>
        public Exception? Exception { get; set; }
        
        /// <summary>
        /// Test score (0-100)
        /// </summary>
        public int Score { get; set; }
        
        /// <summary>
        /// Test results
        /// </summary>
        public List<TestResult> TestResults { get; set; } = new();
        
        /// <summary>
        /// Test suggestions
        /// </summary>
        public List<string> Suggestions { get; set; } = new();
        
        /// <summary>
        /// Test warnings
        /// </summary>
        public List<string> Warnings { get; set; } = new();
        
        /// <summary>
        /// Success message
        /// </summary>
        public string? SuccessMessage { get; set; }
        
        /// <summary>
        /// Testing duration
        /// </summary>
        public TimeSpan Duration { get; set; }
        
        /// <summary>
        /// Timestamp when testing completed
        /// </summary>
        public DateTime CompletedAt { get; set; } = DateTime.UtcNow;
        
        // Properties accessed by application layer
        public string GeneratedTests { get; set; } = "";
        public int QualityScore { get; set; }
        public int Coverage { get; set; }
        public AIEngineType EngineType { get; set; }
        public List<string> Tags { get; set; } = new();
        public Dictionary<string, object> Metadata { get; set; } = new();
        
        /// <summary>
        /// Creates a successful result
        /// </summary>
        public static TestingResult Success(string? message = null)
        {
            return new TestingResult
            {
                IsSuccess = true,
                SuccessMessage = message
            };
        }
        
        /// <summary>
        /// Creates a failed result
        /// </summary>
        public static TestingResult Failure(string errorMessage, Exception? exception = null)
        {
            return new TestingResult
            {
                IsSuccess = false,
                ErrorMessage = errorMessage,
                Exception = exception
            };
        }
        
        /// <summary>
        /// Adds test result
        /// </summary>
        public void AddTestResult(TestResult testResult)
        {
            TestResults.Add(testResult);
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
