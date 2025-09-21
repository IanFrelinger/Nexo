using System.Diagnostics;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nexo.Observability.ActivitySources;
using Nexo.Observability.Configuration;
using Nexo.Observability.Metrics;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Nexo.Observability;

/// <summary>
/// Extension methods for adding OpenTelemetry observability to the service collection.
/// </summary>
public static class ObservabilityServiceCollectionExtensions
{
    /// <summary>
    /// Adds OpenTelemetry observability services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddNexoObservability(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Configure options
        services.Configure<ObservabilityOptions>(configuration.GetSection(ObservabilityOptions.SectionName));

        // Add OpenTelemetry services
        services.AddOpenTelemetry()
            .ConfigureResource(ConfigureResource)
            .WithTracing(ConfigureTracing)
            .WithMetrics(ConfigureMetrics);

        // Register ActivitySources as singletons
        services.AddSingleton<ActivitySource>(provider =>
        {
            var options = provider.GetRequiredService<IOptions<ObservabilityOptions>>().Value;
            return new ActivitySource(options.ServiceName);
        });

        // Register custom meters
        services.AddSingleton<Meter>(provider =>
        {
            var options = provider.GetRequiredService<IOptions<ObservabilityOptions>>().Value;
            return new Meter($"{options.ServiceName}.Pipeline");
        });

        // Register metrics
        services.AddSingleton<PipelineMetrics>();

        // Register activity sources
        services.AddSingleton(_ => NexoActivitySources.Generation);
        services.AddSingleton(_ => NexoActivitySources.Validation);
        services.AddSingleton(_ => NexoActivitySources.Policy);
        services.AddSingleton(_ => NexoActivitySources.Pipeline);
        services.AddSingleton(_ => NexoActivitySources.Repair);
        services.AddSingleton(_ => NexoActivitySources.Observability);

        return services;
    }

    /// <summary>
    /// Configures the OpenTelemetry resource.
    /// </summary>
    /// <param name="builder">The resource builder.</param>
    private static void ConfigureResource(ResourceBuilder builder)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var version = assembly.GetName().Version?.ToString() ?? "1.0.0";

        builder
            .AddService("Nexo", version)
            .AddAttributes(new Dictionary<string, object>
            {
                ["service.namespace"] = "Nexo",
                ["service.instance.id"] = Environment.MachineName,
                ["deployment.environment"] = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development"
            });
    }

    /// <summary>
    /// Configures OpenTelemetry tracing.
    /// </summary>
    /// <param name="builder">The tracing builder.</param>
    private static void ConfigureTracing(IServiceProvider serviceProvider, TracerProviderBuilder builder)
    {
        var options = serviceProvider.GetRequiredService<IOptions<ObservabilityOptions>>().Value;

        // Add sources
        builder
            .AddSource(NexoActivitySources.Generation.Name)
            .AddSource(NexoActivitySources.Validation.Name)
            .AddSource(NexoActivitySources.Policy.Name)
            .AddSource(NexoActivitySources.Pipeline.Name)
            .AddSource(NexoActivitySources.Repair.Name)
            .AddSource(NexoActivitySources.Observability.Name);

        // Add instrumentation
        builder
            .AddHttpClientInstrumentation()
            .AddAspNetCoreInstrumentation()
            .AddSqlClientInstrumentation();

        // Configure sampling
        ConfigureSampling(builder, options.Sampling);

        // Add exporters
        if (options.Console.Enabled)
        {
            builder.AddConsoleExporter(options =>
            {
                options.IncludeScopes = options.IncludeScopes;
                options.IncludeActivityTags = options.IncludeActivityTags;
            });
        }

        if (options.Otlp.Enabled && !string.IsNullOrEmpty(options.Otlp.Endpoint))
        {
            builder.AddOtlpExporter(otlpOptions =>
            {
                otlpOptions.Endpoint = new Uri(options.Otlp.Endpoint);
                otlpOptions.Protocol = options.Otlp.Protocol == OtlpProtocol.Grpc 
                    ? OtlpExportProtocol.Grpc 
                    : OtlpExportProtocol.HttpProtobuf;
                
                if (options.Otlp.Headers.Any())
                {
                    otlpOptions.Headers = string.Join(",", options.Otlp.Headers.Select(kvp => $"{kvp.Key}={kvp.Value}"));
                }
            });
        }
    }

    /// <summary>
    /// Configures OpenTelemetry metrics.
    /// </summary>
    /// <param name="builder">The metrics builder.</param>
    private static void ConfigureMetrics(IServiceProvider serviceProvider, MeterProviderBuilder builder)
    {
        var options = serviceProvider.GetRequiredService<IOptions<ObservabilityOptions>>().Value;

        // Add runtime instrumentation
        builder.AddRuntimeInstrumentation();

        // Add custom meters
        builder.AddMeter("Nexo.Pipeline");

        // Add exporters
        if (options.Console.Enabled)
        {
            builder.AddConsoleExporter();
        }

        if (options.Otlp.Enabled && !string.IsNullOrEmpty(options.Otlp.Endpoint))
        {
            builder.AddOtlpExporter(otlpOptions =>
            {
                otlpOptions.Endpoint = new Uri(options.Otlp.Endpoint);
                otlpOptions.Protocol = options.Otlp.Protocol == OtlpProtocol.Grpc 
                    ? OtlpExportProtocol.Grpc 
                    : OtlpExportProtocol.HttpProtobuf;
                
                if (options.Otlp.Headers.Any())
                {
                    otlpOptions.Headers = string.Join(",", options.Otlp.Headers.Select(kvp => $"{kvp.Key}={kvp.Value}"));
                }
            });
        }
    }

    /// <summary>
    /// Configures sampling based on the provided options.
    /// </summary>
    /// <param name="builder">The tracing builder.</param>
    /// <param name="sampling">The sampling options.</param>
    private static void ConfigureSampling(TracerProviderBuilder builder, SamplingOptions sampling)
    {
        switch (sampling.Type)
        {
            case SamplingType.AlwaysOn:
                builder.SetSampler(new AlwaysOnSampler());
                break;
            case SamplingType.AlwaysOff:
                builder.SetSampler(new AlwaysOffSampler());
                break;
            case SamplingType.TraceIdRatioBased:
                builder.SetSampler(new TraceIdRatioBasedSampler(sampling.Ratio));
                break;
        }
    }
}
