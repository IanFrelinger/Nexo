using System.Text.Json;
using GameDirector.Domain;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nexo.Core.Application.Trust.Ports;
using Nexo.Core.Domain.Bricks;
using Nexo.Core.Domain.Execution;

namespace GameDirector.Agents;

public sealed class MapValidatorHostedService : BackgroundService
{
    private readonly ILogger<MapValidatorHostedService> _logger;
    private readonly GameDirectorOptions _options;
    private readonly Nexo.Core.Domain.Execution.IBrickRegistry _bricks;
    private readonly IActivityFeedPublisher _feed;
    private FileSystemWatcher? _watcher;
    private DateTimeOffset _lastFullScan = DateTimeOffset.MinValue;

    public MapValidatorHostedService(
        ILogger<MapValidatorHostedService> logger,
        IOptions<GameDirectorOptions> options,
        Nexo.Core.Domain.Execution.IBrickRegistry bricks,
        IActivityFeedPublisher feed)
    {
        _logger = logger;
        _options = options.Value;
        _bricks = bricks;
        _feed = feed;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken) =>
        RunWatchLoopAsync(stoppingToken);

    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage(
        Justification = "BackgroundService loop body; covered indirectly through ValidateFileOnceAsync and lifecycle integration tests.")]
    private async Task RunWatchLoopAsync(CancellationToken stoppingToken)
    {
        var dir = Path.GetFullPath(_options.MapWatchPath);
        Directory.CreateDirectory(dir);

        _watcher = new FileSystemWatcher(dir, "*.mapconfig")
        {
            IncludeSubdirectories = true,
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName
        };
        _watcher.Changed += (_, e) => _ = ValidateFileAsync(e.FullPath, stoppingToken);
        _watcher.Created += (_, e) => _ = ValidateFileAsync(e.FullPath, stoppingToken);
        _watcher.EnableRaisingEvents = true;

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                if (DateTimeOffset.UtcNow - _lastFullScan > _options.MapValidatorFullScanInterval)
                {
                    foreach (var file in Directory.EnumerateFiles(dir, "*.mapconfig", SearchOption.AllDirectories))
                        await ValidateFileAsync(file, stoppingToken).ConfigureAwait(false);
                    _lastFullScan = DateTimeOffset.UtcNow;
                }

                await Task.Delay(_options.MapValidatorLoopInterval, stoppingToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
            // graceful shutdown
        }
    }

    public override void Dispose()
    {
        _watcher?.Dispose();
        base.Dispose();
    }

    /// <summary>Validates one map file (test seam).</summary>
    public Task ValidateFileOnceAsync(string path, CancellationToken ct) => ValidateFileAsync(path, ct);

    private async Task ValidateFileAsync(string path, CancellationToken ct)
    {
        try
        {
            if (!File.Exists(path))
                return;

            var json = await File.ReadAllTextAsync(path, ct).ConfigureAwait(false);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            var mapId = root.TryGetProperty("map_id", out var mid)
                ? mid.GetString() ?? Path.GetFileNameWithoutExtension(path)
                : Path.GetFileNameWithoutExtension(path);

            var input = new BrickInput();
            input.Set("map_id", mapId);
            input.Set("config", root.TryGetProperty("config", out var cfg) ? cfg : root);

            var brick = _bricks.GetBrick("map.flow")
                ?? throw new InvalidOperationException("map.flow brick not registered");
            var context = new Nexo.Infrastructure.Execution.ExecutionContext { AgentId = "map-validator" };
            var output = await brick.ExecuteAsync(input, ImplementationType.Deterministic, context, ct)
                .ConfigureAwait(false);

            var summary = output.Summary ?? $"Validated map {mapId}";
            await _feed.PublishAsync("map-validator", "validation", summary, ct).ConfigureAwait(false);
            _logger.LogInformation("map-validator: {Path} — {Summary}", path, summary);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning("map-validator parse error: {Path} — {Message}", path, ex.Message);
            await _feed.PublishAsync("map-validator", "parse_error", $"{path}: {ex.Message}", ct)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "map-validator failed for {Path}", path);
        }
    }
}
