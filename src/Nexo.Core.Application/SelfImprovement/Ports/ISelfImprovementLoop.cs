using Nexo.Core.Application.SelfImprovement.Models;

namespace Nexo.Core.Application.SelfImprovement.Ports;

/// <summary>
/// Runs the self-improvement loop: test failures → adaptation → validation → promotion.
/// </summary>
public interface ISelfImprovementLoop
{
    Task RunOnceAsync(CancellationToken ct = default);
    Task StartContinuousAsync(CancellationToken ct = default);
    Task<SelfImprovementReport?> GetLastRunReportAsync(CancellationToken ct = default);
}
