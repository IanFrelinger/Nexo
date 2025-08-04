using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Nexo.Feature.Analysis.Models;

namespace Nexo.Feature.Analysis.Interfaces
{
    /// <summary>
    /// Interface for intelligent test orchestration with parallel execution and dependency management.
    /// </summary>
    public interface ITestOrchestrator
    {
        /// <summary>
        /// Executes tests with intelligent orchestration including parallel execution and dependency management.
        /// </summary>
        /// <param name="options">Test orchestration options.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Test orchestration result.</returns>
        Task<TestOrchestrationResult> ExecuteTestsAsync(TestOrchestrationOptions options, CancellationToken cancellationToken = default);

        /// <summary>
        /// Executes tests in parallel with resource optimization.
        /// </summary>
        /// <param name="testFiles">List of test files to execute.</param>
        /// <param name="options">Parallel execution options.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Parallel execution result.</returns>
        Task<ParallelExecutionResult> ExecuteTestsInParallelAsync(List<string> testFiles, ParallelExecutionOptions options, CancellationToken cancellationToken = default);

        /// <summary>
        /// Orders tests based on dependencies and execution requirements.
        /// </summary>
        /// <param name="testFiles">List of test files to order.</param>
        /// <param name="options">Dependency ordering options.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Ordered test execution plan.</returns>
        Task<TestExecutionPlan> CreateDependencyOrderedPlanAsync(List<string> testFiles, DependencyOrderingOptions options, CancellationToken cancellationToken = default);

        /// <summary>
        /// Performs incremental testing based on previous test results and changes.
        /// </summary>
        /// <param name="options">Incremental testing options.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Incremental testing result.</returns>
        Task<IncrementalTestingResult> ExecuteIncrementalTestsAsync(IncrementalTestingOptions options, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the current resource utilization and optimization recommendations.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Resource utilization information.</returns>
        Task<ResourceUtilization> GetResourceUtilizationAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Validates test orchestration options.
        /// </summary>
        /// <param name="options">Options to validate.</param>
        /// <returns>Validation result.</returns>
        TestOrchestrationValidation ValidateOptions(TestOrchestrationOptions options);
    }

    /// <summary>
    /// Options for test orchestration.
    /// </summary>
    public class TestOrchestrationOptions
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
    public class ParallelExecutionOptions
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
    public class DependencyOrderingOptions
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
    public class IncrementalTestingOptions
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

    /// <summary>
    /// Result of test orchestration.
    /// </summary>
    public class TestOrchestrationResult
    {
        /// <summary>
        /// Whether the orchestration was successful.
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// Total number of tests executed.
        /// </summary>
        public int TotalTests { get; set; }

        /// <summary>
        /// Number of tests that passed.
        /// </summary>
        public int PassedTests { get; set; }

        /// <summary>
        /// Number of tests that failed.
        /// </summary>
        public int FailedTests { get; set; }

        /// <summary>
        /// Number of tests that were skipped.
        /// </summary>
        public int SkippedTests { get; set; }

        /// <summary>
        /// Total execution time.
        /// </summary>
        public TimeSpan TotalExecutionTime { get; set; }

        /// <summary>
        /// Parallel execution metrics.
        /// </summary>
        public ParallelExecutionMetrics ParallelMetrics { get; set; } = new ParallelExecutionMetrics();

        /// <summary>
        /// Resource utilization during execution.
        /// </summary>
        public ResourceUtilization ResourceUtilization { get; set; } = new ResourceUtilization();

        /// <summary>
        /// Test execution results.
        /// </summary>
        public List<TestExecutionResult> TestResults { get; set; } = new List<TestExecutionResult>();

        /// <summary>
        /// Warnings or issues encountered.
        /// </summary>
        public List<string> Warnings { get; set; } = new List<string>();

        /// <summary>
        /// Error message if orchestration failed.
        /// </summary>
        public string ErrorMessage { get; set; } = string.Empty;
    }

