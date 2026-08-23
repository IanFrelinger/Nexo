using Ashlar.Core.Application.Maintenance.Models;

namespace Ashlar.Core.Application.Maintenance.Ports;

/// <summary>
/// Port for artifact cleanup orchestration. Runs registered strategies on demand.
/// Implemented by Infrastructure; used by CLI, RunTestsHandler, and agent tools.
/// </summary>
public interface IArtifactCleanupService
{
    /// <summary>Returns IDs of all registered cleanup strategies.</summary>
    IReadOnlyList<string> GetAvailableStrategyIds();

    /// <summary>Runs cleanup. When strategyId is null, runs all registered strategies.</summary>
    /// <param name="strategyId">Optional strategy ID to run; null = all strategies.</param>
    /// <param name="context">Optional context (repo root, options).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Aggregated result with bytes reclaimed and any errors.</returns>
    Task<ArtifactCleanupResult> CleanAsync(
        string? strategyId = null,
        ArtifactCleanupContext? context = null,
        CancellationToken ct = default);
}
