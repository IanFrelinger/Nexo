using System;
using System.Collections.Generic;
using System.Threading;

namespace Nexo.Feature.Factory.Testing.Models
{
    /// <summary>
    /// Represents the context for test execution.
    /// </summary>
    public interface ITestContext
    {
        /// <summary>
        /// Gets the test session identifier.
        /// </summary>
        string SessionId { get; }

        /// <summary>
        /// Gets the test configuration.
        /// </summary>
        TestConfiguration Configuration { get; }

        /// <summary>
        /// Gets the shared data between test commands.
        /// </summary>
        IDictionary<string, object> SharedData { get; }

        /// <summary>
        /// Gets the cancellation token for the test session.
        /// </summary>
        CancellationToken CancellationToken { get; }

        /// <summary>
        /// Gets the logger for the test session.
        /// </summary>
        Microsoft.Extensions.Logging.ILogger Logger { get; }
    }

    /// <summary>
    /// Represents the configuration for test execution.
    /// </summary>
    public sealed class TestConfiguration
    {
        /// <summary>
        /// Gets or sets the maximum parallel test executions.
        /// </summary>
        public int MaxParallelExecutions { get; set; } = 3;

        /// <summary>
        /// Gets or sets the default test timeout.
        /// </summary>
        public TimeSpan DefaultTimeout { get; set; } = TimeSpan.FromMinutes(5);

        /// <summary>
        /// Gets or sets the timeout for AI connectivity tests.
        /// </summary>
        public TimeSpan AiConnectivityTimeout { get; set; } = TimeSpan.FromSeconds(30);

        /// <summary>
        /// Gets or sets the timeout for domain analysis tests.
        /// </summary>
        public TimeSpan DomainAnalysisTimeout { get; set; } = TimeSpan.FromMinutes(2);

        /// <summary>
        /// Gets or sets the timeout for code generation tests.
        /// </summary>
        public TimeSpan CodeGenerationTimeout { get; set; } = TimeSpan.FromMinutes(3);

        /// <summary>
        /// Gets or sets the timeout for end-to-end tests.
        /// </summary>
        public TimeSpan EndToEndTimeout { get; set; } = TimeSpan.FromMinutes(5);

        /// <summary>
        /// Gets or sets the timeout for performance tests.
        /// </summary>
        public TimeSpan PerformanceTimeout { get; set; } = TimeSpan.FromMinutes(2);

        /// <summary>
        /// Gets or sets the timeout for validation tests.
        /// </summary>
        public TimeSpan ValidationTimeout { get; set; } = TimeSpan.FromSeconds(10);

        /// <summary>
        /// Gets or sets the timeout for cleanup operations.
        /// </summary>
        public TimeSpan CleanupTimeout { get; set; } = TimeSpan.FromSeconds(30);

        /// <summary>
        /// Gets or sets whether to enable detailed logging.
        /// </summary>
        public bool EnableDetailedLogging { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to enable performance monitoring.
        /// </summary>
        public bool EnablePerformanceMonitoring { get; set; } = true;

        /// <summary>
        /// Gets or sets the output directory for test results.
        /// </summary>
        public string OutputDirectory { get; set; } = "./test-results";

        /// <summary>
        /// Gets or sets whether to clean up test artifacts after execution.
        /// </summary>
        public bool CleanupAfterExecution { get; set; } = true;

        /// <summary>
        /// Gets or sets the AI provider to use for testing.
        /// </summary>
        public string AiProvider { get; set; } = "ollama";

        /// <summary>
        /// Gets or sets the AI model to use for testing.
        /// </summary>
        public string AiModel { get; set; } = "codellama";

        /// <summary>
        /// Gets or sets whether to use mock AI responses for faster testing.
        /// </summary>
        public bool UseMockAiResponses { get; set; } = false;

        /// <summary>
        /// Gets the appropriate timeout for a specific test command.
        /// </summary>
        /// <param name="commandId">The command ID</param>
        /// <returns>The timeout duration</returns>
        public TimeSpan GetTimeoutForCommand(string commandId)
        {
            return commandId switch
            {
                "validate-ai-connectivity" => AiConnectivityTimeout,
                "validate-domain-analysis" => DomainAnalysisTimeout,
                "validate-code-generation" => CodeGenerationTimeout,
                "validate-end-to-end" => EndToEndTimeout,
                "validate-performance" => PerformanceTimeout,
                _ => DefaultTimeout
            };
        }
    }

    /// <summary>
    /// Represents the result of test validation.
    /// </summary>
    public sealed class TestValidationResult
    {
        /// <summary>
        /// Gets whether the validation was successful.
        /// </summary>
        public bool IsValid { get; }

        /// <summary>
        /// Gets the validation errors.
        /// </summary>
        public IReadOnlyList<string> Errors { get; }

        /// <summary>
        /// Gets the validation warnings.
        /// </summary>
        public IReadOnlyList<string> Warnings { get; }

        /// <summary>
        /// Gets the validation duration.
        /// </summary>
        public TimeSpan Duration { get; }

        public TestValidationResult(bool isValid, IReadOnlyList<string>? errors = null, IReadOnlyList<string>? warnings = null, TimeSpan duration = default)
        {
            IsValid = isValid;
            Errors = errors ?? new List<string>();
            Warnings = warnings ?? new List<string>();
            Duration = duration;
        }
    }

    /// <summary>
    /// Represents the result of test execution.
    /// </summary>
    public sealed class TestExecutionResult
    {
        /// <summary>
        /// Gets whether the test execution was successful.
        /// </summary>
        public bool IsSuccess { get; }

