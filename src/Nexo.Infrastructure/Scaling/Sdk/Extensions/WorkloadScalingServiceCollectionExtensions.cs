using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Nexo.Core.Application.Scaling.Ports;

namespace Nexo.Infrastructure.Scaling.Sdk.Extensions;

/// <summary>DI registration for swappable container workload scaling.</summary>
public static class WorkloadScalingServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="IWorkloadScaler"/> and related ports.
    /// Provider selected by <c>Nexo:WorkloadScaling:Provider</c> or <c>NEXO_WORKLOAD_SCALER</c>
    /// (<c>null</c> | <c>kubernetes</c> | <c>compose</c>).
    /// </summary>
    public static IServiceCollection AddNexoWorkloadScaling(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<WorkloadScalerOptions>(configuration.GetSection(WorkloadScalerOptions.SectionName));
        services.PostConfigure<WorkloadScalerOptions>(opts =>
        {
            var envProvider = Environment.GetEnvironmentVariable("NEXO_WORKLOAD_SCALER")?.Trim();
            if (!string.IsNullOrWhiteSpace(envProvider))
                opts.Provider = envProvider;

            var envEnabled = Environment.GetEnvironmentVariable("NEXO_WORKLOAD_SCALING_ENABLED")?.Trim();
            if (string.Equals(envEnabled, "0", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(envEnabled, "false", StringComparison.OrdinalIgnoreCase))
            {
                opts.Enabled = false;
            }
            else if (string.Equals(envEnabled, "1", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(envEnabled, "true", StringComparison.OrdinalIgnoreCase))
            {
                opts.Enabled = true;
            }

            var envAutoscale = Environment.GetEnvironmentVariable("NEXO_WORKLOAD_AUTOSCALE")?.Trim();
            if (string.Equals(envAutoscale, "1", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(envAutoscale, "true", StringComparison.OrdinalIgnoreCase))
            {
                opts.Autoscale.Enabled = true;
            }
        });

        services.TryAddSingleton<IProcessCommandRunner, ProcessCommandRunner>();
        services.TryAddSingleton<IWorkloadDemandSignal, NullWorkloadDemandSignal>();
        services.TryAddSingleton<IWorkloadScalePolicy, QueueDepthWorkloadScalePolicy>();

        services.AddSingleton<IWorkloadScaler>(sp =>
        {
            var opts = sp.GetRequiredService<IOptions<WorkloadScalerOptions>>().Value;
            var provider = (opts.Provider ?? "null").Trim().ToLowerInvariant();
            return provider switch
            {
                "kubernetes" or "k8s" => ActivatorUtilities.CreateInstance<KubernetesWorkloadScaler>(sp),
                "compose" or "docker-compose" => ActivatorUtilities.CreateInstance<ComposeWorkloadScaler>(sp),
                _ => ActivatorUtilities.CreateInstance<NullWorkloadScaler>(sp),
            };
        });

        // Resolve options after PostConfigure by building a temporary monitor pattern:
        // register hosted service only when Autoscale.Enabled is true at startup.
        var autoscaleEnabled =
            configuration.GetValue($"{WorkloadScalerOptions.SectionName}:Autoscale:Enabled", false) ||
            string.Equals(Environment.GetEnvironmentVariable("NEXO_WORKLOAD_AUTOSCALE"), "1", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(Environment.GetEnvironmentVariable("NEXO_WORKLOAD_AUTOSCALE"), "true", StringComparison.OrdinalIgnoreCase);

        if (autoscaleEnabled)
            services.AddHostedService<ElasticWorkloadAutoscaleService>();

        return services;
    }
}
