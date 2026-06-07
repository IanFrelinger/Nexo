using System.Text.Json;
using FluentAssertions;
using GameDirector.Agents;
using GameDirector.Bricks.Schemas;
using GameDirector.Domain;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Nexo.Core.Domain.Bricks;
using Nexo.Core.Domain.Execution;
using Nexo.Infrastructure.Execution;
using Xunit;

namespace Nexo.Tests.GameDirector;

[Trait("Category", "GameDirectorApplication")]
public sealed class AgentWatcherApplicationTests
{
    [Fact]
    public async Task BalanceWatcher_scans_json_sheet_and_flags_drift()
    {
        var dir = Path.Combine(Path.GetTempPath(), $"gd-balance-{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);
        var sheetPath = Path.Combine(dir, "weapons.json");
        await File.WriteAllTextAsync(sheetPath, JsonSerializer.Serialize(new
        {
            sheet_id = "watcher_test",
            data = new[] { new { id = "assault_rifle", stats = new { ttk_ms = 1200, damage = 12, fire_rate = 600 } } }
        }));

        try
        {
            var audit = GameDirectorTestHost.CreateAuditLog();
            var registry = GameDirectorTestHost.CreateBrickRegistry(audit);
            var feed = new CapturingActivityFeed();
            var opts = Options.Create(new GameDirectorOptions
            {
                BalanceWatchPath = dir,
                BalanceWatcherInterval = TimeSpan.FromHours(1)
            });
            var watcher = new BalanceWatcherHostedService(
                NullLogger<BalanceWatcherHostedService>.Instance,
                opts,
                registry,
                audit,
                feed);

            await watcher.ScanOnceAsync(CancellationToken.None);

            feed.Entries.Should().Contain(e =>
                e.Source == "balance-watcher" &&
                e.EventType == "drift");
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public async Task MapValidator_parses_mapconfig_and_posts_validation()
    {
        var dir = Path.Combine(Path.GetTempPath(), $"gd-maps-{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);
        var mapPath = Path.Combine(dir, "test.mapconfig");
        await File.WriteAllTextAsync(mapPath, JsonSerializer.Serialize(new
        {
            map_id = "agent_map",
            config = new
            {
                tiles = new[] { new[] { new { x = 0, y = 0, traversalOptions = 1 } } },
                spawns = new[] { new { id = "s1", x = 0, y = 0 } }
            }
        }));

        try
        {
            var audit = GameDirectorTestHost.CreateAuditLog();
            var registry = GameDirectorTestHost.CreateBrickRegistry(audit);
            var feed = new CapturingActivityFeed();
            var opts = Options.Create(new GameDirectorOptions { MapWatchPath = dir });
            var validator = new MapValidatorHostedService(
                NullLogger<MapValidatorHostedService>.Instance,
                opts,
                registry,
                feed);

            await validator.ValidateFileOnceAsync(mapPath, CancellationToken.None);

            feed.Entries.Should().Contain(e => e.Source == "map-validator");
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public async Task BalanceWatcher_balanced_sheet_does_not_publish_drift()
    {
        var dir = Path.Combine(Path.GetTempPath(), $"gd-balance-ok-{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);
        var sheetPath = Path.Combine(dir, "balanced.json");
        await File.WriteAllTextAsync(sheetPath, JsonSerializer.Serialize(new
        {
            sheet_id = "balanced",
            data = new[] { new { id = "assault_rifle", stats = new { ttk_ms = 800, damage = 12, fire_rate = 600 } } }
        }));

        try
        {
            var audit = GameDirectorTestHost.CreateAuditLog();
            var registry = GameDirectorTestHost.CreateBrickRegistry(audit);
            var feed = new CapturingActivityFeed();
            var watcher = new BalanceWatcherHostedService(
                NullLogger<BalanceWatcherHostedService>.Instance,
                Options.Create(new GameDirectorOptions { BalanceWatchPath = dir }),
                registry,
                audit,
                feed);

            await watcher.ScanOnceAsync(CancellationToken.None);

            feed.Entries.Should().NotContain(e => e.EventType == "drift");
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public async Task MapValidator_malformed_json_logs_parse_error()
    {
        var dir = Path.Combine(Path.GetTempPath(), $"gd-maps-{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);
        var badPath = Path.Combine(dir, "bad.mapconfig");
        await File.WriteAllTextAsync(badPath, "{ not valid json");

        try
        {
            var audit = GameDirectorTestHost.CreateAuditLog();
            var registry = GameDirectorTestHost.CreateBrickRegistry(audit);
            var feed = new CapturingActivityFeed();
            var validator = new MapValidatorHostedService(
                NullLogger<MapValidatorHostedService>.Instance,
                Options.Create(new GameDirectorOptions { MapWatchPath = dir }),
                registry,
                feed);

            await validator.ValidateFileOnceAsync(badPath, CancellationToken.None);

            feed.Entries.Should().Contain(e => e.EventType == "parse_error");
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    private sealed class CapturingActivityFeed : IActivityFeedPublisher
    {
        public List<(string Source, string EventType, string Summary)> Entries { get; } = [];

        public Task PublishAsync(string source, string eventType, string summary, CancellationToken ct)
        {
            Entries.Add((source, eventType, summary));
            return Task.CompletedTask;
        }
    }
}
