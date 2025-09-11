using System;
using System.Collections.Generic;

namespace Nexo.Core.Domain.Results
{
    /// <summary>
    /// Result of test suite operations
    /// </summary>
    public class TestSuiteResult
    {
        /// <summary>
        /// Whether the test suite was successful
        /// </summary>
        public bool IsSuccess { get; set; }
        
        /// <summary>
        /// Error message if test suite failed
        /// </summary>
        public string? ErrorMessage { get; set; }
        
        /// <summary>
        /// Exception if test suite failed
        /// </summary>
        public Exception? Exception { get; set; }
        
        /// <summary>
        /// Total number of tests
        /// </summary>
        public int TotalTests { get; set; }
        
        /// <summary>
        /// Number of passed tests
        /// </summary>
        public int PassedTests { get; set; }
        
        /// <summary>
        /// Number of failed tests
        /// </summary>
        public int FailedTests { get; set; }
        
        /// <summary>
        /// Number of skipped tests
        /// </summary>
        public int SkippedTests { get; set; }
        
        /// <summary>
        /// Test execution duration
        /// </summary>
        public TimeSpan Duration { get; set; }
        
        /// <summary>
        /// Test results
        /// </summary>
        public List<TestResult> TestResults { get; set; } = new();
        
        /// <summary>
        /// Warnings from the test suite
        /// </summary>
        public List<string> Warnings { get; set; } = new();
        
        /// <summary>
        /// Success message
        /// </summary>
        public string? SuccessMessage { get; set; }
        
        /// <summary>
        /// Timestamp when test suite completed
        /// </summary>
        public DateTime CompletedAt { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// Creates a successful result
        /// </summary>
        public static TestSuiteResult Success(string? message = null)
        {
            return new TestSuiteResult
            {
                IsSuccess = true,
                SuccessMessage = message
            };
        }
        
        /// <summary>
        /// Creates a failed result
        /// </summary>
        public static TestSuiteResult Failure(string errorMessage, Exception? exception = null)
        {
            return new TestSuiteResult
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
            TotalTests++;
            
            if (testResult.IsPassed)
                PassedTests++;
            else if (testResult.IsSkipped)
                SkippedTests++;
            else
                FailedTests++;
        }
        
        /// <summary>
        /// Adds warning
        /// </summary>
        public void AddWarning(string warning)
        {
            Warnings.Add(warning);
        }
        
        /// <summary>
        /// Unit tests generated
        /// </summary>
        public List<string> UnitTests { get; set; } = new();
        
        /// <summary>
        /// Integration tests generated
        /// </summary>
        public List<string> IntegrationTests { get; set; } = new();
        
        /// <summary>
        /// Domain tests generated
        /// </summary>
        public List<string> DomainTests { get; set; } = new();
        
        /// <summary>
        /// Test fixtures generated
        /// </summary>
        public List<string> TestFixtures { get; set; } = new();
        
        /// <summary>
        /// Test coverage percentage
        /// </summary>
        public double Coverage { get; set; }
    }
    
    /// <summary>
    /// Individual test result
    /// </summary>
    public class TestResult
    {
        /// <summary>
        /// Test name
        /// </summary>
        public string TestName { get; set; } = string.Empty;
        
        /// <summary>
        /// Whether the test passed
        /// </summary>
        public bool IsPassed { get; set; }
        
        /// <summary>
        /// Whether the test was skipped
        /// </summary>
        public bool IsSkipped { get; set; }
        
        /// <summary>
        /// Test duration
        /// </summary>
        public TimeSpan Duration { get; set; }
        
        /// <summary>
        /// Error message if test failed
        /// </summary>
        public string? ErrorMessage { get; set; }
        
        /// <summary>
        /// Exception if test failed
        /// </summary>
        public Exception? Exception { get; set; }
        
        /// <summary>
        /// Test output
        /// </summary>
        public string? Output { get; set; }
        
        /// <summary>
        /// Timestamp when test completed
        /// </summary>
        public DateTime CompletedAt { get; set; } = DateTime.UtcNow;
    }
}
