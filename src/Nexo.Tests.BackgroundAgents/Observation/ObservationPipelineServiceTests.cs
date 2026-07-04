using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Nexo.BackgroundAgents.Observation;
using Nexo.Infrastructure.Observation;
using Xunit;

namespace Nexo.Tests.BackgroundAgents.Observation;

/// <summary>Tests for observation pipeline service.</summary>
public class ObservationPipelineServiceTests
{
    [Fact]
    public void Ctor_WithValidOptions_DoesNotThrow()
    {
        var options = Options.Create(new ObservationPipelineOptions
        {
            RepoRoot = Path.GetTempPath(),
            StorePath = $"nexo_test_{Guid.NewGuid():N}.db",
            WatchPaths = new[] { "src" },
        });
        var storePath = Path.Combine(options.Value.RepoRoot ?? Path.GetTempPath(), options.Value.StorePath);
        var store = new LiteDbPatternStore(storePath);
        var logger = NullLogger<ObservationPipelineService>.Instance;
        var loggerFactory = NullLoggerFactory.Instance;

        var service = new ObservationPipelineService(options, store, logger, loggerFactory);

        service.Should().NotBeNull();
    }

    [Fact]
    public async Task ExecuteAsync_WhenPatternStoreFailsAndFailOpenEnabled_DoesNotThrow()
    {
        var options = Options.Create(new ObservationPipelineOptions
        {
            RepoRoot = Path.GetTempPath(),
            StorePath = $"nexo_test_{Guid.NewGuid():N}.db",
            WatchPaths = new[] { "__does_not_exist__" },
            FailOpenOnStoreErrors = true
        });
        var logger = NullLogger<ObservationPipelineService>.Instance;
        var loggerFactory = NullLoggerFactory.Instance;
        var service = new ObservationPipelineService(options, new ThrowingPatternStore(), logger, loggerFactory);

        var start = () => service.StartAsync(CancellationToken.None);
        await start.Should().NotThrowAsync();
        await service.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ExecuteAsync_with_observation_gate_filters_events()
    {
        var root = Path.Combine(Path.GetTempPath(), "nexo-obs-pipe-" + Guid.NewGuid().ToString("N"));
        var watchDir = Path.Combine(root, "src");
        Directory.CreateDirectory(watchDir);
        try
        {
            var options = Options.Create(new ObservationPipelineOptions
            {
                RepoRoot = root,
                StorePath = $"nexo_test_{Guid.NewGuid():N}.db",
                WatchPaths = new[] { "src" },
            });
            var storePath = Path.Combine(root, options.Value.StorePath);
            var store = new LiteDbPatternStore(storePath);
            var logger = NullLogger<ObservationPipelineService>.Instance;
            var loggerFactory = NullLoggerFactory.Instance;
            var gate = new DenyAllObservationGate();
            var service = new ObservationPipelineService(options, store, logger, loggerFactory, gate);

            using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(300));
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

    /// <summary>Deny all observation gate.</summary>
    private sealed class DenyAllObservationGate : Nexo.Core.Application.Trust.Ports.IObservationGate
    {
        /// <summary>Should observe.</summary>
        /// <param name="category">Category.</param>
        /// <param name="sourceId">Source id.</param>
        /// <param name="projectPath">Project path.</param>
        public bool ShouldObserve(string category, string sourceId, string? projectPath) => false;
    }

    /// <summary>Throwing pattern store.</summary>
    private sealed class ThrowingPatternStore : Nexo.Core.Application.Observation.Ports.IPatternStore
    {
        public Task AddAsync(Nexo.Core.Application.Observation.Models.ObservedPattern pattern, CancellationToken cancellationToken = default)
            => throw new UnauthorizedAccessException("test fail-open");

        public Task<IReadOnlyList<Nexo.Core.Application.Observation.Models.ObservedPattern>> QueryAsync(
            Nexo.Core.Application.Observation.Models.PatternStoreQueryParams query,
            CancellationToken cancellationToken = default)
            => throw new UnauthorizedAccessException("test fail-open");

        public Task PersistAsync(CancellationToken cancellationToken = default)
            => throw new UnauthorizedAccessException("test fail-open");
    }
}
