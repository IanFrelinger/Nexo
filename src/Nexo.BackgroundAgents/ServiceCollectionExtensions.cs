using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Nexo.BackgroundAgents.Configuration;
using Nexo.BackgroundAgents.DataSensitivity;
using Nexo.BackgroundAgents.Logging;
using Nexo.BackgroundAgents.Extending;
using Nexo.BackgroundAgents.Observations;
using Nexo.BackgroundAgents.Optimization;
using Nexo.BackgroundAgents.Registry;
using Nexo.BackgroundAgents.Testing;
using Nexo.BackgroundAgents.RAG;
using Nexo.BackgroundAgents.Scheduling;
using Nexo.BackgroundAgents.Services;
using Nexo.BackgroundAgents.Telemetry;
using Nexo.BackgroundAgents.Tools;
using Nexo.BackgroundAgents.Trust;
using Nexo.BackgroundAgents.WebSearch;
using Nexo.BackgroundAgents.Observation;
using Nexo.Core.Application.Trust.Ports;
using Nexo.Infrastructure.Observation;
using Nexo.Infrastructure.Trust;
using Nexo.Orchestration.Agents;

namespace Nexo.BackgroundAgents;

/// <summary>
/// Extension methods for registering background agent services.
/// </summary>
public static class ServiceCollectionExtensions
{
    private const string ObservationDegradedModeEnv = "NEXO_OBSERVATION_DEGRADED_MODE";

    /// <summary>
    /// Adds background agent services including scheduler, registry, and policy integration.
    /// Requires orchestration (AgentFactory, LifecycleManager for BackgroundAgentService) and configuration to be registered.
    /// </summary>
    /// <param name="registerHostedService">If true, registers BackgroundAgentService as a hosted service (default true). Set false for CLI-only usage.</param>
    public static IServiceCollection AddBackgroundAgents(this IServiceCollection services, bool registerHostedService = true)
    {
        services.TryAddSingleton<IDataSensitivityRegistry, DataSensitivityRegistry>();
        services.TryAddSingleton<IAggressivenessModeStore>(sp =>
        {
            var path = Environment.GetEnvironmentVariable("NEXO_AGENT_MODE_PATH");
            return string.IsNullOrWhiteSpace(path)
                ? new FileBasedAggressivenessModeStore()
                : new FileBasedAggressivenessModeStore(path.Trim());
        });
        services.TryAddSingleton<IBackgroundAgentLogStore, InMemoryAgentLogStore>();
        services.TryAddSingleton<IScheduleExecutor, ScheduleExecutor>();
        services.AddSingleton<IAgentScheduler, AgentScheduler>();
        services.TryAddSingleton<IApprovalGate, NoApprovalGate>();
        services.TryAddSingleton<CycleEventStore>(sp =>
        {
            // Default to <repoRoot>/.nexo/runtime-studio/cycles.jsonl. Operators can override
            // by setting NEXO_CYCLE_EVENTS_PATH to an absolute or repo-relative path.
            var overridePath = Environment.GetEnvironmentVariable("NEXO_CYCLE_EVENTS_PATH");
            return string.IsNullOrWhiteSpace(overridePath)
                ? new CycleEventStore()
                : new CycleEventStore(overridePath.Trim());
        });
        services.TryAddSingleton<IObservationStore>(sp =>
        {
            // Companion to cycles.jsonl: structured facts agents publish for each other to read.
            // Override location with NEXO_OBSERVATIONS_PATH for tests / sandboxed runs.
            var overridePath = Environment.GetEnvironmentVariable("NEXO_OBSERVATIONS_PATH");
            return string.IsNullOrWhiteSpace(overridePath)
                ? new JsonlObservationStore()
                : new JsonlObservationStore(overridePath.Trim());
        });
        services.AddSingleton<IBackgroundAgentRegistry>(sp =>
        {
            var scheduler = sp.GetRequiredService<IAgentScheduler>();
            var logger = sp.GetService<Microsoft.Extensions.Logging.ILogger<BackgroundAgentRegistry>>();
            var logStore = sp.GetService<IBackgroundAgentLogStore>();
            var codeAnalysisRunner = sp.GetService<ICodeAnalysisRunner>();
            var testRunRunner = sp.GetService<ITestRunRunner>();
            var selfExtendRunner = sp.GetService<ISelfExtendRunner>();
            var selfImprovementLoop = sp.GetService<Nexo.Core.Application.SelfImprovement.Ports.ISelfImprovementLoop>();
            var modeStore = sp.GetService<IAggressivenessModeStore>();
            var approvalGate = sp.GetService<IApprovalGate>();
            var auditLog = sp.GetService<IDataDecisionAuditLog>();
            var cycleEvents = sp.GetService<CycleEventStore>();
            var observations = sp.GetService<IObservationStore>();
            return new BackgroundAgentRegistry(scheduler, logger, logStore, codeAnalysisRunner, testRunRunner, selfExtendRunner, selfImprovementLoop, modeStore, approvalGate, auditLog, cycleEvents, observations);
        });
        services.TryAddSingleton<BackgroundAgentConfigLoader>();
        services.TryAddSingleton<BackgroundAgentSpecBuilder>();
        services.TryAddSingleton<AgentManagementToolbox>(sp =>
        {
            var registry = sp.GetRequiredService<IBackgroundAgentRegistry>();
            var configLoader = sp.GetRequiredService<BackgroundAgentConfigLoader>();
            var specBuilder = sp.GetRequiredService<BackgroundAgentSpecBuilder>();
            var agentFactory = sp.GetRequiredService<AgentFactory>();
            return new AgentManagementToolbox(registry, configLoader, specBuilder, agentFactory);
        });
        if (registerHostedService)
            services.AddHostedService<BackgroundAgentService>();
        return services;
    }

