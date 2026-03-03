using Nexo.Core.Application.ParallelTesting.Models;

namespace Nexo.Core.Application.Composition.Ports;

/// <summary>
/// Runs tests through a composed agent pipeline (test-discovery → test-execution → result-aggregation).
/// Phase D: Composition-driven testing.
/// </summary>
public interface IComposedTestRunner
{
    /// <summary>
    /// Composes an agent for the problem description and runs the test pipeline if the composition
    /// includes test-runner components.
    /// </summary>
    /// <param name="problemDescription">e.g. "test Nexo suite"</param>
    /// <param name="scenario">Test scenario (project path, filters)</param>
    /// <param name="instanceCount">Number of parallel instances (default 1)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Aggregated result, or null if composition did not yield a test-runner pipeline</returns>
    Task<AggregatedTestResult?> RunViaCompositionAsync(
        string problemDescription,
        Scenario scenario,
        int instanceCount = 1,
        CancellationToken cancellationToken = default);
}
