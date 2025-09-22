using System.Diagnostics;
using System.Diagnostics.Metrics;
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
        var serviceName = Environment.GetEnvironmentVariable("OTEL_SERVICE_NAME") ?? "Nexo";

        var resourceAttributes = new Dictionary<string, object>
        {
            ["service.name"] = serviceName,
            ["service.version"] = version,
            ["service.namespace"] = "Nexo",
            ["service.instance.id"] = Environment.MachineName,
            ["deployment.environment"] = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development"
        };

        // Add custom resource attributes from environment
        var customAttributes = Environment.GetEnvironmentVariable("OTEL_RESOURCE_ATTRIBUTES");
        if (!string.IsNullOrEmpty(customAttributes))
        {
            var pairs = customAttributes.Split(',', StringSplitOptions.RemoveEmptyEntries);
            foreach (var pair in pairs)
            {
                var keyValue = pair.Split('=', 2);
                if (keyValue.Length == 2)
                {
                    resourceAttributes[keyValue[0].Trim()] = keyValue[1].Trim();
                }
            }
        }

        builder.AddService(serviceName, version)
               .AddAttributes(resourceAttributes);
    }

    /// <summary>
    /// Configures OpenTelemetry tracing.
    /// </summary>
    /// <param name="builder">The tracing builder.</param>
    private static void ConfigureTracing(TracerProviderBuilder builder)
    {
        // Note: We'll configure options through environment variables and defaults

        // Add sources
        builder
            .AddSource(NexoActivitySources.Generation.Name)
            .AddSource(NexoActivitySources.Validation.Name)
            .AddSource(NexoActivitySources.Policy.Name)
            .AddSource(NexoActivitySources.Pipeline.Name)
            .AddSource(NexoActivitySources.Repair.Name)
            .AddSource(NexoActivitySources.Observability.Name);

        // Add instrumentation

        // Configure sampling - use default sampling
        builder.SetSampler(new AlwaysOnSampler());

        // Add exporters - always add console exporter unless explicitly disabled
        var consoleEnabled = !string.Equals(Environment.GetEnvironmentVariable("OTEL_CONSOLE_DISABLED"), "true", StringComparison.OrdinalIgnoreCase);
        
        if (consoleEnabled)
        {
            builder.AddConsoleExporter();
        }

        // Add OTLP exporter if endpoint is configured
        var otlpEndpoint = Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT");
        if (!string.IsNullOrEmpty(otlpEndpoint))
        {
            builder.AddOtlpExporter(otlpOptions =>
            {
                otlpOptions.Endpoint = new Uri(otlpEndpoint);
                var protocol = Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_PROTOCOL")?.ToLowerInvariant();
                otlpOptions.Protocol = protocol switch
                {
                    "http" or "http/protobuf" => OtlpExportProtocol.HttpProtobuf,
                    "grpc" => OtlpExportProtocol.Grpc,
                    _ => OtlpExportProtocol.Grpc
                };
            });
        }
    }

    /// <summary>
    /// Configures OpenTelemetry metrics.
    /// </summary>
    /// <param name="builder">The metrics builder.</param>
    private static void ConfigureMetrics(MeterProviderBuilder builder)
    {
        // Note: We'll configure options through environment variables and defaults

        // Add runtime instrumentation
        builder.AddRuntimeInstrumentation();

        // Add custom meters
        builder.AddMeter("Nexo.Pipeline");
        builder.AddMeter("Nexo.Generation");
        builder.AddMeter("Nexo.Validation");
        builder.AddMeter("Nexo.Policy");
        builder.AddMeter("Nexo.Repair");

        // Add exporters - always add console exporter unless explicitly disabled
        var consoleEnabled = !string.Equals(Environment.GetEnvironmentVariable("OTEL_CONSOLE_DISABLED"), "true", StringComparison.OrdinalIgnoreCase);
        
        if (consoleEnabled)
        {
            builder.AddConsoleExporter();
        }

        // Add OTLP exporter if endpoint is configured
        var otlpEndpoint = Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT");
        if (!string.IsNullOrEmpty(otlpEndpoint))
        {
            builder.AddOtlpExporter(otlpOptions =>
            {
                otlpOptions.Endpoint = new Uri(otlpEndpoint);
                var protocol = Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_PROTOCOL")?.ToLowerInvariant();
                otlpOptions.Protocol = protocol switch
                {
                    "http" or "http/protobuf" => OtlpExportProtocol.HttpProtobuf,
                    "grpc" => OtlpExportProtocol.Grpc,
                    _ => OtlpExportProtocol.Grpc
                };
            });
        }
    }

}





