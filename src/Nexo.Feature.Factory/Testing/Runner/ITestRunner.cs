using Microsoft.Extensions.Logging;
using Nexo.Feature.Factory.Testing.Models;

namespace Nexo.Feature.Factory.Testing.Runner
{
    /// <summary>
    /// Interface for C#-based test runners that provide better control over test execution.
    /// </summary>
    public interface ITestRunner
    {
        /// <summary>
        /// Gets the name of the test runner.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets the version of the test runner.
        /// </summary>
        string Version { get; }

        /// <summary>
        /// Runs all tests with the specified configuration.
        /// </summary>
        /// <param name="configuration">Test configuration</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Test execution results</returns>
        Task<TestExecutionSummary> RunAllTestsAsync(TestConfiguration configuration, CancellationToken cancellationToken = default);

        /// <summary>
        /// Runs tests matching the specified filter.
        /// </summary>
        /// <param name="configuration">Test configuration</param>
        /// <param name="filter">Test filter criteria</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Test execution results</returns>
        Task<TestExecutionSummary> RunFilteredTestsAsync(TestConfiguration configuration, TestFilter filter, CancellationToken cancellationToken = default);

        /// <summary>
        /// Discovers all available tests.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of discovered tests</returns>
        Task<IEnumerable<TestInfo>> DiscoverTestsAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Validates the test runner configuration.
        /// </summary>
        /// <param name="configuration">Test configuration</param>
        /// <returns>Validation result</returns>
        Task<TestValidationResult> ValidateConfigurationAsync(TestConfiguration configuration);
    }
}
