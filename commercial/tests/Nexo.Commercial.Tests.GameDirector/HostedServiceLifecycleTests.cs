using FluentAssertions;
using GameDirector.Agents;
using GameDirector.Domain;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Nexo.Core.Domain.Execution;
using Xunit;

namespace Nexo.Tests.GameDirector;

[Trait("Category", "GameDirectorApplication")]
public sealed class HostedServiceLifecycleTests
{
    [Fact]
    public async Task BalanceWatcher_full_execute_cycle_processes_files_and_responds_to_cancellation()
    {
        var dir = Path.Combine(Path.GetTempPath(), $"gd-bw-exec-{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);
        await File.WriteAllTextAsync(Path.Combine(dir, "drift.json"),
            "{\"sheet_id\":\"drift\",\"data\":[{\"id\":\"assault_rifle\",\"stats\":{\"ttk_ms\":1200}}]}");

        try
        {
            var audit = GameDirectorTestHost.CreateAuditLog();
            var feed = new RecordingActivityFeed();
            var watcher = new BalanceWatcherHostedService(
                NullLogger<BalanceWatcherHostedService>.Instance,
                Options.Create(new GameDirectorOptions
                {
                    BalanceWatchPath = dir,
                    BalanceWatcherStartupDelay = TimeSpan.Zero,
                    BalanceWatcherInterval = TimeSpan.FromMilliseconds(50)
                }),
                GameDirectorTestHost.CreateBrickRegistry(audit),
                audit,
                feed);

            using var lifetime = new CancellationTokenSource();
            await watcher.StartAsync(lifetime.Token);
            for (var i = 0; i < 20 && feed.Entries.Count == 0; i++)
                await Task.Delay(50, CancellationToken.None);
            lifetime.Cancel();
            await watcher.StopAsync(CancellationToken.None);

            feed.Entries.Should().Contain(e => e.EventType == "drift");
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public async Task BalanceWatcher_logs_error_when_brick_registry_is_unconfigured()
    {
        var dir = Path.Combine(Path.GetTempPath(), $"gd-bw-err-{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);
        await File.WriteAllTextAsync(Path.Combine(dir, "broken.json"), "{\"sheet_id\":\"x\",\"data\":[]}");

        try
        {
            var audit = GameDirectorTestHost.CreateAuditLog();
            var feed = new RecordingActivityFeed();
            var watcher = new BalanceWatcherHostedService(
                NullLogger<BalanceWatcherHostedService>.Instance,
                Options.Create(new GameDirectorOptions { BalanceWatchPath = dir }),
                new EmptyBrickRegistry(),
                audit,
                feed);

            var act = () => watcher.ScanOnceAsync(CancellationToken.None);

            await act.Should().ThrowAsync<InvalidOperationException>()
                .Where(e => e.Message.Contains("balance.analysis"));
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public async Task BalanceWatcher_skips_when_directory_missing()
    {
        var missing = Path.Combine(Path.GetTempPath(), $"gd-missing-{Guid.NewGuid():N}");

        var audit = GameDirectorTestHost.CreateAuditLog();
        var feed = new RecordingActivityFeed();
        var watcher = new BalanceWatcherHostedService(
            NullLogger<BalanceWatcherHostedService>.Instance,
            Options.Create(new GameDirectorOptions { BalanceWatchPath = missing }),
            GameDirectorTestHost.CreateBrickRegistry(audit),
            audit,
            feed);

        await watcher.ScanOnceAsync(CancellationToken.None);
        feed.Entries.Should().BeEmpty();
    }

    [Fact]
    public async Task MapValidator_full_execute_cycle_validates_existing_files()
    {
        var dir = Path.Combine(Path.GetTempPath(), $"gd-mv-exec-{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);
        await File.WriteAllTextAsync(Path.Combine(dir, "init.mapconfig"),
            "{\"map_id\":\"m\",\"config\":{\"tiles\":[[{\"x\":0,\"y\":0,\"traversalOptions\":1}]]," +
            "\"spawns\":[{\"id\":\"s1\",\"x\":0,\"y\":0}]}}");

        try
        {
            var audit = GameDirectorTestHost.CreateAuditLog();
            var feed = new RecordingActivityFeed();
            var validator = new MapValidatorHostedService(
                NullLogger<MapValidatorHostedService>.Instance,
                Options.Create(new GameDirectorOptions
                {
                    MapWatchPath = dir,
                    MapValidatorFullScanInterval = TimeSpan.FromMilliseconds(1),
                    MapValidatorLoopInterval = TimeSpan.FromMilliseconds(50)
                }),
                GameDirectorTestHost.CreateBrickRegistry(audit),
                feed);

            using var lifetime = new CancellationTokenSource();
            await validator.StartAsync(lifetime.Token);
            for (var i = 0; i < 40 && !feed.Entries.Any(e => e.EventType == "validation"); i++)
                await Task.Delay(50, CancellationToken.None);
            lifetime.Cancel();
            await validator.StopAsync(CancellationToken.None);
            validator.Dispose();

            feed.Entries.Should().Contain(e => e.Source == "map-validator" && e.EventType == "validation");
        }
        finally
        {
            try { Directory.Delete(dir, recursive: true); } catch { }
        }
    }

    [Fact]
    public async Task MapValidator_skips_missing_file_without_error()
    {
        var dir = Path.Combine(Path.GetTempPath(), $"gd-mv-skip-{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);
        try
        {
            var audit = GameDirectorTestHost.CreateAuditLog();
            var feed = new RecordingActivityFeed();
            var validator = new MapValidatorHostedService(
                NullLogger<MapValidatorHostedService>.Instance,
                Options.Create(new GameDirectorOptions { MapWatchPath = dir }),
                GameDirectorTestHost.CreateBrickRegistry(audit),
                feed);

            await validator.ValidateFileOnceAsync(Path.Combine(dir, "missing.mapconfig"), CancellationToken.None);
            feed.Entries.Should().BeEmpty();
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public async Task MapValidator_unexpected_brick_failure_is_logged_without_throwing()
    {
        var dir = Path.Combine(Path.GetTempPath(), $"gd-mv-fail-{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);
        var file = Path.Combine(dir, "broken.mapconfig");
        await File.WriteAllTextAsync(file, "{\"map_id\":\"x\",\"config\":{}}");

        try
        {
            var feed = new RecordingActivityFeed();
            var validator = new MapValidatorHostedService(
                NullLogger<MapValidatorHostedService>.Instance,
                Options.Create(new GameDirectorOptions { MapWatchPath = dir }),
                new EmptyBrickRegistry(),
                feed);

            await validator.ValidateFileOnceAsync(file, CancellationToken.None);

            feed.Entries.Should().NotContain(e => e.EventType == "validation");
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    private sealed class RecordingActivityFeed : IActivityFeedPublisher
    {
        public List<(string Source, string EventType, string Summary)> Entries { get; } = [];
        public Task PublishAsync(string source, string eventType, string summary, CancellationToken ct)
        {
            Entries.Add((source, eventType, summary));
            return Task.CompletedTask;
        }
    }

    private sealed class EmptyBrickRegistry : IBrickRegistry
    {
        public Nexo.Core.Domain.Bricks.Brick? GetBrick(string id) => null;
        public IReadOnlyList<Nexo.Core.Domain.Bricks.Brick> GetAllBricks() => [];
        public void Register(Nexo.Core.Domain.Bricks.Brick brick) { }
    }
}