    /// <summary>
    /// Adds RAG (Retrieval Augmented Generation) services for CLI and agent use.
    /// Registers in-memory vector store and token-based embeddings by default.
    /// </summary>
    public static IServiceCollection AddBackgroundAgentsRAG(this IServiceCollection services)
    {
        services.TryAddSingleton<IEmbeddingGenerator, TokenEmbeddingGenerator>();
        services.TryAddSingleton<IVectorStore>(sp =>
        {
            var registry = sp.GetService<IDataSensitivityRegistry>();
            return new InMemoryVectorStore(registry);
        });
        services.TryAddSingleton<IRAGService, RAGService>();
        services.TryAddSingleton<IKnowledgeBaseIndexer, KnowledgeBaseIndexer>();
        return services;
    }

    /// <summary>
    /// Adds Trust &amp; Information Architecture services: data taxonomy, cloud sanitization proxy, audit log.
    /// Call before registering IProviderFactory. When useSanitizingProviderFactory is true, IProviderFactory
    /// will be a SanitizingProviderFactory that wraps the inner ProviderFactory.
    /// </summary>
    /// <param name="useSanitizingProviderFactory">If true, IProviderFactory is registered as SanitizingProviderFactory wrapping ProviderFactory.</param>
    /// <param name="ephemeralLifecycle">If true, ProviderFactory will receive IEphemeralModelLifecycle for ephemeral Ollama (caller must register it).</param>
    /// <param name="skipProviderRegistration">If true, trust services are added but provider factory is not registered (caller builds chain).</param>
    public static IServiceCollection AddTrustServices(this IServiceCollection services, bool useSanitizingProviderFactory = false, bool ephemeralLifecycle = false, bool skipProviderRegistration = false)
    {
        services.TryAddSingleton<IDataTaxonomy, DataTaxonomy>();
        services.TryAddSingleton<ISensitiveContentFilter, SensitiveContentFilter>();
        services.AddUserKnowledgeLog(Environment.GetEnvironmentVariable("NEXO_KNOWLEDGE_LOG_PATH"));
        services.AddAccessBoundary(Environment.GetEnvironmentVariable("NEXO_ACCESS_BOUNDARY_CONFIG"));
        services.AddCloudAvailabilityResolver(
            configPath: Environment.GetEnvironmentVariable("NEXO_CONFIG_PATH"),
            enableNetworkProbe: string.Equals(Environment.GetEnvironmentVariable("NEXO_AIRGAP_PROBE"), "1", StringComparison.Ordinal));
        var auditDbPath = Environment.GetEnvironmentVariable("NEXO_TRUST_AUDIT_DB");
        object auditLogInstance = !string.IsNullOrWhiteSpace(auditDbPath)
            ? new Nexo.BackgroundAgents.Trust.LiteDbDataDecisionAuditLog(auditDbPath)
            : new Nexo.BackgroundAgents.Trust.DataDecisionAuditLog();
        services.TryAddSingleton<IDataDecisionAuditLog>(sp => (IDataDecisionAuditLog)auditLogInstance);
        services.TryAddSingleton<ISanitizationAuditLog>(sp => (ISanitizationAuditLog)auditLogInstance);
        services.TryAddSingleton<ICloudSanitizationProxy>(sp =>
        {
            var filter = sp.GetService<ISensitiveContentFilter>();
            var taxonomy = sp.GetService<IDataTaxonomy>();
            var audit = sp.GetService<ISanitizationAuditLog>();
            return new CloudSanitizationProxy(filter, taxonomy, audit);
        });
        services.TryAddSingleton<ITrustPolicyPackRegistry, TrustPolicyPackRegistry>();

        if (useSanitizingProviderFactory && !skipProviderRegistration)
        {
            services.AddSingleton<Nexo.Infrastructure.Execution.ProviderFactory>(sp =>
            {
                var logger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<Nexo.Infrastructure.Execution.ProviderFactory>>();
                var lifecycle = ephemeralLifecycle ? sp.GetService<Nexo.Core.Application.Ephemeral.Ports.IEphemeralModelLifecycle>() : null;
                return new Nexo.Infrastructure.Execution.ProviderFactory(logger, lifecycle);
            });
            services.AddSingleton<Nexo.Infrastructure.Execution.IProviderFactory>(sp =>
            {
                var inner = sp.GetRequiredService<Nexo.Infrastructure.Execution.ProviderFactory>();
                var proxy = sp.GetRequiredService<ICloudSanitizationProxy>();
                var logger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<SanitizingProviderFactory>>();
                return new SanitizingProviderFactory(inner, proxy, logger);
            });
        }

        return services;
    }

