using System.Text.Json;
using GameDirector.Domain;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nexo.Core.Application.Trust.Ports;
using Nexo.Core.Domain.Bricks;
using Nexo.Core.Domain.Execution;

namespace GameDirector.Agents;

public sealed class BalanceWatcherHostedService : BackgroundService
{
    private readonly ILogger<BalanceWatcherHostedService> _logger;
    private readonly GameDirectorOptions _options;
    private readonly Nexo.Core.Domain.Execution.IBrickRegistry _bricks;
    private readonly IDataDecisionAuditLog _auditLog;
    private readonly IActivityFeedPublisher _feed;

    public BalanceWatcherHostedService(
        ILogger<BalanceWatcherHostedService> logger,
        IOptions<GameDirectorOptions> options,
        Nexo.Core.Domain.Execution.IBrickRegistry bricks,
        IDataDecisionAuditLog auditLog,
        IActivityFeedPublisher feed)
    {
        _logger = logger;
        _options = options.Value;
        _bricks = bricks;
        _auditLog = auditLog;
        _feed = feed;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken) =>
        RunWatchLoopAsync(stoppingToken);

    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage(
        Justification = "BackgroundService loop body; covered indirectly through ScanOnceAsync and lifecycle integration tests.")]
    private async Task RunWatchLoopAsync(CancellationToken stoppingToken)
    {
        Directory.CreateDirectory(_options.BalanceWatchPath);
        try
        {
            if (_options.BalanceWatcherStartupDelay > TimeSpan.Zero)
                await Task.Delay(_options.BalanceWatcherStartupDelay, stoppingToken).ConfigureAwait(false);

            while (!stoppingToken.IsCancellationRequested)
            {
                await RunCycleAsync(stoppingToken).ConfigureAwait(false);
                await Task.Delay(_options.BalanceWatcherInterval, stoppingToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
            // graceful shutdown
        }
    }

    private async Task RunCycleAsync(CancellationToken stoppingToken)
    {
        try
        {
            await ScanBalanceSheetsAsync(stoppingToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "balance-watcher cycle failed");
            await _feed.PublishAsync("balance-watcher", "error", ex.Message, stoppingToken)
                .ConfigureAwait(false);
        }
    }

    /// <summary>Runs a single balance scan cycle (test seam).</summary>
    public Task ScanOnceAsync(CancellationToken ct) => ScanBalanceSheetsAsync(ct);

    private async Task ScanBalanceSheetsAsync(CancellationToken ct)
    {
        var dir = Path.GetFullPath(_options.BalanceWatchPath);
        if (!Directory.Exists(dir))
            return;

        foreach (var file in Directory.EnumerateFiles(dir, "*.json"))
        {
            var json = await File.ReadAllTextAsync(file, ct).ConfigureAwait(false);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            var sheetId = root.TryGetProperty("sheet_id", out var sid)
                ? sid.GetString() ?? Path.GetFileNameWithoutExtension(file)
                : Path.GetFileNameWithoutExtension(file);

            var input = new BrickInput();
            input.Set("sheet_id", sheetId);
            if (root.TryGetProperty("data", out var dataEl))
                input.Set("data", dataEl.Clone());
            else
                input.Set("data", JsonSerializer.SerializeToElement(Array.Empty<object>()));

            var brick = _bricks.GetBrick("balance.analysis")
                ?? throw new InvalidOperationException("balance.analysis brick not registered");
            var context = new Nexo.Infrastructure.Execution.ExecutionContext { AgentId = "balance-watcher" };
            var output = await brick.ExecuteAsync(input, ImplementationType.Deterministic, context, ct)
                .ConfigureAwait(false);

            var flaggedCount = output.ToDictionary().TryGetValue("flagged", out var f)
                                && f is System.Collections.ICollection col
                ? col.Count
                : 0;

            if (flaggedCount > 0)
            {
                var summary = output.Summary ?? $"Drift detected in {sheetId}";
                await _feed.PublishAsync("balance-watcher", "drift", summary, ct).ConfigureAwait(false);
                _logger.LogInformation("balance-watcher: {Summary}", summary);
            }
        }
    }
}

public interface IActivityFeedPublisher
{
    Task PublishAsync(string source, string eventType, string summary, CancellationToken ct);
}

public sealed class AuditLogActivityFeedPublisher : IActivityFeedPublisher
{
    private readonly IDataDecisionAuditLog _auditLog;

    public AuditLogActivityFeedPublisher(IDataDecisionAuditLog auditLog) => _auditLog = auditLog;

    public Task PublishAsync(string source, string eventType, string summary, CancellationToken ct)
    {
        _auditLog.LogClassification("game-director-activity", eventType, JsonSerializer.Serialize(new
        {
            source,
            summary,
            at = DateTimeOffset.UtcNow
        }));
        return Task.CompletedTask;
    }
}
