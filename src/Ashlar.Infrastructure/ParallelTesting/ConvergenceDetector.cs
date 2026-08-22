using Ashlar.Core.Application.ParallelTesting.Models;
using Ashlar.Core.Application.ParallelTesting.Ports;

namespace Ashlar.Infrastructure.ParallelTesting;

/// <summary>
/// Detects convergence: stops when all tests pass, or when no improvement for 3 consecutive rounds.
/// </summary>
public sealed class ConvergenceDetector : IConvergenceDetector
{
    /// <inheritdoc />
    public bool HasConverged(IReadOnlyList<AggregatedTestResult> history)
    {
        if (history.Count == 0) return false;
        var last = history[^1];
        if (last.AllPassed) return true;
        if (history.Count >= 3)
        {
            var recent = history.TakeLast(3).ToList();
            var passedCounts = recent.Select(r => r.Results.Count(x => x.Passed)).ToList();
            if (passedCounts[1] <= passedCounts[0] && passedCounts[2] <= passedCounts[1])
                return true;
        }
        return false;
    }
}
