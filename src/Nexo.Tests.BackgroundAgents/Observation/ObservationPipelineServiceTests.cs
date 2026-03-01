using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Nexo.BackgroundAgents.Observation;
using Nexo.Core.Application.Observation.Ports;
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
}
