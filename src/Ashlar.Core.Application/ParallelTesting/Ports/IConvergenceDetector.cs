using Ashlar.Core.Application.ParallelTesting.Models;

namespace Ashlar.Core.Application.ParallelTesting.Ports;

/// <summary>
/// Detects when to stop iterating (all pass or improvement rate below threshold).
/// </summary>
public interface IConvergenceDetector
{
    bool HasConverged(IReadOnlyList<AggregatedTestResult> history);
}