        /// <summary>
        /// Gets the test execution duration.
        /// </summary>
        public TimeSpan Duration { get; }

        /// <summary>
        /// Gets the error message if the test failed.
        /// </summary>
        public string? ErrorMessage { get; }

        /// <summary>
        /// Gets the test output data.
        /// </summary>
        public IDictionary<string, object> OutputData { get; }

        /// <summary>
        /// Gets the performance metrics.
        /// </summary>
        public TestPerformanceMetrics? PerformanceMetrics { get; }

        /// <summary>
        /// Gets the test artifacts created during execution.
        /// </summary>
        public IReadOnlyList<TestArtifact> Artifacts { get; }

        public TestExecutionResult(
            bool isSuccess, 
            TimeSpan duration, 
            string? errorMessage = null, 
            IDictionary<string, object>? outputData = null,
            TestPerformanceMetrics? performanceMetrics = null,
            IReadOnlyList<TestArtifact>? artifacts = null)
        {
            IsSuccess = isSuccess;
            Duration = duration;
            ErrorMessage = errorMessage;
            OutputData = outputData ?? new Dictionary<string, object>();
            PerformanceMetrics = performanceMetrics;
            Artifacts = artifacts ?? new List<TestArtifact>();
        }
    }

    /// <summary>
    /// Represents the result of test cleanup.
    /// </summary>
    public sealed class TestCleanupResult
    {
        /// <summary>
        /// Gets whether the cleanup was successful.
        /// </summary>
        public bool IsSuccess { get; }

        /// <summary>
        /// Gets the cleanup duration.
        /// </summary>
        public TimeSpan Duration { get; }

        /// <summary>
        /// Gets the error message if cleanup failed.
        /// </summary>
        public string? ErrorMessage { get; }

        /// <summary>
        /// Gets the number of artifacts cleaned up.
        /// </summary>
        public int ArtifactsCleanedUp { get; }

        public TestCleanupResult(bool isSuccess, TimeSpan duration, string? errorMessage = null, int artifactsCleanedUp = 0)
        {
            IsSuccess = isSuccess;
            Duration = duration;
            ErrorMessage = errorMessage;
            ArtifactsCleanedUp = artifactsCleanedUp;
        }
    }

    /// <summary>
    /// Represents performance metrics for a test execution.
    /// </summary>
    public sealed class TestPerformanceMetrics
    {
        /// <summary>
        /// Gets the CPU usage percentage.
        /// </summary>
        public double CpuUsagePercentage { get; }

        /// <summary>
        /// Gets the memory usage in bytes.
        /// </summary>
        public long MemoryUsageBytes { get; }

        /// <summary>
        /// Gets the number of AI API calls made.
        /// </summary>
        public int AiApiCalls { get; }

        /// <summary>
        /// Gets the total AI processing time.
        /// </summary>
        public TimeSpan AiProcessingTime { get; }

        /// <summary>
        /// Gets the number of files created.
        /// </summary>
        public int FilesCreated { get; }

        /// <summary>
        /// Gets the total file size created in bytes.
        /// </summary>
        public long TotalFileSizeBytes { get; }

        public TestPerformanceMetrics(
            double cpuUsagePercentage,
            long memoryUsageBytes,
            int aiApiCalls,
            TimeSpan aiProcessingTime,
            int filesCreated,
            long totalFileSizeBytes)
        {
            CpuUsagePercentage = cpuUsagePercentage;
            MemoryUsageBytes = memoryUsageBytes;
            AiApiCalls = aiApiCalls;
            AiProcessingTime = aiProcessingTime;
            FilesCreated = filesCreated;
            TotalFileSizeBytes = totalFileSizeBytes;
        }
    }

    /// <summary>
    /// Represents a test artifact created during test execution.
    /// </summary>
    public sealed class TestArtifact
    {
        /// <summary>
        /// Gets the artifact name.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the artifact type.
        /// </summary>
        public TestArtifactType Type { get; }

        /// <summary>
        /// Gets the artifact file path.
        /// </summary>
        public string FilePath { get; }

        /// <summary>
        /// Gets the artifact size in bytes.
        /// </summary>
        public long SizeBytes { get; }

        /// <summary>
        /// Gets the artifact creation time.
        /// </summary>
        public DateTimeOffset CreatedAt { get; }

        /// <summary>
        /// Gets whether this artifact should be cleaned up after testing.
        /// </summary>
        public bool ShouldCleanup { get; }

        public TestArtifact(string name, TestArtifactType type, string filePath, long sizeBytes, DateTimeOffset createdAt, bool shouldCleanup = true)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Type = type;
            FilePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
            SizeBytes = sizeBytes;
            CreatedAt = createdAt;
            ShouldCleanup = shouldCleanup;
        }
    }

    /// <summary>
    /// Represents the type of test artifact.
    /// </summary>
    public enum TestArtifactType
    {
        GeneratedCode,
        TestResult,
        LogFile,
        ConfigurationFile,
        TemporaryFile,
        Report
    }

    /// <summary>
    /// Represents the category of a test command.
    /// </summary>
    public enum TestCategory
    {
        Unit,
        Integration,
        EndToEnd,
        E2E,
        Performance,
        Security,
        Validation,
        Functional
    }

    /// <summary>
    /// Represents the priority of a test command.
    /// </summary>
    public enum TestPriority
    {
        Low,
        Medium,
        High,
        Critical
    }
}
