using Nexo.Core.Application.SelfImprovement.Models;

namespace Nexo.Core.Application.SelfImprovement.Ports;

/// <summary>
/// Runs the self-improvement loop: test failures → adaptation → validation → promotion.
/// </summary>
public interface ISelfImprovementLoop
{
    /// <summary>Runs one self-improvement cycle and persists the resulting report.</summary>
    /// <param name="ct">Token used to cancel the operation.</param>
    Task RunOnceAsync(CancellationToken ct = default);

    /// <summary>Starts continuous self-improvement until the token is cancelled.</summary>
    /// <param name="ct">Token used to cancel the operation.</param>
    Task StartContinuousAsync(CancellationToken ct = default);

    /// <summary>Gets the report from the most recent loop run, if any.</summary>
    /// <param name="ct">Token used to cancel the operation.</param>
    /// <returns>The last run report, or null when no run has completed.</returns>
    Task<SelfImprovementReport?> GetLastRunReportAsync(CancellationToken ct = default);
}
