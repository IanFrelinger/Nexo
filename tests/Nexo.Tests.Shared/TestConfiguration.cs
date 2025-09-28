using System;
using System.Collections.Generic;

namespace Nexo.Tests.Shared
{
    /// <summary>
    /// Configuration for error-resistant test execution
    /// </summary>
    public class TestConfiguration
    {
        public static readonly TestConfiguration Default = new();

        /// <summary>
        /// Default timeout for individual tests
        /// </summary>
        public TimeSpan DefaultTestTimeout { get; set; } = TimeSpan.FromSeconds(30);

        /// <summary>
        /// Default timeout for orchestration tests
        /// </summary>
        public TimeSpan DefaultOrchestrationTimeout { get; set; } = TimeSpan.FromMinutes(5);

        /// <summary>
        /// Maximum number of retry attempts for flaky tests
        /// </summary>
        public int MaxRetryAttempts { get; set; } = 3;

        /// <summary>
        /// Base delay between retry attempts (exponential backoff)
        /// </summary>
        public int RetryDelayMs { get; set; } = 100;

        /// <summary>
        /// Maximum delay between retry attempts
        /// </summary>
        public int MaxRetryDelayMs { get; set; } = 5000;

        /// <summary>
        /// Enable parallel test execution
        /// </summary>
        public bool EnableParallelExecution { get; set; } = false;

        /// <summary>
        /// Maximum degree of parallelism for parallel tests
        /// </summary>
        public int MaxDegreeOfParallelism { get; set; } = Environment.ProcessorCount;

        /// <summary>
        /// Enable test isolation (each test runs in isolation)
        /// </summary>
        public bool EnableTestIsolation { get; set; } = true;

        /// <summary>
        /// Enable resource cleanup after each test
        /// </summary>
        public bool EnableResourceCleanup { get; set; } = true;

        /// <summary>
        /// Enable timeout protection for all tests
        /// </summary>
        public bool EnableTimeoutProtection { get; set; } = true;

        /// <summary>
        /// Enable retry logic for transient failures
        /// </summary>
        public bool EnableRetryLogic { get; set; } = true;

        /// <summary>
        /// Enable input validation for all test commands
        /// </summary>
        public bool EnableInputValidation { get; set; } = true;

        /// <summary>
        /// Enable defensive programming practices
        /// </summary>
        public bool EnableDefensiveProgramming { get; set; } = true;

        /// <summary>
        /// Enable comprehensive error reporting
        /// </summary>
        public bool EnableComprehensiveErrorReporting { get; set; } = true;

        /// <summary>
        /// Test environment (Development, Test, Production)
        /// </summary>
        public string TestEnvironment { get; set; } = "Development";

        /// <summary>
        /// Enable performance monitoring
        /// </summary>
        public bool EnablePerformanceMonitoring { get; set; } = true;

        /// <summary>
        /// Performance threshold in milliseconds
        /// </summary>
        public int PerformanceThresholdMs { get; set; } = 1000;

        /// <summary>
        /// Enable memory leak detection
        /// </summary>
        public bool EnableMemoryLeakDetection { get; set; } = false;

        /// <summary>
        /// Memory threshold in MB
        /// </summary>
        public int MemoryThresholdMB { get; set; } = 100;

        /// <summary>
        /// Test categories to include
        /// </summary>
        public HashSet<string> IncludedTestCategories { get; set; } = new() { "Unit", "Integration", "Performance" };

        /// <summary>
        /// Test categories to exclude
        /// </summary>
        public HashSet<string> ExcludedTestCategories { get; set; } = new();

        /// <summary>
        /// Enable test data generation
        /// </summary>
        public bool EnableTestDataGeneration { get; set; } = true;

        /// <summary>
        /// Test data seed for reproducible tests
        /// </summary>
        public int TestDataSeed { get; set; } = 12345;

        /// <summary>
        /// Enable test result caching
        /// </summary>
        public bool EnableTestResultCaching { get; set; } = false;

        /// <summary>
        /// Cache expiration time
        /// </summary>
        public TimeSpan CacheExpirationTime { get; set; } = TimeSpan.FromMinutes(30);

        /// <summary>
        /// Enable test result persistence
        /// </summary>
        public bool EnableTestResultPersistence { get; set; } = false;

        /// <summary>
        /// Test result storage path
        /// </summary>
        public string TestResultStoragePath { get; set; } = "./TestResults";

        /// <summary>
        /// Enable test result analysis
        /// </summary>
        public bool EnableTestResultAnalysis { get; set; } = true;

        /// <summary>
        /// Enable test trend analysis
        /// </summary>
        public bool EnableTestTrendAnalysis { get; set; } = false;

        /// <summary>
        /// Enable test failure prediction
        /// </summary>
        public bool EnableTestFailurePrediction { get; set; } = false;

        /// <summary>
        /// Enable test optimization suggestions
        /// </summary>
        public bool EnableTestOptimizationSuggestions { get; set; } = false;

