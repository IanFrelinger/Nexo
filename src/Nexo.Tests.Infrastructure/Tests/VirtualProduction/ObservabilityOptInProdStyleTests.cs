using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Options;
using Nexo.Core.Application.Common.Ports;
using Nexo.Infrastructure.Metrics;
using Nexo.Tests.Infrastructure.Helpers;
using Nexo.Tests.Infrastructure.Helpers.VirtualProduction;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.VirtualProduction;

/// <summary>
/// ProdStyle coverage for the opt-in observability switches on the real Nexo.API host:
/// <list type="bullet">
///   <item>Default: human-readable console logging and the in-process <see cref="MemoryMetricsCollector"/>;
///   no OpenTelemetry providers are built.</item>
///   <item><c>NEXO_LOG_JSON=1</c> / <c>Nexo:Logging:Json=true</c>: the console provider switches to the JSON
///   formatter and the host still boots.</item>
///   <item><c>OTEL_EXPORTER_OTLP_ENDPOINT</c> pointing at an unreachable local port: the host boots and serves
///   <c>/health</c> (the exporter batches in the background and must never fail startup),
///   <c>IMetricsCollector</c> becomes the OpenTelemetry-backed collector, and both a
///   <see cref="TracerProvider"/> and a <see cref="MeterProvider"/> are registered.</item>
/// </list>
/// Program.cs reads every switch through <c>builder.Configuration</c>, so the values are injected with
/// <c>UseSetting</c> only; no process environment variables are touched.
/// </summary>
[Collection("Integration")]
[Trait("Category", "Integration")]
[Trait("Category", "ProdStyle")]
public sealed class ObservabilityOptInProdStyleTests
{
    // Port 1 (tcpmux) is closed on every developer and CI host we run on: the connect is refused
    // immediately, which is exactly the "collector is down" shape operators hit.
    private const string UnreachableOtlpEndpoint = "http://127.0.0.1:1";

    private static WebApplicationFactory<Program> CreateFactory(IDictionary<string, string?>? settings = null)
        => new NexoApiWebApplicationFactory().WithWebHostBuilder(builder =>
        {
            if (settings is null)
            {
                return;
            }

            foreach (var pair in settings)
            {
                builder.UseSetting(pair.Key, pair.Value);
            }
        });

    private static async Task AssertHealthyAsync(WebApplicationFactory<Program> factory)
    {
        using var client = factory.CreateClient();
        var health = await client.GetAsync("/health");
        health.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact(Timeout = TestTimeouts.HostTouching)]
    public async Task Default_host_keeps_plain_console_and_in_process_metrics()
    {
        using var factory = CreateFactory();
        await AssertHealthyAsync(factory);

        var consoleOptions = factory.Services.GetRequiredService<IOptionsMonitor<ConsoleLoggerOptions>>().CurrentValue;
        consoleOptions.FormatterName.Should().NotBe(ConsoleFormatterNames.Json, "JSON console output is opt-in");

        factory.Services.GetRequiredService<IMetricsCollector>().Should().BeOfType<MemoryMetricsCollector>(
            "without OTEL_EXPORTER_OTLP_ENDPOINT the in-process collector stays the default");
        factory.Services.GetService<TracerProvider>().Should().BeNull("no OTLP endpoint means no tracer provider");
        factory.Services.GetService<MeterProvider>().Should().BeNull("no OTLP endpoint means no meter provider");
    }

    [Fact(Timeout = TestTimeouts.HostTouching)]
    public async Task Json_console_logging_is_switched_on_by_the_env_shorthand_and_boots()
    {
        using var factory = CreateFactory(new Dictionary<string, string?>
        {
            ["NEXO_LOG_JSON"] = "1",
        });
        await AssertHealthyAsync(factory);

        var consoleOptions = factory.Services.GetRequiredService<IOptionsMonitor<ConsoleLoggerOptions>>().CurrentValue;
        consoleOptions.FormatterName.Should().Be(ConsoleFormatterNames.Json);
    }

    [Fact(Timeout = TestTimeouts.HostTouching)]
    public async Task Json_console_logging_is_switched_on_by_the_configuration_key_and_boots()
    {
        using var factory = CreateFactory(new Dictionary<string, string?>
        {
            ["Nexo:Logging:Json"] = "true",
        });
        await AssertHealthyAsync(factory);

        var consoleOptions = factory.Services.GetRequiredService<IOptionsMonitor<ConsoleLoggerOptions>>().CurrentValue;
        consoleOptions.FormatterName.Should().Be(ConsoleFormatterNames.Json);
    }

    [Fact(Timeout = TestTimeouts.HostTouching)]
    public async Task Otlp_endpoint_on_an_unreachable_port_enables_export_without_failing_startup()
    {
        using var factory = CreateFactory(new Dictionary<string, string?>
        {
            ["OTEL_EXPORTER_OTLP_ENDPOINT"] = UnreachableOtlpEndpoint,
            // Bounds the exporter's per-batch wait so host disposal stays quick when the port is dark.
            ["OTEL_EXPORTER_OTLP_TIMEOUT"] = "2000",
            ["NEXO_LOG_JSON"] = "1",
        });
        await AssertHealthyAsync(factory);

        factory.Services.GetRequiredService<IMetricsCollector>().Should().BeOfType<OpenTelemetryMetricsCollector>(
            "AddNexoOpenTelemetry replaces the in-process collector once OTLP export is on");
        factory.Services.GetService<TracerProvider>().Should().NotBeNull("traces are exported over OTLP");
        factory.Services.GetService<MeterProvider>().Should().NotBeNull("metrics are exported over OTLP");

        // Recording through the swapped collector must not throw even though nothing is listening.
        var collector = factory.Services.GetRequiredService<IMetricsCollector>();
        var act = () =>
        {
            collector.IncrementCounter("ncr.model_load.success");
            collector.RecordExecutionTime("ncr.model_resolution.duration", TimeSpan.FromMilliseconds(3));
        };
        act.Should().NotThrow();
    }
}