    /// <summary>
    /// Result of parallel execution.
    /// </summary>
    public class ParallelExecutionResult
    {
        /// <summary>
        /// Whether parallel execution was successful.
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// Number of tests executed in parallel.
        /// </summary>
        public int TestsExecuted { get; set; }

        /// <summary>
        /// Parallel execution metrics.
        /// </summary>
        public ParallelExecutionMetrics Metrics { get; set; } = new ParallelExecutionMetrics();

        /// <summary>
        /// Test execution results.
        /// </summary>
        public List<TestExecutionResult> Results { get; set; } = new List<TestExecutionResult>();

        /// <summary>
        /// Error message if execution failed.
        /// </summary>
        public string ErrorMessage { get; set; } = string.Empty;
    }

    /// <summary>
    /// Test execution plan with dependency ordering.
    /// </summary>
    public class TestExecutionPlan
    {
        /// <summary>
        /// Ordered phases of test execution.
        /// </summary>
        public List<TestExecutionPhase> Phases { get; set; } = new List<TestExecutionPhase>();

        /// <summary>
        /// Total number of tests in the plan.
        /// </summary>
        public int TotalTests { get; set; }

        /// <summary>
        /// Estimated execution time.
        /// </summary>
        public TimeSpan EstimatedExecutionTime { get; set; }

        /// <summary>
        /// Dependency graph visualization.
        /// </summary>
        public string DependencyGraph { get; set; } = string.Empty;

        /// <summary>
        /// Whether the plan is valid.
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// Validation errors if any.
        /// </summary>
        public List<string> ValidationErrors { get; set; } = new List<string>();
    }

    /// <summary>
    /// Phase of test execution.
    /// </summary>
    public class TestExecutionPhase
    {
        /// <summary>
        /// Phase identifier.
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Phase name.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Test files in this phase.
        /// </summary>
        public List<string> TestFiles { get; set; } = new List<string>();

        /// <summary>
        /// Whether tests in this phase can run in parallel.
        /// </summary>
        public bool CanRunInParallel { get; set; } = true;

        /// <summary>
        /// Dependencies for this phase.
        /// </summary>
        public List<string> Dependencies { get; set; } = new List<string>();

        /// <summary>
        /// Estimated execution time for this phase.
        /// </summary>
        public TimeSpan EstimatedTime { get; set; }
    }

    /// <summary>
    /// Result of incremental testing.
    /// </summary>
    public class IncrementalTestingResult
    {
        /// <summary>
        /// Whether incremental testing was successful.
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// Number of tests executed incrementally.
        /// </summary>
        public int TestsExecuted { get; set; }

        /// <summary>
        /// Number of tests that would have been executed in full suite.
        /// </summary>
        public int TotalTestsInSuite { get; set; }

        /// <summary>
        /// Time saved compared to full suite execution.
        /// </summary>
        public TimeSpan TimeSaved { get; set; }

        /// <summary>
        /// Confidence level of the incremental selection.
        /// </summary>
        public double Confidence { get; set; }

        /// <summary>
        /// Test execution results.
        /// </summary>
        public List<TestExecutionResult> Results { get; set; } = new List<TestExecutionResult>();

        /// <summary>
        /// Whether fallback to full suite was used.
        /// </summary>
        public bool UsedFallback { get; set; }

        /// <summary>
        /// Error message if incremental testing failed.
        /// </summary>
        public string ErrorMessage { get; set; } = string.Empty;
    }

    /// <summary>
    /// Parallel execution metrics.
    /// </summary>
    public class ParallelExecutionMetrics
    {
        /// <summary>
        /// Maximum parallelism achieved.
        /// </summary>
        public int MaxParallelism { get; set; }

        /// <summary>
        /// Average parallelism during execution.
        /// </summary>
        public double AverageParallelism { get; set; }

