using System;
using System.Collections.Generic;
using System.Linq;

namespace NexoDirectorStudio.Tests.EditMode
{
    /// <summary>
    /// Configuration for test suite execution.
    /// </summary>
    public class TestSuiteConfig
    {
        public int TimeoutSeconds { get; set; } = 60;
        public int MaxRetries { get; set; } = 1;
        public bool EnablePerformanceTests { get; set; } = true;
        public bool EnableStressTests { get; set; } = true;
        public bool EnableMemoryTests { get; set; } = true;
        public float MinValidationScore { get; set; } = 70f;
    }

    /// <summary>
    /// Result of test execution.
    /// </summary>
    public class TestExecutionResult
    {
        public string Category { get; set; } = "";
        public string Environment { get; set; } = "";
        public DateTimeOffset StartTime { get; set; }
        public DateTimeOffset EndTime { get; set; }
        public TimeSpan Duration { get; set; }
        public bool Success { get; set; }
        public string ErrorMessage { get; set; } = "";
        public int TestsRun { get; set; }
        public int TestsPassed { get; set; }
        public int TestsFailed { get; set; }
        public Dictionary<string, object> PerformanceMetrics { get; set; } = new();
        public List<TestExecutionResult> CategoryResults { get; set; } = new();
    }

    /// <summary>
    /// Test categories available for execution.
    /// </summary>
    public static class TestCategories
    {
        public static readonly string[] All = new[]
        {
            "E2E",
            "Integration", 
            "Performance",
            "CICD",
            "ComponentValidation"
        };
    }

    /// <summary>
    /// Test suite configurations for different environments.
    /// </summary>
    public static class TestSuiteConfigs
    {
        public static readonly Dictionary<string, TestSuiteConfig> All = new()
        {
            ["Development"] = new TestSuiteConfig
            {
                TimeoutSeconds = 120,
                MaxRetries = 3,
                EnablePerformanceTests = true,
                EnableStressTests = true,
                EnableMemoryTests = true,
                MinValidationScore = 70f
            },
            ["CI"] = new TestSuiteConfig
            {
                TimeoutSeconds = 60,
                MaxRetries = 1,
                EnablePerformanceTests = false,
                EnableStressTests = false,
                EnableMemoryTests = true,
                MinValidationScore = 60f
            },
            ["Production"] = new TestSuiteConfig
            {
                TimeoutSeconds = 180,
                MaxRetries = 5,
                EnablePerformanceTests = true,
                EnableStressTests = true,
                EnableMemoryTests = true,
                MinValidationScore = 85f
            }
        };
    }
}
