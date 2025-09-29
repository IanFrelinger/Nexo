using System;
using System.Collections.Generic;

namespace Nexo.Tests.Integration.Configuration
{
    /// <summary>
    /// Configuration settings for integration tests
    /// </summary>
    public class IntegrationTestConfiguration
    {
        public string TestEnvironment { get; set; } = "Development";
        public string TestDataPath { get; set; } = "/tmp/nexo-integration-tests";
        public bool CleanupAfterTests { get; set; } = true;
        public bool GenerateCoverageReport { get; set; } = true;
        public bool EnablePerformanceMonitoring { get; set; } = true;
        public bool EnableResourceMonitoring { get; set; } = true;
        public int MaxConcurrentOperations { get; set; } = 5;
        public int DefaultTimeoutSeconds { get; set; } = 30;
        public int PerformanceThresholdMs { get; set; } = 5000;
        public int MemoryThresholdMB { get; set; } = 100;

        public Dictionary<string, string> TestScenarios { get; set; } = new()
        {
            { "Full", "Complete end-to-end workflow testing" },
            { "ProjectOnly", "Project creation and management testing" },
            { "AgentOnly", "Agent creation and execution testing" },
            { "AssemblyOnly", "Assembly analysis testing" },
            { "Performance", "Performance and load testing" },
            { "ErrorHandling", "Error handling and resilience testing" }
        };

        public Dictionary<string, object> TestParameters { get; set; } = new()
        {
            { "MaxRetryAttempts", 3 },
            { "RetryDelayMs", 1000 },
            { "ConcurrentTestLimit", 10 },
            { "MemoryLeakThresholdMB", 50 },
            { "PerformanceRegressionThreshold", 0.2 }
        };

        public List<string> RequiredTestSuites { get; set; } = new()
        {
            "CLI",
            "CoreApplication", 
            "CoreDomain",
            "Shared",
            "Integration"
        };

        public Dictionary<string, bool> TestSuiteSettings { get; set; } = new()
        {
            { "IncludeCLITests", true },
            { "IncludeCoreApplicationTests", true },
            { "IncludeCoreDomainTests", true },
            { "IncludeSharedTests", true },
            { "IncludeIntegrationTests", true }
        };

        public static IntegrationTestConfiguration Default => new()
        {
            TestEnvironment = Environment.GetEnvironmentVariable("NEXO_TEST_ENV") ?? "Development",
            TestDataPath = Environment.GetEnvironmentVariable("NEXO_TEST_DATA_PATH") ?? "/tmp/nexo-integration-tests",
            CleanupAfterTests = bool.Parse(Environment.GetEnvironmentVariable("NEXO_CLEANUP_AFTER_TESTS") ?? "true"),
            GenerateCoverageReport = bool.Parse(Environment.GetEnvironmentVariable("NEXO_GENERATE_COVERAGE") ?? "true"),
            EnablePerformanceMonitoring = bool.Parse(Environment.GetEnvironmentVariable("NEXO_PERFORMANCE_MONITORING") ?? "true"),
            EnableResourceMonitoring = bool.Parse(Environment.GetEnvironmentVariable("NEXO_RESOURCE_MONITORING") ?? "true"),
            MaxConcurrentOperations = int.Parse(Environment.GetEnvironmentVariable("NEXO_MAX_CONCURRENT") ?? "5"),
            DefaultTimeoutSeconds = int.Parse(Environment.GetEnvironmentVariable("NEXO_DEFAULT_TIMEOUT") ?? "30"),
            PerformanceThresholdMs = int.Parse(Environment.GetEnvironmentVariable("NEXO_PERFORMANCE_THRESHOLD") ?? "5000"),
            MemoryThresholdMB = int.Parse(Environment.GetEnvironmentVariable("NEXO_MEMORY_THRESHOLD") ?? "100")
        };

        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(TestEnvironment))
                throw new ArgumentException("TestEnvironment cannot be null or empty");

            if (string.IsNullOrWhiteSpace(TestDataPath))
                throw new ArgumentException("TestDataPath cannot be null or empty");

            if (MaxConcurrentOperations <= 0)
                throw new ArgumentException("MaxConcurrentOperations must be greater than 0");

            if (DefaultTimeoutSeconds <= 0)
                throw new ArgumentException("DefaultTimeoutSeconds must be greater than 0");

            if (PerformanceThresholdMs <= 0)
                throw new ArgumentException("PerformanceThresholdMs must be greater than 0");

            if (MemoryThresholdMB <= 0)
                throw new ArgumentException("MemoryThresholdMB must be greater than 0");
        }
    }
}
