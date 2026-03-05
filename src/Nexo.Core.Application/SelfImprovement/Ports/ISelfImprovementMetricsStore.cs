using Nexo.Core.Application.SelfImprovement.Models;

namespace Nexo.Core.Application.SelfImprovement.Ports;

/// <summary>
/// Persists self-improvement metrics (including holdout pass rate) for nexo metrics display.
/// P3.4: Holdout pass rate tracked in metrics.
/// </summary>
public interface ISelfImprovementMetricsStore
{
    /// <summary>
    /// Saves the latest report (called when self-improvement loop completes).
    /// </summary>
    Task SaveAsync(SelfImprovementReport report, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the last saved report, or null if none.
    /// </summary>
    Task<SelfImprovementReport?> GetLastAsync(CancellationToken cancellationToken = default);
}
