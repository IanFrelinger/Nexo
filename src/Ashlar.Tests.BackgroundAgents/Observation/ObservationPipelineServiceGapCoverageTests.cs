using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Ashlar.BackgroundAgents.Observation;
using Ashlar.Core.Application.Observation.Models;
using Ashlar.Core.Application.Observation.Ports;
using Ashlar.Core.Application.Trust.Ports;
using Ashlar.Infrastructure.Observation;
using Xunit;

namespace Ashlar.Tests.BackgroundAgents.Observation;

/// <summary>Tests for observation pipeline service gap coverage.</summary>
public sealed class ObservationPipelineServiceGapCoverageTests
{
    [Fact]
    public void Constructor_rejects_null_dependencies()
    {
        var options = Options.Create(new ObservationPipelineOptions());
        var store = new LiteDbPatternStore(Path.Combine(Path.GetTempPath(), $"ashlar_obs_gap_{Guid.NewGuid():N}.db"));

        var act1 = () => new ObservationPipelineService(null!, store, NullLogger<ObservationPipelineService>.Instance, NullLoggerFactory.Instance);
        act1.Should().Throw<ArgumentNullException>();

        var act2 = () => new ObservationPipelineService(options, null!, NullLogger<ObservationPipelineService>.Instance, NullLoggerFactory.Instance);
        act2.Should().Throw<ArgumentNullException>();

        var act3 = () => new ObservationPipelineService(options, store, null!, NullLoggerFactory.Instance);
        act3.Should().Throw<ArgumentNullException>();

        var act4 = () => new ObservationPipelineService(options, store, NullLogger<ObservationPipelineService>.Instance, null!);
        act4.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public async Task ExecuteAsync_with_no_existing_watch_paths_idles_until_cancelled()
    {
        var root = Path.Combine(Path.GetTempPath(), "ashlar-obs-idle-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var options = Options.Create(new ObservationPipelineOptions
            {
                RepoRoot = root,
                StorePath = $"ashlar_test_{Guid.NewGuid():N}.db",
                WatchPaths = new[] { "missing-dir" },
            });
            var storePath = Path.Combine(root, options.Value.StorePath);
            var service = new ObservationPipelineService(
                options,
                new LiteDbPatternStore(storePath),
                NullLogger<ObservationPipelineService>.Instance,
                NullLoggerFactory.Instance);

            using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(200));
            await service.StartAsync(cts.Token);
            await Task.Delay(50);
            await service.StopAsync(CancellationToken.None);
        }
        finally
        {
            if (Directory.Exists(root))
                Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task ExecuteAsync_processes_events_when_watch_path_exists()
    {
        var root = Path.Combine(Path.GetTempPath(), "ashlar-obs-watch-" + Guid.NewGuid().ToString("N"));
        var watchDir = Path.Combine(root, "src");
        Directory.CreateDirectory(watchDir);
        try
        {
            var options = Options.Create(new ObservationPipelineOptions
            {
                RepoRoot = root,
                StorePath = $"ashlar_test_{Guid.NewGuid():N}.db",
                WatchPaths = new[] { "src" },
                PatternWindowSeconds = 5,
                RepeatedEditThreshold = 1,
            });
            var storePath = Path.Combine(root, options.Value.StorePath);
            var service = new ObservationPipelineService(
                options,
                new LiteDbPatternStore(storePath),
                NullLogger<ObservationPipelineService>.Instance,
                NullLoggerFactory.Instance);

            using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(800));
            await service.StartAsync(cts.Token);
            await File.WriteAllTextAsync(Path.Combine(watchDir, "touch.cs"), "// ping");
            await Task.Delay(200);
            await service.StopAsync(CancellationToken.None);
        }
        finally
        {
            if (Directory.Exists(root))
                Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task ExecuteAsync_skips_events_blocked_by_observation_gate()
    {
        var root = Path.Combine(Path.GetTempPath(), "ashlar-obs-gate-" + Guid.NewGuid().ToString("N"));
        var watchDir = Path.Combine(root, "src");
        Directory.CreateDirectory(watchDir);
        try
        {
            var store = new Mock<IPatternStore>();
            var gate = new Mock<IObservationGate>();
            gate.Setup(g => g.ShouldObserve(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>()))
                .Returns(false);

            var options = Options.Create(new ObservationPipelineOptions
            {
                RepoRoot = root,
                StorePath = $"ashlar_test_{Guid.NewGuid():N}.db",
                WatchPaths = new[] { "src" },
            });

            var service = new ObservationPipelineService(
                options,
                store.Object,
                NullLogger<ObservationPipelineService>.Instance,
                NullLoggerFactory.Instance,
                gate.Object);

            using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(800));
            await service.StartAsync(cts.Token);
            await File.WriteAllTextAsync(Path.Combine(watchDir, "blocked.cs"), "// blocked");
            await Task.Delay(250);
            await service.StopAsync(CancellationToken.None);

            store.Verify(
                s => s.AddAsync(It.IsAny<ObservedPattern>(), It.IsAny<CancellationToken>()),
                Times.Never);
            gate.Verify(
                g => g.ShouldObserve(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>()),
                Times.AtLeastOnce);
        }
        finally
        {
            if (Directory.Exists(root))
                Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task ExecuteAsync_degrades_gracefully_on_storage_io_errors()
    {
        var root = Path.Combine(Path.GetTempPath(), "ashlar-obs-io-" + Guid.NewGuid().ToString("N"));
        var watchDir = Path.Combine(root, "src");
        Directory.CreateDirectory(watchDir);
        try
        {
            var store = new Mock<IPatternStore>();
            store.Setup(s => s.AddAsync(It.IsAny<ObservedPattern>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new IOException("store unavailable"));

            var options = Options.Create(new ObservationPipelineOptions
            {
                RepoRoot = root,
                StorePath = $"ashlar_test_{Guid.NewGuid():N}.db",
                WatchPaths = new[] { "src" },
                PatternWindowSeconds = 30,
                RepeatedEditThreshold = 1,
            });

            var service = new ObservationPipelineService(
                options,
                store.Object,
                NullLogger<ObservationPipelineService>.Instance,
                NullLoggerFactory.Instance);

            using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(1200));
            await service.StartAsync(cts.Token);

            var target = Path.Combine(watchDir, "repeat.cs");
            for (var i = 0; i < 3; i++)
            {
                await File.WriteAllTextAsync(target, $"// edit {i}");
                await Task.Delay(80);
            }

            await Task.Delay(200);
            await service.StopAsync(CancellationToken.None);

            store.Verify(
                s => s.AddAsync(It.IsAny<ObservedPattern>(), It.IsAny<CancellationToken>()),
                Times.AtLeastOnce);
        }
        finally
        {
            if (Directory.Exists(root))
                Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task ExecuteAsync_rethrows_unexpected_pipeline_failures()
    {
        var root = Path.Combine(Path.GetTempPath(), "ashlar-obs-fail-" + Guid.NewGuid().ToString("N"));
        var watchDir = Path.Combine(root, "src");
        Directory.CreateDirectory(watchDir);
        try
        {
            var store = new Mock<IPatternStore>();
            store.Setup(s => s.AddAsync(It.IsAny<ObservedPattern>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("unexpected store failure"));

            var options = Options.Create(new ObservationPipelineOptions
            {
                RepoRoot = root,
                StorePath = $"ashlar_test_{Guid.NewGuid():N}.db",
                WatchPaths = new[] { "src" },
                PatternWindowSeconds = 30,
                RepeatedEditThreshold = 1,
            });

            var service = new ObservationPipelineService(
                options,
                store.Object,
                NullLogger<ObservationPipelineService>.Instance,
                NullLoggerFactory.Instance);

            using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(1200));
            await service.StartAsync(cts.Token);

            var target = Path.Combine(watchDir, "boom.cs");
            for (var i = 0; i < 3; i++)
            {
                await File.WriteAllTextAsync(target, $"// boom {i}");
                await Task.Delay(80);
            }

            await Task.Delay(300);

            var act = async () => await service.StopAsync(CancellationToken.None);
            await act.Should().NotThrowAsync();
        }
        finally
        {
            if (Directory.Exists(root))
                Directory.Delete(root, recursive: true);
        }
    }
}
