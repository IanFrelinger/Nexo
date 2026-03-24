using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Nexo.Core.Application.Pipelines.Ports;

namespace Nexo.Infrastructure.Pipelines;

/// <summary>
/// Registers pipeline composition layer services.
/// </summary>
public static class PipelineServiceCollectionExtensions
{
    /// <summary>
    /// Adds default pipeline composition services.
    /// </summary>
    public static IServiceCollection AddPipelineCompositionLayer(this IServiceCollection services)
    {
        services.AddOptions<PipelineExecutionOptions>();
        services.Configure<PipelineExecutionOptions>(opts =>
        {
            if (int.TryParse(Environment.GetEnvironmentVariable("NEXO_PIPELINE_MAX_RETRIES"), out var retries) && retries >= 1)
                opts.MaxRetryAttempts = retries;
            if (int.TryParse(Environment.GetEnvironmentVariable("NEXO_PIPELINE_RETRY_DELAY_MS"), out var retryDelayMs) && retryDelayMs >= 0)
                opts.RetryDelayMs = retryDelayMs;
            if (bool.TryParse(Environment.GetEnvironmentVariable("NEXO_PIPELINE_RESUME_FAILED"), out var resumeFailed))
                opts.ResumeFailedStages = resumeFailed;
        });

        services.AddOptions<PipelinePersistenceOptions>();
        services.Configure<PipelinePersistenceOptions>(opts =>
        {
            var provider = Environment.GetEnvironmentVariable("NEXO_PIPELINE_STORE_PROVIDER");
            if (!string.IsNullOrWhiteSpace(provider))
                opts.Provider = provider;
            var dbPath = Environment.GetEnvironmentVariable("NEXO_PIPELINE_STORE_PATH");
            if (!string.IsNullOrWhiteSpace(dbPath))
                opts.DatabasePath = dbPath;
        });

        services.TryAddSingleton<IPipelineTemplateValidator, PipelineTemplateValidator>();
        services.TryAddSingleton<IPipelineDecomposer, PipelineDecomposer>();
        services.TryAddSingleton<IPipelineScheduler, PipelineScheduler>();
        services.TryAddSingleton<IPipelineScalingPolicy, ThresholdScalingPolicy>();
        services.TryAddSingleton<IPipelineRunStore>(sp =>
        {
            var options = sp.GetService<IOptions<PipelinePersistenceOptions>>()?.Value ?? new PipelinePersistenceOptions();
            if (string.Equals(options.Provider, "LiteDb", StringComparison.OrdinalIgnoreCase))
                return new LiteDbPipelineRunStore(options.DatabasePath);
            return new InMemoryPipelineRunStore();
        });
        services.AddSingleton<IPipelineStageExecutor, DeterministicPipelineStageExecutor>();
        services.AddSingleton<IPipelineStageExecutor, AgenticPipelineStageExecutor>();
        services.TryAddSingleton<IPipelineOrchestrator, PipelineOrchestrator>();
        return services;
    }

    /// <summary>
    /// Adds pipeline composition layer with explicit options configuration.
    /// </summary>
    public static IServiceCollection AddPipelineCompositionLayer(
        this IServiceCollection services,
        Action<PipelineExecutionOptions>? configureExecution,
        Action<PipelinePersistenceOptions>? configurePersistence)
    {
        services.AddPipelineCompositionLayer();

        if (configureExecution != null)
            services.Configure(configureExecution);

        if (configurePersistence != null)
            services.Configure(configurePersistence);

        return services;
    }
}
