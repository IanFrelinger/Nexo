using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Nexo.Core.Application.Common.Ports;
using Nexo.Hosting;
using Nexo.Infrastructure.Metrics;
using Nexo.Tests.Infrastructure.Helpers;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.Hosting;

/// <summary>Tests for open telemetry.</summary>
[Trait("Category", "E2E")]
public sealed class OpenTelemetryTests
{
    [Fact(Timeout = TestTimeouts.E2E)]
    public async Task AddNexoOpenTelemetry_BuildsProvider_WithoutError()
    {
        await Task.CompletedTask;
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddNexo();
        services.AddNexoOpenTelemetry();

        var sp = services.BuildServiceProvider();

        sp.Should().NotBeNull();

        var collector = sp.GetRequiredService<IMetricsCollector>();
        collector.Should().NotBeNull();
        collector.Should().BeOfType<OpenTelemetryMetricsCollector>();
    }
}
