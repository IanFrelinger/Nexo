using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Ashlar.Core.Application.Common.Ports;
using Ashlar.Hosting;
using Ashlar.Infrastructure.Metrics;
using Ashlar.Tests.Infrastructure.Helpers;
using Xunit;

namespace Ashlar.Tests.Infrastructure.Tests.Hosting;

/// <summary>Tests for open telemetry.</summary>
[Trait("Category", "E2E")]
public sealed class OpenTelemetryTests
{
    [Fact(Timeout = TestTimeouts.E2E)]
    public async Task AddAshlarOpenTelemetry_BuildsProvider_WithoutError()
    {
        await Task.CompletedTask;
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAshlar();
        services.AddAshlarOpenTelemetry();

        var sp = services.BuildServiceProvider();

        sp.Should().NotBeNull();

        var collector = sp.GetRequiredService<IMetricsCollector>();
        collector.Should().NotBeNull();
        collector.Should().BeOfType<OpenTelemetryMetricsCollector>();
    }
}
