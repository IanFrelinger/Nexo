using System;
using System.Threading;
using System.Threading.Tasks;
using Nexo.Feature.Factory.Testing.Models;

namespace Nexo.Feature.Factory.Testing.Commands
{
    /// <summary>
    /// Base interface for all test commands in the Feature Factory testing system.
    /// </summary>
    public interface ITestCommand
    {
        /// <summary>
        /// Gets the unique identifier for this test command.
        /// </summary>
        string CommandId { get; }

        /// <summary>
        /// Gets the name of the test command.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets the description of what this test command does.
        /// </summary>
        string Description { get; }

        /// <summary>
        /// Gets the category this test command belongs to.
        /// </summary>
        TestCategory Category { get; }

        /// <summary>
        /// Gets the priority of this test command.
        /// </summary>
        TestPriority Priority { get; }

        /// <summary>
        /// Gets the estimated duration for this test command.
        /// </summary>
        TimeSpan EstimatedDuration { get; }

        /// <summary>
        /// Gets whether this test command can be executed in parallel with other commands.
        /// </summary>
        bool CanExecuteInParallel { get; }

        /// <summary>
        /// Gets the dependencies that must be completed before this command can execute.
        /// </summary>
        string[] Dependencies { get; }

        /// <summary>
        /// Validates the test command configuration and prerequisites.
        /// </summary>
        /// <param name="context">The test context</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Validation result</returns>
        Task<TestValidationResult> ValidateAsync(ITestContext context, CancellationToken cancellationToken = default);

        /// <summary>
        /// Executes the test command.
        /// </summary>
        /// <param name="context">The test context</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Test execution result</returns>
        Task<TestExecutionResult> ExecuteAsync(ITestContext context, CancellationToken cancellationToken = default);

        /// <summary>
        /// Cleans up resources after test execution.
        /// </summary>
        /// <param name="context">The test context</param>
        /// <param name="result">The test execution result</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Cleanup result</returns>
        Task<TestCleanupResult> CleanupAsync(ITestContext context, TestExecutionResult result, CancellationToken cancellationToken = default);
    }
}
