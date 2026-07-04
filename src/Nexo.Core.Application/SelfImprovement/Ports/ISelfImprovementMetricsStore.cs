using Nexo.Core.Application.SelfImprovement.Models;

namespace Nexo.Core.Application.SelfImprovement.Ports;

/// <summary>
/// Persists self-improvement metrics (including holdout pass rate) for nexo metrics display.
/// P3.4: Holdout pass rate tracked in metrics.
/// </summary>
public interface ISelfImprovementMetricsStore
{
    /// <summary>Saves the latest report when a self-improvement loop completes.</summary>
    /// <param name="report">Report produced by the completed loop run.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    Task SaveAsync(SelfImprovementReport report, CancellationToken cancellationToken = default);

    /// <summary>Gets the last saved report, or null if none exists.</summary>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The most recently persisted report, or null.</returns>
    Task<SelfImprovementReport?> GetLastAsync(CancellationToken cancellationToken = default);
}