        /// <summary>
        /// Total execution time.
        /// </summary>
        public TimeSpan TotalTime { get; set; }

        /// <summary>
        /// Sequential execution time (for comparison).
        /// </summary>
        public TimeSpan SequentialTime { get; set; }

        /// <summary>
        /// Speedup factor compared to sequential execution.
        /// </summary>
        public double SpeedupFactor { get; set; }

        /// <summary>
        /// Resource utilization efficiency.
        /// </summary>
        public double Efficiency { get; set; }
    }

    /// <summary>
    /// Resource utilization information.
    /// </summary>
    public class ResourceUtilization
    {
        /// <summary>
        /// Current CPU usage percentage.
        /// </summary>
        public double CpuUsagePercent { get; set; }

        /// <summary>
        /// Current memory usage in MB.
        /// </summary>
        public double MemoryUsageMB { get; set; }

        /// <summary>
        /// Available memory in MB.
        /// </summary>
        public double AvailableMemoryMB { get; set; }

        /// <summary>
        /// Number of available CPU cores.
        /// </summary>
        public int AvailableCores { get; set; }

        /// <summary>
        /// Recommended maximum parallelism.
        /// </summary>
        public int RecommendedMaxParallelism { get; set; }

        /// <summary>
        /// Whether resources are constrained.
        /// </summary>
        public bool IsResourceConstrained { get; set; }
    }

    /// <summary>
    /// Test execution result.
    /// </summary>
    public class TestExecutionResult
    {
        /// <summary>
        /// Test file path.
        /// </summary>
        public string TestFile { get; set; } = string.Empty;

        /// <summary>
        /// Whether the test passed.
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// Test execution time.
        /// </summary>
        public TimeSpan ExecutionTime { get; set; }

        /// <summary>
        /// Error message if test failed.
        /// </summary>
        public string ErrorMessage { get; set; } = string.Empty;

        /// <summary>
        /// Test output.
        /// </summary>
        public string Output { get; set; } = string.Empty;

        /// <summary>
        /// Exit code.
        /// </summary>
        public int ExitCode { get; set; }

        /// <summary>
        /// Whether the test was executed in parallel.
        /// </summary>
        public bool WasExecutedInParallel { get; set; }

        /// <summary>
        /// Phase in which the test was executed.
        /// </summary>
        public string ExecutionPhase { get; set; } = string.Empty;
    }

    /// <summary>
    /// Test dependency rule.
    /// </summary>
    public class TestDependencyRule
    {
        /// <summary>
        /// Test that depends on another.
        /// </summary>
        public string DependentTest { get; set; } = string.Empty;

        /// <summary>
        /// Test that the dependent test depends on.
        /// </summary>
        public string DependencyTest { get; set; } = string.Empty;

        /// <summary>
        /// Type of dependency.
        /// </summary>
        public DependencyType DependencyType { get; set; }

        /// <summary>
        /// Whether the dependency is required.
        /// </summary>
        public bool IsRequired { get; set; } = true;
    }

    /// <summary>
    /// Type of test dependency.
    /// </summary>
    public enum DependencyType
    {
        /// <summary>
        /// Execution dependency - must execute before.
        /// </summary>
        Execution,

        /// <summary>
        /// Data dependency - depends on data produced.
        /// </summary>
        Data,

        /// <summary>
        /// Resource dependency - depends on shared resources.
        /// </summary>
        Resource,

        /// <summary>
        /// Conditional dependency - depends under certain conditions.
        /// </summary>
        Conditional
    }

    /// <summary>
    /// Validation result for test orchestration options.
    /// </summary>
    public class TestOrchestrationValidation
    {
        /// <summary>
        /// Whether the options are valid.
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// Validation errors.
        /// </summary>
        public List<string> Errors { get; set; } = new List<string>();

        /// <summary>
        /// Validation warnings.
        /// </summary>
        public List<string> Warnings { get; set; } = new List<string>();
    }
} 