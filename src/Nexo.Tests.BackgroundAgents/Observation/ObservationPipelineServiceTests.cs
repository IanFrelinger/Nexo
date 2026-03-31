using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Nexo.BackgroundAgents.Observation;
using Nexo.Infrastructure.Observation;
using Xunit;

namespace Nexo.Tests.BackgroundAgents.Observation;

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
