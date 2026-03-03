using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Maintenance.Models;
using Nexo.Core.Application.Maintenance.Ports;

namespace Nexo.Infrastructure.Maintenance;

/// <summary>
/// Orchestrator for artifact cleanup. Runs registered strategies; aggregates results.
/// </summary>
public sealed class ArtifactCleanupService : IArtifactCleanupService
{
    private readonly IReadOnlyList<IArtifactCleanupStrategy> _strategies;
    private readonly ILogger<ArtifactCleanupService> _logger;

    public ArtifactCleanupService(
        IEnumerable<IArtifactCleanupStrategy> strategies,
        ILogger<ArtifactCleanupService> logger)
    {
        _strategies = strategies?.ToList() ?? throw new ArgumentNullException(nameof(strategies));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public IReadOnlyList<string> GetAvailableStrategyIds()
        => _strategies.Select(s => s.Id).ToList();

    public async Task<ArtifactCleanupResult> CleanAsync(
        string? strategyId = null,
        ArtifactCleanupContext? context = null,
        CancellationToken ct = default)
    {
        var ctx = context ?? new ArtifactCleanupContext();
        var toRun = string.IsNullOrEmpty(strategyId)
            ? _strategies
            : _strategies.Where(s => string.Equals(s.Id, strategyId, StringComparison.OrdinalIgnoreCase)).ToList();

        if (toRun.Count == 0)
        {
            if (!string.IsNullOrEmpty(strategyId))
                _logger.LogWarning("No strategy found for id: {StrategyId}", strategyId);
            return new ArtifactCleanupResult(
                strategyId ?? "none",
                0,
                Array.Empty<string>(),
                string.IsNullOrEmpty(strategyId) ? Array.Empty<string>() : new[] { $"Strategy '{strategyId}' not found." });
        }

        var allPaths = new List<string>();
        var allErrors = new List<string>();
        long totalBytes = 0;

        foreach (var strategy in toRun)
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                var result = await strategy.CleanAsync(ctx, ct).ConfigureAwait(false);
                totalBytes += result.BytesReclaimed;
                allPaths.AddRange(result.PathsDeleted);
                allErrors.AddRange(result.Errors);
                _logger.LogInformation("Strategy {Id} reclaimed {Bytes} bytes, deleted {Count} path(s)",
                    result.StrategyId, result.BytesReclaimed, result.PathsDeleted.Count);
            }
            catch (Exception ex)
            {
                allErrors.Add($"{strategy.Id}: {ex.Message}");
                _logger.LogError(ex, "Strategy {Id} failed", strategy.Id);
            }
        }

        return new ArtifactCleanupResult(
            strategyId ?? "all",
            totalBytes,
            allPaths,
            allErrors);
    }
}