    /// <summary>
    /// Adds the observation pipeline: event sources, pattern detection, pattern store.
    /// Registers ObservationPipelineService as a hosted service when registerHostedService is true.
    /// </summary>
    /// <param name="configure">Optional configuration of pipeline options.</param>
    /// <param name="registerHostedService">If true, registers ObservationPipelineService (default true).</param>
    public static IServiceCollection AddObservationPipeline(this IServiceCollection services, Action<ObservationPipelineOptions>? configure = null, bool registerHostedService = true)
    {
        var degradedModeEnabled = string.Equals(
            Environment.GetEnvironmentVariable(ObservationDegradedModeEnv),
            "1",
            StringComparison.OrdinalIgnoreCase);
        services.Configure<ObservationPipelineOptions>(opts =>
        {
            configure?.Invoke(opts);
        });
        services.AddSingleton<Nexo.Core.Application.Observation.Ports.IPatternStore>(sp =>
        {
            var opts = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<ObservationPipelineOptions>>().Value;
            var repoRoot = opts.RepoRoot ?? Directory.GetCurrentDirectory();
            var storePath = Path.Combine(repoRoot, opts.StorePath);
            if (!degradedModeEnabled)
            {
                return new LiteDbPatternStore(storePath);
            }

            try
            {
                return new LiteDbPatternStore(storePath);
            }
            catch
            {
                return new NoOpPatternStore();
            }
        });
        services.AddSingleton<Nexo.Core.Application.Observation.Ports.IPatternProcessedStore>(sp =>
        {
            var opts = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<ObservationPipelineOptions>>().Value;
            var repoRoot = opts.RepoRoot ?? Directory.GetCurrentDirectory();
            var storePath = Path.Combine(repoRoot, opts.StorePath);
            return new Nexo.Infrastructure.Observation.LiteDbPatternProcessedStore(storePath);
        });
        services.AddSingleton<Nexo.Core.Application.Observation.Ports.IContextAssembler, ContextAssembler>();
        if (registerHostedService)
            services.AddHostedService<ObservationPipelineService>();
        return services;
    }
}
