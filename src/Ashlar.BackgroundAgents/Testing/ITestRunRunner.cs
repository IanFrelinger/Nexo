namespace Ashlar.BackgroundAgents.Testing;

/// <summary>
/// Abstraction for running tests from a background agent (e.g. post-deployment self-tester).
/// The host supplies an implementation that calls the application's test pipeline (e.g. RunTestsCommand / ITestRunner).
/// Used to dog-food: the framework runs tester agents that execute its own tests.
/// </summary>
public interface ITestRunRunner
{
    /// <summary>
    /// Run tests, optionally filtered by category or name.
    /// </summary>
    /// <param name="filter">Optional filter (e.g. category or test name filter).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Summary result for logging and agent use.</returns>
    Task<TestRunResult> RunAsync(string? filter, CancellationToken cancellationToken = default);
}
