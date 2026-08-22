using Microsoft.Extensions.DependencyInjection;
using Ashlar.Core.Application.Common.Ports;
using Ashlar.Infrastructure.Metrics;
using OpenTelemetry.Metrics;

namespace Ashlar.Hosting.Sdk.Extensions;
/// <summary>
/// OpenTelemetry integration for Ashlar. Call AddAshlarOpenTelemetry() after AddAshlar() to enable
/// metrics export via OpenTelemetry (OTLP, Console, etc.).
/// </summary>
public static class OpenTelemetryServiceCollectionExtensions
{
    /// <summary>
    /// Adds OpenTelemetry metrics for Ashlar and replaces IMetricsCollector with OpenTelemetryMetricsCollector.
    /// Call after AddAshlar(). Optionally configure the MeterProviderBuilder (e.g. AddConsoleExporter, AddOtlpExporter).
    /// </summary>
    /// <param name="services">The service collection (after AddAshlar).</param>
    /// <param name="configure">Optional action to configure the MeterProviderBuilder.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddAshlarOpenTelemetry(
        this IServiceCollection services,
        Action<MeterProviderBuilder>? configure = null)
    {
        services.AddOpenTelemetry()
            .WithMetrics(m =>
            {
                m.AddMeter(OpenTelemetryMetricsCollector.MeterName);
                configure?.Invoke(m);
            });

        // Replace default MemoryMetricsCollector with OpenTelemetryMetricsCollector (last registration wins)
        services.AddSingleton<IMetricsCollector>(sp =>
            new OpenTelemetryMetricsCollector(
                sp.GetService<Microsoft.Extensions.Logging.ILogger<OpenTelemetryMetricsCollector>>()));

        return services;
    }
}