        /// <summary>
        /// Creates a configuration optimized for CI/CD environments
        /// </summary>
        public static TestConfiguration ForCI()
        {
            return new TestConfiguration
            {
                DefaultTestTimeout = TimeSpan.FromSeconds(60),
                DefaultOrchestrationTimeout = TimeSpan.FromMinutes(10),
                MaxRetryAttempts = 2,
                RetryDelayMs = 200,
                EnableParallelExecution = true,
                MaxDegreeOfParallelism = Environment.ProcessorCount,
                EnableTestIsolation = true,
                EnableResourceCleanup = true,
                EnableTimeoutProtection = true,
                EnableRetryLogic = true,
                EnableInputValidation = true,
                EnableDefensiveProgramming = true,
                EnableComprehensiveErrorReporting = true,
                TestEnvironment = "CI",
                EnablePerformanceMonitoring = true,
                PerformanceThresholdMs = 2000,
                EnableMemoryLeakDetection = true,
                MemoryThresholdMB = 200,
                IncludedTestCategories = new() { "Unit", "Integration" },
                ExcludedTestCategories = new() { "Performance", "Manual" },
                EnableTestDataGeneration = true,
                TestDataSeed = DateTime.UtcNow.Millisecond,
                EnableTestResultCaching = false,
                EnableTestResultPersistence = true,
                TestResultStoragePath = "./TestResults/CI",
                EnableTestResultAnalysis = true,
                EnableTestTrendAnalysis = true,
                EnableTestFailurePrediction = false,
                EnableTestOptimizationSuggestions = true
            };
        }

        /// <summary>
        /// Creates a configuration optimized for local development
        /// </summary>
        public static TestConfiguration ForDevelopment()
        {
            return new TestConfiguration
            {
                DefaultTestTimeout = TimeSpan.FromSeconds(30),
                DefaultOrchestrationTimeout = TimeSpan.FromMinutes(2),
                MaxRetryAttempts = 3,
                RetryDelayMs = 100,
                EnableParallelExecution = false,
                MaxDegreeOfParallelism = 1,
                EnableTestIsolation = true,
                EnableResourceCleanup = true,
                EnableTimeoutProtection = true,
                EnableRetryLogic = true,
                EnableInputValidation = true,
                EnableDefensiveProgramming = true,
                EnableComprehensiveErrorReporting = true,
                TestEnvironment = "Development",
                EnablePerformanceMonitoring = false,
                PerformanceThresholdMs = 5000,
                EnableMemoryLeakDetection = false,
                MemoryThresholdMB = 500,
                IncludedTestCategories = new() { "Unit", "Integration", "Performance", "Manual" },
                ExcludedTestCategories = new(),
                EnableTestDataGeneration = true,
                TestDataSeed = 12345,
                EnableTestResultCaching = true,
                CacheExpirationTime = TimeSpan.FromMinutes(10),
                EnableTestResultPersistence = false,
                TestResultStoragePath = "./TestResults/Development",
                EnableTestResultAnalysis = true,
                EnableTestTrendAnalysis = false,
                EnableTestFailurePrediction = false,
                EnableTestOptimizationSuggestions = false
            };
        }

        /// <summary>
        /// Creates a configuration optimized for production testing
        /// </summary>
        public static TestConfiguration ForProduction()
        {
            return new TestConfiguration
            {
                DefaultTestTimeout = TimeSpan.FromSeconds(120),
                DefaultOrchestrationTimeout = TimeSpan.FromMinutes(15),
                MaxRetryAttempts = 5,
                RetryDelayMs = 500,
                EnableParallelExecution = true,
                MaxDegreeOfParallelism = Math.Max(1, Environment.ProcessorCount / 2),
                EnableTestIsolation = true,
                EnableResourceCleanup = true,
                EnableTimeoutProtection = true,
                EnableRetryLogic = true,
                EnableInputValidation = true,
                EnableDefensiveProgramming = true,
                EnableComprehensiveErrorReporting = true,
                TestEnvironment = "Production",
                EnablePerformanceMonitoring = true,
                PerformanceThresholdMs = 5000,
                EnableMemoryLeakDetection = true,
                MemoryThresholdMB = 1000,
                IncludedTestCategories = new() { "Unit", "Integration", "Performance", "Load", "Stress" },
                ExcludedTestCategories = new() { "Manual" },
                EnableTestDataGeneration = true,
                TestDataSeed = DateTime.UtcNow.Millisecond,
                EnableTestResultCaching = true,
                CacheExpirationTime = TimeSpan.FromHours(1),
                EnableTestResultPersistence = true,
                TestResultStoragePath = "./TestResults/Production",
                EnableTestResultAnalysis = true,
                EnableTestTrendAnalysis = true,
                EnableTestFailurePrediction = true,
                EnableTestOptimizationSuggestions = true
            };
        }
    }
}
