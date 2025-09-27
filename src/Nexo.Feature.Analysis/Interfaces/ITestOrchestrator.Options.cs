using System;
using System.Collections.Generic;

namespace Nexo.Feature.Analysis.Interfaces
{
    /// <summary>
    /// Options classes for test orchestration
    /// </summary>
    public partial class TestOrchestrationOptions
    {
        /// <summary>
        /// Whether to use parallel execution.
        /// </summary>
        public bool UseParallelExecution { get; set; } = true;

        /// <summary>
        /// Maximum number of parallel test executions.
        /// </summary>
        public int MaxParallelism { get; set; } = Environment.ProcessorCount;

        /// <summary>
        /// Whether to use dependency-aware ordering.
        /// </summary>
        public bool UseDependencyOrdering { get; set; } = true;

        /// <summary>
        /// Whether to use incremental testing.
        /// </summary>
        public bool UseIncrementalTesting { get; set; } = true;

        /// <summary>
        /// Whether to use resource optimization.
        /// </summary>
        public bool UseResourceOptimization { get; set; } = true;

        /// <summary>
        /// Maximum memory usage in MB.
        /// </summary>
        public int MaxMemoryUsageMB { get; set; } = 2048;

        /// <summary>
        /// Maximum CPU usage percentage.
        /// </summary>
        public int MaxCpuUsagePercent { get; set; } = 80;

        /// <summary>
        /// Test execution timeout in seconds.
        /// </summary>
        public int TestTimeoutSeconds { get; set; } = 300;

        /// <summary>
        /// Whether to stop on first failure.
        /// </summary>
        public bool StopOnFirstFailure { get; set; } = false;

        /// <summary>
        /// Whether to retry failed tests.
        /// </summary>
        public bool RetryFailedTests { get; set; } = true;

        /// <summary>
        /// Maximum number of retry attempts.
        /// </summary>
        public int MaxRetryAttempts { get; set; } = 2;

        /// <summary>
        /// Test categories to include.
        /// </summary>
        public List<string> IncludeCategories { get; set; } = new List<string>();

        /// <summary>
        /// Test categories to exclude.
        /// </summary>
        public List<string> ExcludeCategories { get; set; } = new List<string>();

        /// <summary>
        /// Environment-specific options.
        /// </summary>
        public Dictionary<string, object> EnvironmentOptions { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Options for parallel execution.
    /// </summary>
    public partial class ParallelExecutionOptions
    {
        /// <summary>
        /// Maximum number of parallel executions.
        /// </summary>
        public int MaxParallelism { get; set; } = Environment.ProcessorCount;

        /// <summary>
        /// Batch size for grouping tests.
        /// </summary>
        public int BatchSize { get; set; } = 10;

        /// <summary>
        /// Whether to use resource-aware scheduling.
        /// </summary>
        public bool UseResourceAwareScheduling { get; set; } = true;

        /// <summary>
        /// Whether to balance load across available resources.
        /// </summary>
        public bool BalanceLoad { get; set; } = true;

        /// <summary>
        /// Timeout for individual test execution.
        /// </summary>
        public TimeSpan TestTimeout { get; set; } = TimeSpan.FromMinutes(5);

        /// <summary>
        /// Whether to continue on test failures.
        /// </summary>
        public bool ContinueOnFailure { get; set; } = true;
    }

    /// <summary>
    /// Options for dependency ordering.
    /// </summary>
    public partial class DependencyOrderingOptions
    {
        /// <summary>
        /// Whether to detect test dependencies automatically.
        /// </summary>
        public bool AutoDetectDependencies { get; set; } = true;

        /// <summary>
        /// Whether to respect explicit dependencies.
        /// </summary>
        public bool RespectExplicitDependencies { get; set; } = true;

        /// <summary>
        /// Whether to group independent tests for parallel execution.
        /// </summary>
        public bool GroupIndependentTests { get; set; } = true;

        /// <summary>
        /// Maximum group size for parallel execution.
        /// </summary>
        public int MaxGroupSize { get; set; } = 5;

        /// <summary>
        /// Whether to validate dependency cycles.
        /// </summary>
        public bool ValidateCycles { get; set; } = true;

        /// <summary>
        /// Custom dependency rules.
        /// </summary>
        public List<TestDependencyRule> CustomDependencies { get; set; } = new List<TestDependencyRule>();
    }

    /// <summary>
    /// Options for incremental testing.
    /// </summary>
    public partial class IncrementalTestingOptions
    {
        /// <summary>
        /// Base reference for incremental testing (commit, branch, etc.).
        /// </summary>
        public string BaseReference { get; set; } = "HEAD~1";

        /// <summary>
        /// Whether to use cached test results.
        /// </summary>
        public bool UseCachedResults { get; set; } = true;

        /// <summary>
        /// Whether to run affected tests only.
        /// </summary>
        public bool RunAffectedTestsOnly { get; set; } = true;

        /// <summary>
        /// Whether to include dependent tests.
        /// </summary>
        public bool IncludeDependentTests { get; set; } = true;

        /// <summary>
        /// Confidence threshold for test selection.
        /// </summary>
        public double ConfidenceThreshold { get; set; } = 0.8;

        /// <summary>
        /// Whether to fallback to full test suite on low confidence.
        /// </summary>
        public bool FallbackToFullSuite { get; set; } = true;

        /// <summary>
        /// Cache expiration time in minutes.
        /// </summary>
        public int CacheExpirationMinutes { get; set; } = 60;
    }
}
