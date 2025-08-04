using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Nexo.Feature.Analysis.Interfaces
{
    /// <summary>
    /// Interface for executing individual tests.
    /// </summary>
    public interface ITestExecutionEngine
    {
        /// <summary>
        /// Executes a single test file.
        /// </summary>
        /// <param name="testFile">Path to the test file to execute.</param>
        /// <param name="timeout">Execution timeout.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Test execution result.</returns>
        Task<TestExecutionResult> ExecuteTestAsync(string testFile, TimeSpan timeout, CancellationToken cancellationToken = default);

        /// <summary>
        /// Executes a test with specific options.
        /// </summary>
        /// <param name="testFile">Path to the test file to execute.</param>
        /// <param name="options">Test execution options.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Test execution result.</returns>
        Task<TestExecutionResult> ExecuteTestAsync(string testFile, TestExecutionOptions options, CancellationToken cancellationToken = default);

        /// <summary>
        /// Validates that a test file can be executed.
        /// </summary>
        /// <param name="testFile">Path to the test file to validate.</param>
        /// <returns>Validation result.</returns>
        TestExecutionValidation ValidateTestFile(string testFile);

        /// <summary>
        /// Gets test metadata without executing the test.
        /// </summary>
        /// <param name="testFile">Path to the test file.</param>
        /// <returns>Test metadata.</returns>
        Task<TestMetadata> GetTestMetadataAsync(string testFile);
    }

    /// <summary>
    /// Options for test execution.
    /// </summary>
    public class TestExecutionOptions
    {
        /// <summary>
        /// Execution timeout.
        /// </summary>
        public TimeSpan Timeout { get; set; } = TimeSpan.FromMinutes(5);

        /// <summary>
        /// Whether to capture output.
        /// </summary>
        public bool CaptureOutput { get; set; } = true;

        /// <summary>
        /// Whether to capture error output.
        /// </summary>
        public bool CaptureErrorOutput { get; set; } = true;

        /// <summary>
        /// Environment variables to set.
        /// </summary>
        public Dictionary<string, string> EnvironmentVariables { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// Working directory for test execution.
        /// </summary>
        public string WorkingDirectory { get; set; } = string.Empty;

        /// <summary>
        /// Additional arguments to pass to the test runner.
        /// </summary>
        public List<string> AdditionalArguments { get; set; } = new List<string>();

        /// <summary>
        /// Whether to run in isolation.
        /// </summary>
        public bool RunInIsolation { get; set; } = false;

        /// <summary>
        /// Retry count on failure.
        /// </summary>
        public int RetryCount { get; set; } = 0;

        /// <summary>
        /// Delay between retries.
        /// </summary>
        public TimeSpan RetryDelay { get; set; } = TimeSpan.FromSeconds(1);
    }

    /// <summary>
    /// Validation result for test execution.
    /// </summary>
    public class TestExecutionValidation
    {
        /// <summary>
        /// Whether the test file is valid for execution.
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

        /// <summary>
        /// Estimated execution time.
        /// </summary>
        public TimeSpan EstimatedExecutionTime { get; set; }

        /// <summary>
        /// Test file type.
        /// </summary>
        public string TestFileType { get; set; } = string.Empty;
    }

    /// <summary>
    /// Metadata about a test file.
    /// </summary>
    public class TestMetadata
    {
        /// <summary>
        /// Test file path.
        /// </summary>
        public string TestFile { get; set; } = string.Empty;

        /// <summary>
        /// Test file type.
        /// </summary>
        public string TestFileType { get; set; } = string.Empty;

        /// <summary>
        /// Number of test methods.
        /// </summary>
        public int TestMethodCount { get; set; }

        /// <summary>
        /// Test categories.
        /// </summary>
        public List<string> Categories { get; set; } = new List<string>();

        /// <summary>
        /// Estimated execution time.
        /// </summary>
        public TimeSpan EstimatedExecutionTime { get; set; }

        /// <summary>
        /// Dependencies.
        /// </summary>
        public List<string> Dependencies { get; set; } = new List<string>();

        /// <summary>
        /// Whether the test requires isolation.
        /// </summary>
        public bool RequiresIsolation { get; set; }

        /// <summary>
        /// Whether the test is slow.
        /// </summary>
        public bool IsSlow { get; set; }

        /// <summary>
        /// Whether the test is flaky.
        /// </summary>
        public bool IsFlaky { get; set; }
    }
} 