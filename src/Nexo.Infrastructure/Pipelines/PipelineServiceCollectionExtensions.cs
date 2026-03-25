using Microsoft.Extensions.Configuration;
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
        if (services == null) throw new ArgumentNullException(nameof(services));

        services.AddOptions<PipelineExecutionOptions>();
        services.Configure<PipelineExecutionOptions>(opts =>
        {
            if (int.TryParse(Environment.GetEnvironmentVariable("NEXO_PIPELINE_MAX_RETRIES"), out var retries) && retries >= 1)
                opts.MaxRetryAttempts = retries;
            if (int.TryParse(Environment.GetEnvironmentVariable("NEXO_PIPELINE_RETRY_DELAY_MS"), out var retryDelayMs) && retryDelayMs >= 0)
                opts.RetryDelayMs = retryDelayMs;
            if (TryParseBoolean(Environment.GetEnvironmentVariable("NEXO_PIPELINE_RESUME_FAILED"), out var resumeFailed))
                opts.ResumeFailedStages = resumeFailed;
            if (TryParseBoolean(Environment.GetEnvironmentVariable("NEXO_PIPELINE_ALLOW_MISSING_RESUME_SOURCE"), out var allowMissingResume))
                opts.AllowMissingResumeSource = allowMissingResume;
            if (TryParseBoolean(Environment.GetEnvironmentVariable("NEXO_PIPELINE_ENABLE_TEST_HOOKS"), out var enableTestHooks))
                opts.EnableTestHooks = enableTestHooks;
            var completionPolicy = Environment.GetEnvironmentVariable("NEXO_PIPELINE_COMPLETION_POLICY");
            if (TryParseCompletionPolicy(completionPolicy, out var parsedPolicy))
                opts.CompletionPolicy = parsedPolicy;
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
        services.AddOptions<PipelineExecutionAdapterOptions>();
        services.Configure<PipelineExecutionAdapterOptions>(opts =>
        {
            var deterministic = Environment.GetEnvironmentVariable("NEXO_PIPELINE_DETERMINISTIC_ADAPTER");
            if (!string.IsNullOrWhiteSpace(deterministic))
                opts.DeterministicAdapter = deterministic;
            var agentic = Environment.GetEnvironmentVariable("NEXO_PIPELINE_AGENTIC_ADAPTER");
            if (!string.IsNullOrWhiteSpace(agentic))
                opts.AgenticAdapter = agentic;
        });

        services.TryAddSingleton<IPipelineTemplateValidator, PipelineTemplateValidator>();
        services.TryAddSingleton<IPipelineDecomposer, PipelineDecomposer>();
        services.TryAddSingleton<IPipelineScheduler, PipelineScheduler>();
        services.TryAddSingleton<IPipelineScalingPolicy, ThresholdScalingPolicy>();
        services.TryAddSingleton<IPipelineRunStore>(sp =>
        {
            var options = sp.GetService<IOptions<PipelinePersistenceOptions>>()?.Value ?? new PipelinePersistenceOptions();
            if (!PipelinePersistenceOptions.IsKnownProvider(options.Provider))
            {
                throw new InvalidOperationException(
                    $"Unknown pipeline persistence provider '{options.Provider}'. Supported values: InMemory, LiteDb.");
            }
            if (string.Equals(options.Provider, "LiteDb", StringComparison.OrdinalIgnoreCase))
                return new LiteDbPipelineRunStore(options.DatabasePath);
            return new InMemoryPipelineRunStore();
        });
        services.AddSingleton<IPipelineStageExecutionAdapter, DefaultDeterministicStageExecutionAdapter>();
        services.AddSingleton<IPipelineStageExecutionAdapter, DefaultAgenticStageExecutionAdapter>();
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

    /// <summary>
    /// Adds pipeline composition layer with configuration binding.
    /// Configuration values are applied before environment variable overrides.
    /// </summary>
    public static IServiceCollection AddPipelineCompositionLayer(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        if (services == null) throw new ArgumentNullException(nameof(services));
        if (configuration == null) throw new ArgumentNullException(nameof(configuration));

        services.AddOptions<PipelineExecutionOptions>()
            .Configure(options => configuration.GetSection(PipelineExecutionOptions.SectionName).Bind(options));
        services.AddOptions<PipelinePersistenceOptions>()
            .Configure(options => configuration.GetSection(PipelinePersistenceOptions.SectionName).Bind(options));
        services.AddOptions<PipelineExecutionAdapterOptions>()
            .Configure(options => configuration.GetSection(PipelineExecutionAdapterOptions.SectionName).Bind(options));

        return services.AddPipelineCompositionLayer();
    }

    private static bool TryParseCompletionPolicy(string? value, out PipelineCompletionPolicy policy)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            policy = default;
            return false;
        }

        return Enum.TryParse(value, ignoreCase: true, out policy);
    }

    private static bool TryParseBoolean(string? value, out bool parsed)
    {
        if (bool.TryParse(value, out parsed))
            return true;

        if (string.Equals(value, "1", StringComparison.Ordinal))
        {
            parsed = true;
            return true;
        }

        if (string.Equals(value, "0", StringComparison.Ordinal))
        {
            parsed = false;
            return true;
        }

        parsed = default;
        return false;
    }
}
