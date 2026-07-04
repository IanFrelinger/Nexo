using Microsoft.Extensions.DependencyInjection;
using Nexo.Core.Application.Common.Ports;
using Nexo.Infrastructure.Metrics;
using OpenTelemetry.Metrics;

namespace Nexo.Hosting.Sdk.Extensions;
/// <summary>
/// OpenTelemetry integration for Nexo. Call AddNexoOpenTelemetry() after AddNexo() to enable
/// metrics export via OpenTelemetry (OTLP, Console, etc.).
/// </summary>
public static class OpenTelemetryServiceCollectionExtensions
{
    /// <summary>
    /// Adds OpenTelemetry metrics for Nexo and replaces IMetricsCollector with OpenTelemetryMetricsCollector.
    /// Call after AddNexo(). Optionally configure the MeterProviderBuilder (e.g. AddConsoleExporter, AddOtlpExporter).
    /// </summary>
    /// <param name="services">The service collection (after AddNexo).</param>
    /// <param name="configure">Optional action to configure the MeterProviderBuilder.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddNexoOpenTelemetry(
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
