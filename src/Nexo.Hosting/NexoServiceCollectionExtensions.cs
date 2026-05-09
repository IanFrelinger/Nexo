using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Nexo.BackgroundAgents;
using Nexo.BackgroundAgents.Trust;
using Nexo.Core.Application.Adaptation.Ports;
using Nexo.Core.Application.Analysis.UseCases.AnalyzeCode;
using Nexo.Core.Application.Ephemeral.Ports;
using Nexo.Core.Application.Knowledge.Ports;
using Nexo.Core.Application.Observation.Ports;
using Nexo.Core.Application.Validation.UseCases.RunValidation;
using Nexo.Core.Application.Testing.UseCases.RunTests;
using Nexo.Core.Application.Common.Ports;
using Nexo.Core.Application.Common.Services;
using Nexo.Core.Application.Copilot.Ports;
using Nexo.Core.Application.Paths;
using Nexo.Core.Application.Trust.Ports;
using Nexo.Infrastructure;
using Nexo.Infrastructure.Execution;
using Nexo.Infrastructure.Execution.Routing;
using Nexo.Infrastructure.Execution.Ephemeral;
using Nexo.Infrastructure.Execution.LoadPolicy;
using Nexo.Infrastructure.Knowledge;
using Nexo.Infrastructure.Maintenance;
using Nexo.Infrastructure.NodeCapabilityRuntime;
using Nexo.Infrastructure.ModelArtifacts;
using Nexo.Infrastructure.Pipelines;
using Nexo.Infrastructure.Persistence.Ephemeral;
using Nexo.Infrastructure.Persistence;
using Nexo.Infrastructure.Copilot;
using Nexo.Orchestration;
using Nexo.Orchestration.Models;
using Nexo.Abstractions.Routing;
using Nexo.Abstractions.Transport;
using Nexo.Orchestration.Transport;
using Nexo.Runtime;
using Nexo.Runtime.Routing;
using Nexo.Transport.Grpc;

namespace Nexo.Hosting;

/// <summary>
/// DI composition root for the Nexo kernel.  This is the single place that wires every
/// subsystem together — orchestration, adaptation, persistence, trust, execution, etc.
/// <para>
/// <b>Architecture:</b> The method <see cref="AddNexo"/> follows a strict registration
/// order because later registrations depend on services registered earlier (e.g. the
/// model decorator chain wraps <c>ProviderBackedModel → HotSwappableModel →
/// OrchestrationRuntimeModelDecorator</c>, so the provider factory must already exist).
/// </para>
/// <para>
/// <b>Deployment profiles:</b> A <see cref="NexoDeploymentProfile"/> (resolved from
/// <c>NEXO_DEPLOYMENT_PROFILE</c> or <see cref="NexoHostingOptions.DeploymentProfile"/>)
/// controls which subsystem modules are included via <see cref="ModuleSelection"/>.
/// Profiles range from <c>Full</c> (all modules) down to <c>System</c> (bare minimum
/// for CLI/headless tooling).
/// </para>
/// <para>
/// <b>Related files:</b>
/// <see cref="NexoHostingOptions"/> — caller-facing option bag;
/// <see cref="NexoDeploymentProfile"/> — deployment tier enum;
/// <c>Nexo.Core.Domain.NexoDefaults</c> — all tuneable default constants.
/// </para>
/// </summary>
public static class NexoServiceCollectionExtensions
{
    /// <summary>
    /// Flags produced by <see cref="GetModuleSelection"/> that decide which subsystem
    /// modules are registered.  Each flag maps 1-to-1 to a conditional block inside
    /// <see cref="AddNexo"/>.  The mapping is intentionally explicit (no reflection)
    /// so that trimming and ahead-of-time compilation remain safe.
    /// </summary>
    private sealed record ModuleSelection(
        bool IncludeNodeCapabilityRuntime,
        bool IncludeRuntimeTransport,
        bool IncludePersistence,
        bool IncludeAdaptation,
        bool IncludePipelineComposition,
        bool IncludeBackgroundAgents,
        bool IncludeBackgroundAgentRag,
        bool IncludeObservationPipeline,
        bool IncludeTrustServices,
        bool IncludeWorkflowIntegrations,
        bool IncludeTestingAdapters);

    /// <summary>
    /// Adds Nexo with an explicit deployment profile.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="profile">Dependency profile to apply.</param>
    /// <param name="configure">Optional additional options overrides.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddNexoProfile(
        this IServiceCollection services,
        NexoDeploymentProfile profile,
        Action<NexoHostingOptions>? configure = null)
    {
        return services.AddNexo(options =>
        {
            options.DeploymentProfile = profile;
            configure?.Invoke(options);
        });
    }

    /// <summary>
    /// Registers every Nexo subsystem into the DI container.  The registration order
    /// matters: downstream registrations (model decorator chain, workflow executor)
    /// resolve services registered in earlier blocks.
    /// <para>
    /// <b>Environment variables read here (see inline comments for each):</b>
    /// <c>NEXO_STRICT_MODE</c>, <c>NEXO_DEPLOYMENT_PROFILE</c>,
    /// <c>NEXO_LOOP_PARALLEL</c>, <c>NEXO_LOOP_INSTRUMENT</c>,
    /// <c>NEXO_OBSERVATION_FAIL_OPEN</c>, <c>NEXO_EPHEMERAL</c>,
    /// <c>NEXO_EPHEMERAL_MODELS</c>, <c>NEXO_EPHEMERAL_DB</c>,
    /// <c>NEXO_TRUST_ENABLED</c>, <c>NEXO_LOAD_PREFERENCE</c>,
    /// <c>NEXO_EXECUTION_REMOTE_URL</c>.
    /// </para>
    /// </summary>
    public static IServiceCollection AddNexo(
        this IServiceCollection services,
        Action<NexoHostingOptions>? configure = null)
    {
        // ── Strict mode & deployment profile ───────────────────────────
        // Strict mode is resolved first because the configuration service
        // adapter (registered below) reads it to decide whether config
        // warnings should throw.  Deployment profile gates every
        // conditional module block that follows.
        var options = new NexoHostingOptions();
        configure?.Invoke(options);
        ResolveStrictMode(options);
        var deploymentProfile = ResolveDeploymentProfile(options);
        var modules = GetModuleSelection(deploymentProfile);

        services.AddSingleton(options.StrictMode);

        // ── Configuration & Node Capability Runtime ────────────────────
        // Environment variables are the primary config source; appsettings
        // is intentionally NOT loaded here so that containerised deployments
        // stay 12-factor compliant.  RemoteCapabilitiesOptions binds from
        // the "Nexo:RemoteCapabilities" section for RunPod/cloud routing.
        services.AddHttpClient();
        var configuration = new ConfigurationBuilder()
            .AddEnvironmentVariables()
            .Build();
        services.AddOptions<Nexo.Infrastructure.Execution.RemoteCapabilitiesOptions>()
            .Bind(configuration.GetSection("Nexo:RemoteCapabilities"));
        if (modules.IncludeNodeCapabilityRuntime)
        {
            services.AddRunPodCapabilityRouting(configuration);
            RegisterNodeCapabilityRuntime(services, configuration);
        }

        // ── CQRS (MediatR) & FluentValidation ─────────────────────────
        // MediatR handlers from both the Analysis and Testing assemblies
        // are registered in one pass.  The ValidationBehavior pipeline
        // behavior runs FluentValidation before each handler, so
        // validators must also be registered here.
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(AnalyzeCodeCommand).Assembly);
            cfg.RegisterServicesFromAssembly(typeof(RunTestsCommand).Assembly);
        });

        services.AddValidatorsFromAssembly(typeof(AnalyzeCodeValidator).Assembly);
        services.AddTransient(typeof(MediatR.IPipelineBehavior<,>), typeof(Nexo.Core.Application.Behaviors.ValidationBehavior<,>));

        // ── Configuration service adapter ──────────────────────────────
        // Bridges the domain-level IConfigurationService port to the
        // infrastructure adapter.  Strict mode controls whether config
        // warnings escalate to hard failures (useful in CI pipelines).
        services.AddSingleton<Nexo.Core.Application.Configuration.Ports.IConfigurationService>(sp =>
        {
            var logger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<Nexo.Infrastructure.Configuration.ConfigurationServiceAdapter>>();
            var strictMode = sp.GetService<StrictModeOptions>();
            return new Nexo.Infrastructure.Configuration.ConfigurationServiceAdapter(logger, strictMode?.ShouldFailOnConfigurationWarnings ?? false);
        });

        // ── Loop kernel (decorator chain) ──────────────────────────────
        // The loop kernel runs brick-level iterations.  It is composed via
        // the decorator pattern:
        //   SequentialLoopKernel  (always present — baseline)
        //     → ParallelLoopKernel      (if NEXO_LOOP_PARALLEL=1)
        //       → InstrumentedLoopKernel (if NEXO_LOOP_INSTRUMENT=1)
        //
        // NEXO_LOOP_PARALLEL ("1"): wraps in a parallelising decorator for
        //   concurrent brick evaluation; useful on multi-core servers.
        // NEXO_LOOP_INSTRUMENT ("1"): adds timing/counter telemetry around
        //   each loop iteration; adds overhead, meant for dev profiling.
        services.AddSingleton<ILoopKernel>(sp =>
        {
            ILoopKernel k = new SequentialLoopKernel();
            var enableParallel = string.Equals(Environment.GetEnvironmentVariable("NEXO_LOOP_PARALLEL"), "1", StringComparison.OrdinalIgnoreCase);
            if (enableParallel)
                k = new ParallelLoopKernel(k);
            var instrument = string.Equals(Environment.GetEnvironmentVariable("NEXO_LOOP_INSTRUMENT"), "1", StringComparison.OrdinalIgnoreCase);
            if (instrument)
                k = new InstrumentedLoopKernel(k, sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<InstrumentedLoopKernel>>());
            return k;
        });

        // ── Orchestration & transport ──────────────────────────────────
        // Orchestration is always registered (it owns the runtime spec
        // accessor used by the model decorator chain).  Transport is
        // optional: when present it registers gRPC channels plus the
        // dual in-process / gRPC agent transport pair used for peer
        // communication.  See Nexo.Transport.Grpc for channel config.
        services.AddNexoOrchestration();
        if (modules.IncludeRuntimeTransport)
        {
            services.AddOptions<GrpcTransportOptions>();
            services.AddOptions<RoutingOptions>();
            services.TryAddSingleton<IEndpointRegistry, InMemoryEndpointRegistry>();
            services.TryAddSingleton<IGrpcChannelFactory, DefaultGrpcChannelFactory>();
            services.AddNexoRuntimeTransport<InProcessAgentTransport, GrpcAgentTransport>();
        }

        // ── Persistence ────────────────────────────────────────────────
        if (modules.IncludePersistence)
        {
            services.AddNexoPersistence();
            services.AddPostgresIsolatedDatabaseProvisioner();
        }

        // ── Adaptation ─────────────────────────────────────────────────
        // Pattern store path is forwarded so the adaptation layer knows
        // where to persist learned patterns on disk.
        if (modules.IncludeAdaptation)
            services.AddAdaptationInfrastructure(options.PatternStorePath);

        if (modules.IncludeAdaptation)
            services.AddNexoFederatedBrickMesh(configuration);

        // ── Copilot task store ──────────────────────────────────────────
        // LiteDB file is co-located with the pattern store directory
        // (or the repo root as fallback) to keep all Nexo-generated
        // state in one discoverable location.
        var copilotTasksBasePath = !string.IsNullOrEmpty(options.PatternStorePath)
            ? Path.GetDirectoryName(options.PatternStorePath) ?? "."
            : RepoPathResolver.FindRepoRoot();
        var copilotTasksDbPath = Path.Combine(copilotTasksBasePath, "nexo-copilot-tasks.db");
        services.TryAddSingleton<ICopilotTaskStore>(_ => new LiteDbCopilotTaskStore(copilotTasksDbPath));

        // ── Knowledge query service ────────────────────────────────────
        // Aggregates adaptation logs, pattern store, and (optionally)
        // user-knowledge logs into a single query façade.  Falls back to
        // an in-memory knowledge log when the trust module is absent.
        services.TryAddSingleton<IKnowledgeQueryService>(sp =>
        {
            var adaptationLog = sp.GetRequiredService<IAdaptationLog>();
            var patternStore = sp.GetRequiredService<IPatternStore>();
            var userKnowledgeStore = sp.GetService<IUserKnowledgeLogStore>()
                ?? new Nexo.Infrastructure.Trust.InMemoryUserKnowledgeLogStore();
            return new KnowledgeQueryService(adaptationLog, patternStore, userKnowledgeStore);
        });

        // ── Pipeline composition ───────────────────────────────────────
        if (modules.IncludePipelineComposition)
            services.AddPipelineCompositionLayer();

        // ── Background agents & RAG ────────────────────────────────────
        if (modules.IncludeBackgroundAgents)
            services.AddBackgroundAgents(registerHostedService: options.RegisterBackgroundAgentHostedService);

        if (modules.IncludeBackgroundAgentRag)
            services.AddBackgroundAgentsRAG();

        // ── Observation pipeline ───────────────────────────────────────
        // Captures runtime telemetry and persists it alongside patterns.
        // NEXO_OBSERVATION_FAIL_OPEN ("1" / "true"): when set, store I/O
        //   errors are swallowed instead of failing the pipeline — safe
        //   for edge nodes with unreliable storage.
        if (modules.IncludeObservationPipeline && !options.DisableObservationPipeline)
        {
            var repoRoot = RepoPathResolver.FindRepoRoot();
            var observationFailOpen = options.ObservationFailOpen ?? ParseBooleanEnvironmentVariable("NEXO_OBSERVATION_FAIL_OPEN");
            services.AddObservationPipeline(opts =>
            {
                opts.RepoRoot = repoRoot;
                opts.StorePath = options.PatternStorePath ?? "nexo-patterns.db";
                opts.FailOpenOnStoreErrors = observationFailOpen;
            }, registerHostedService: options.RegisterBackgroundAgentHostedService);
        }

        // Mock web-search provider is registered as a fallback so
        // background agents can be instantiated even when no real
        // provider is configured.
        if (modules.IncludeBackgroundAgents)
            services.TryAddSingleton<Nexo.BackgroundAgents.WebSearch.IWebSearchProvider, Nexo.BackgroundAgents.WebSearch.MockWebSearchProvider>();

        // ── Model decorator chain ──────────────────────────────────────
        // The IModel abstraction is built as a three-layer decorator:
        //
        //   1. ProviderBackedModel     – delegates to IProviderFactory
        //   2. HotSwappableModel       – allows runtime model switching
        //                                without restarting the host
        //   3. OrchestrationRuntimeModelDecorator
        //                              – injects orchestration-level
        //                                spec overrides (temperature,
        //                                token limits, etc.) per-call
        //
        // HotSwappableModel is registered as a concrete singleton so that
        // administrative endpoints can resolve it directly for hot-swap
        // operations, while IModel always returns the fully decorated chain.
        services.AddSingleton<Nexo.Infrastructure.Execution.Models.HotSwappableModel>(sp =>
        {
            var providerFactory = sp.GetRequiredService<Nexo.Infrastructure.Execution.IProviderFactory>();
            var providerBacked = new Nexo.Infrastructure.Execution.Models.ProviderBackedModel(
                providerFactory,
                sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<Nexo.Infrastructure.Execution.Models.ProviderBackedModel>>());
            return new Nexo.Infrastructure.Execution.Models.HotSwappableModel(
                providerBacked,
                sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<Nexo.Infrastructure.Execution.Models.HotSwappableModel>>());
        });

        services.AddSingleton<Nexo.Abstractions.IModel>(sp =>
        {
            var accessor = sp.GetRequiredService<IOrchestrationRuntimeSpecAccessor>();
            var inner = sp.GetRequiredService<Nexo.Infrastructure.Execution.Models.HotSwappableModel>();
            return new Nexo.Orchestration.Models.OrchestrationRuntimeModelDecorator(
                inner,
                accessor,
                sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<Nexo.Orchestration.Models.OrchestrationRuntimeModelDecorator>>());
        });

        // ── Ephemeral lifecycle ────────────────────────────────────────
        // "Ephemeral" means Nexo can spin up and tear down backing
        // resources on demand (Ollama models, Postgres databases).
        //
        // NEXO_EPHEMERAL ("1"): master switch — enables ALL ephemeral
        //   subsystems.
        // NEXO_EPHEMERAL_MODELS ("1"): enables only ephemeral model
        //   lifecycle (Ollama pull/remove) without affecting databases.
        // NEXO_EPHEMERAL_DB ("postgres"): enables ephemeral Postgres
        //   database creation; only takes effect when persistence is on.
        var ephemeralAll = string.Equals(Environment.GetEnvironmentVariable("NEXO_EPHEMERAL"), "1", StringComparison.OrdinalIgnoreCase);
        var ephemeralModels = ephemeralAll || string.Equals(Environment.GetEnvironmentVariable("NEXO_EPHEMERAL_MODELS"), "1", StringComparison.OrdinalIgnoreCase);
        if (ephemeralModels)
            services.AddSingleton<IEphemeralModelLifecycle, OllamaEphemeralLifecycle>();

        var ephemeralDb = Environment.GetEnvironmentVariable("NEXO_EPHEMERAL_DB")?.Trim();
        if (modules.IncludePersistence && string.Equals(ephemeralDb, "postgres", StringComparison.OrdinalIgnoreCase))
            services.AddSingleton<Nexo.Core.Application.Persistence.Ports.IEphemeralDatabaseLifecycle, PostgresEphemeralLifecycle>();

        // ── Trust & provider factory (3-way branching) ─────────────────
        // The provider factory is the gateway through which every LLM
        // call flows.  Three mutually-exclusive wiring paths exist:
        //
        //   Path A — Adaptive load-balancing (NEXO_LOAD_PREFERENCE set):
        //     ProviderFactory → (optional SanitizingProviderFactory if
        //     trust is on) → AdaptiveProviderFactory.
        //     Load policy is driven by NEXO_LOAD_PREFERENCE value.
        //
        //   Path B — Trust without adaptive (NEXO_TRUST_ENABLED=1,
        //     no load pref):
        //     Trust module registers its own SanitizingProviderFactory
        //     via AddTrustServices (skipProviderRegistration: false).
        //
        //   Path C — Plain (neither trust nor adaptive):
        //     Bare ProviderFactory is registered directly.
        //
        // NEXO_TRUST_ENABLED ("1"): activates the sanitization proxy
        //   that scrubs PII before LLM calls leave the trust boundary.
        // NEXO_LOAD_PREFERENCE (string, e.g. "latency" / "cost"):
        //   activates adaptive load balancing and selects the policy.
        var trustEnabledByConfig = options.TrustEnabled ?? string.Equals(Environment.GetEnvironmentVariable("NEXO_TRUST_ENABLED"), "1", StringComparison.OrdinalIgnoreCase);
        var trustEnabled = modules.IncludeTrustServices && trustEnabledByConfig;
        var loadPref = Environment.GetEnvironmentVariable("NEXO_LOAD_PREFERENCE")?.Trim();
        var useAdaptive = options.UseAdaptiveLoadBalancing ?? !string.IsNullOrEmpty(loadPref);

        if (modules.IncludeTrustServices)
        {
            services.AddTrustServices(useSanitizingProviderFactory: trustEnabled, ephemeralLifecycle: ephemeralModels, skipProviderRegistration: useAdaptive);
        }

        // Path A: adaptive load-balancing wraps everything
        if (useAdaptive)
        {
            services.AddSingleton<ProviderFactory>(sp =>
            {
                var logger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<ProviderFactory>>();
                var lifecycle = sp.GetService<IEphemeralModelLifecycle>();
                return new ProviderFactory(logger, lifecycle);
            });
            services.TryAddSingleton<ILoadPolicy, PreferenceLoadPolicy>();
            services.AddSingleton<IProviderFactory>(sp =>
            {
                var pf = sp.GetRequiredService<ProviderFactory>();
                Nexo.Infrastructure.Execution.IProviderFactory inner = trustEnabled
                    ? new SanitizingProviderFactory(pf, sp.GetRequiredService<ICloudSanitizationProxy>(),
                        sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<SanitizingProviderFactory>>())
                    : pf;
                var policy = sp.GetRequiredService<ILoadPolicy>();
                var logger = sp.GetService<Microsoft.Extensions.Logging.ILogger<AdaptiveProviderFactory>>();
                return new AdaptiveProviderFactory(inner, policy, logger);
            });
        }
        // Path C: plain provider (Path B is handled inside AddTrustServices)
        else if (!trustEnabled)
        {
            services.AddSingleton<IProviderFactory>(sp =>
            {
                var logger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<ProviderFactory>>();
                var lifecycle = sp.GetService<IEphemeralModelLifecycle>();
                return new ProviderFactory(logger, lifecycle);
            });
        }

        // ── Execution core & workflow ──────────────────────────────────
        services.AddSingleton<Nexo.Core.Application.Common.Ports.ITextFileSystem, Nexo.Infrastructure.IO.LocalTextFileSystem>();

        // Workflow integrations (PDF export, webhooks, DB read/write,
        // cluster store) are only available in Full/Server profiles.
        // WorkflowExecutor resolves them as optional dependencies so it
        // can still execute pure in-memory workflows without them.
        if (modules.IncludeWorkflowIntegrations)
        {
            services.AddSingleton<Nexo.Core.Application.Common.Ports.IWorkflowPdfExporter, Nexo.Infrastructure.Workflows.QuestPdfWorkflowExporter>();
            services.AddSingleton<Nexo.Core.Application.Common.Ports.IWorkflowWebhookClient, Nexo.Infrastructure.Workflows.HttpWorkflowWebhookClient>();
            services.AddSingleton<Nexo.Core.Application.Common.Ports.IWorkflowDatabaseReader, Nexo.Infrastructure.Workflows.DapperWorkflowDatabaseReader>();
            services.AddSingleton<Nexo.Core.Application.Common.Ports.IWorkflowDatabaseWriter, Nexo.Infrastructure.Workflows.DapperWorkflowDatabaseWriter>();
            services.AddSingleton<Nexo.Infrastructure.Execution.IClusterRegistry, Nexo.Infrastructure.Execution.ClusterRegistry>();
            services.AddSingleton<Nexo.Core.Application.Common.Ports.IClusterStore, Nexo.Infrastructure.Workflows.ClusterStoreAdapter>();
        }

        // Semantic cache, behavior registry, step mode, and behavior
        // executor form the brick execution pipeline.  TryAddSingleton
        // is used so that test hosts or SDK consumers can substitute
        // any of these before calling AddNexo.
        services.TryAddSingleton<Nexo.Infrastructure.Execution.ISemanticCache>(sp =>
            new Nexo.Infrastructure.Execution.SemanticCache(sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<Nexo.Infrastructure.Execution.SemanticCache>>()));
        services.TryAddSingleton<Nexo.Core.Domain.Execution.IBehaviorRegistry>(_ =>
            new Nexo.Infrastructure.Execution.BehaviorRegistry(Array.Empty<Nexo.Core.Domain.Behaviors.Behavior>()));
        services.TryAddSingleton<Nexo.Core.Application.Execution.Ports.IStepExecutionMode>(sp =>
            new Nexo.Infrastructure.Execution.StepExecutionModeStore(
                null,
                sp.GetService<Microsoft.Extensions.Logging.ILogger<Nexo.Infrastructure.Execution.StepExecutionModeStore>>()));
        services.TryAddSingleton<Nexo.Core.Domain.Execution.IBehaviorExecutor>(sp =>
            new Nexo.Infrastructure.Execution.BehaviorExecutor(
                sp.GetRequiredService<Nexo.Core.Domain.Execution.IBrickRegistry>(),
                sp.GetRequiredService<Nexo.Infrastructure.Execution.IProviderFactory>(),
                sp.GetRequiredService<Nexo.Infrastructure.Execution.ISemanticCache>(),
                sp.GetRequiredService<ILoopKernel>(),
                sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<Nexo.Infrastructure.Execution.BehaviorExecutor>>(),
                sp.GetService<Nexo.Core.Application.Execution.Ports.IAgenticBrickEngine>(),
                sp.GetService<Nexo.Core.Application.Execution.Ports.IStepExecutionMode>(),
                sp.GetService<IMetricsCollector>()));
        // Agent registry is populated from SDK-provided AgentCards; if
        // none are supplied the registry starts empty and agents can be
        // registered later at runtime.
        services.TryAddSingleton<Nexo.Core.Domain.Execution.IAgentRegistry>(sp =>
        {
            var sdkOptions = sp.GetService<Nexo.Hosting.Sdk.NexoSdkOptions>();
            var cards = sdkOptions?.AgentCards?.ToList() ?? new List<Nexo.Core.Domain.Agents.AgentCard>();
            return new Nexo.Infrastructure.Execution.AgentRegistry(cards);
        });

        // ── Workflow executor ──────────────────────────────────────────
        // Scoped because a single workflow execution may accumulate
        // state (e.g. cluster affinity) that should not leak across
        // independent request scopes.
        services.AddScoped<Nexo.Core.Application.Workflows.WorkflowExecutor>(sp =>
            new Nexo.Core.Application.Workflows.WorkflowExecutor(
                sp.GetRequiredService<Nexo.Core.Domain.Execution.IAgentRegistry>(),
                sp.GetRequiredService<Nexo.Core.Domain.Execution.IBrickRegistry>(),
                sp.GetRequiredService<Nexo.Core.Domain.Execution.IBehaviorRegistry>(),
                sp.GetRequiredService<Nexo.Core.Domain.Execution.IBehaviorExecutor>(),
                sp.GetRequiredService<ILoopKernel>(),
                sp.GetRequiredService<Nexo.Core.Application.Common.Ports.ITextFileSystem>(),
                sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<Nexo.Core.Application.Workflows.WorkflowExecutor>>(),
                pdfExporter: sp.GetService<Nexo.Core.Application.Common.Ports.IWorkflowPdfExporter>(),
                webhookClient: sp.GetService<Nexo.Core.Application.Common.Ports.IWorkflowWebhookClient>(),
                databaseReader: sp.GetService<Nexo.Core.Application.Common.Ports.IWorkflowDatabaseReader>(),
                databaseWriter: sp.GetService<Nexo.Core.Application.Common.Ports.IWorkflowDatabaseWriter>(),
                clusterStore: sp.GetService<Nexo.Core.Application.Common.Ports.IClusterStore>()));

        services.AddScoped<Nexo.Core.Application.Agent.Ports.IAgentRegistry, Nexo.Infrastructure.Agent.Adapters.AgentRegistryAdapter>();
        services.AddScoped<Nexo.Core.Application.Agent.Ports.IAgentExecutor, Nexo.Infrastructure.Agent.Adapters.AgentExecutorAdapter>();

        // ── Analysis & validation ──────────────────────────────────────
        // Both services use a caching decorator (CachedAnalysis/
        // CachedValidation) to avoid re-running expensive analysis
        // or parsing when the same input appears within a scope.
        services.AddScoped<Nexo.Core.Application.Analysis.Ports.IAnalysisService>(sp =>
        {
            var inner = new Nexo.Infrastructure.Analysis.Adapters.AnalysisServiceAdapter(
                sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<Nexo.Infrastructure.Analysis.Adapters.AnalysisServiceAdapter>>(),
                sp.GetRequiredService<Nexo.Infrastructure.Analysis.Rules.AnalysisRuleEngine>());
            var cache = sp.GetRequiredService<ICacheStrategy>();
            var logger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<Nexo.Infrastructure.Analysis.Adapters.CachedAnalysisServiceAdapter>>();
            return new Nexo.Infrastructure.Analysis.Adapters.CachedAnalysisServiceAdapter(inner, cache, logger);
        });

        services.AddScoped<Nexo.Core.Application.Validation.Ports.IValidationService>(sp =>
        {
            var inner = new Nexo.Infrastructure.Validation.Adapters.ValidationServiceAdapter(
                sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<Nexo.Infrastructure.Validation.Adapters.ValidationServiceAdapter>>(),
                sp.GetRequiredService<Nexo.Infrastructure.Validation.Parsers.ITestResultParser>());
            var cache = sp.GetRequiredService<ICacheStrategy>();
            var logger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<Nexo.Infrastructure.Validation.Adapters.CachedValidationServiceAdapter>>();
            return new Nexo.Infrastructure.Validation.Adapters.CachedValidationServiceAdapter(inner, cache, logger);
        });

        services.AddSingleton<ICacheStrategy, Nexo.Infrastructure.Caching.MemoryCacheStrategy>();
        services.AddSingleton<IMetricsCollector, Nexo.Infrastructure.Metrics.MemoryMetricsCollector>();
        // ── Testing adapters ───────────────────────────────────────────
        services.AddScoped<Nexo.Core.Application.Testing.Ports.ITestRunner, Nexo.Infrastructure.Testing.TestRunnerAdapter>();

        // NEXO_EXECUTION_REMOTE_URL (URL string): when set, test
        //   execution is delegated to a remote execution service via
        //   HTTP instead of the local Docker-based platform.  Useful in
        //   CI environments where Docker-in-Docker is unavailable.
        if (modules.IncludeTestingAdapters)
        {
            var executionRemoteUrl = options.ExecutionRemoteUrl ?? Environment.GetEnvironmentVariable("NEXO_EXECUTION_REMOTE_URL")?.Trim();
            if (!string.IsNullOrEmpty(executionRemoteUrl))
            {
                var baseUrl = executionRemoteUrl.TrimEnd('/') + "/";
                services.AddHttpClient("NexoExecution", c => c.BaseAddress = new Uri(baseUrl));
                services.AddSingleton<Nexo.Infrastructure.Testing.ExecutionPlatform.IExecutionPlatform>(sp =>
                {
                    var factory = sp.GetRequiredService<IHttpClientFactory>();
                    var client = factory.CreateClient("NexoExecution");
                    var logger = sp.GetService<Microsoft.Extensions.Logging.ILogger<Nexo.Infrastructure.Testing.ExecutionPlatform.RemoteExecutionPlatform>>();
                    return new Nexo.Infrastructure.Testing.ExecutionPlatform.RemoteExecutionPlatform(client, logger);
                });
            }
            else
            {
                services.AddSingleton<Nexo.Infrastructure.Testing.ExecutionPlatform.IExecutionPlatform>(sp =>
                    new Nexo.Infrastructure.Testing.ExecutionPlatform.DockerExecutionPlatform(sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<Nexo.Infrastructure.Testing.ExecutionPlatform.DockerExecutionPlatform>>()));
            }

            services.AddSingleton<Nexo.Infrastructure.Testing.Docker.IDockerService>(sp =>
                new Nexo.Infrastructure.Testing.Docker.DockerService(sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<Nexo.Infrastructure.Testing.Docker.DockerService>>()));
            services.AddSingleton<Nexo.Infrastructure.Testing.CodeAnalysis.ICodeAnalysisService>(sp =>
                new Nexo.Infrastructure.Testing.CodeAnalysis.RoslynCodeAnalysisService(sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<Nexo.Infrastructure.Testing.CodeAnalysis.RoslynCodeAnalysisService>>()));
            services.AddArtifactCleanup();
        }

        // ── Analysis rule engine ───────────────────────────────────────
        // Rules are collected via DI multi-registration and fed into
        // the engine.  Add new IAnalysisRule implementations to extend
        // the static analysis suite without touching this file.
        services.AddScoped<Nexo.Infrastructure.Validation.Parsers.ITestResultParser, Nexo.Infrastructure.Validation.Parsers.TrxTestResultParser>();
        services.AddScoped<Nexo.Infrastructure.Analysis.Rules.IAnalysisRule, Nexo.Infrastructure.Analysis.Rules.SecurityAnalysisRule>();
        services.AddScoped<Nexo.Infrastructure.Analysis.Rules.IAnalysisRule, Nexo.Infrastructure.Analysis.Rules.CodeQualityRule>();
        services.AddScoped<Nexo.Infrastructure.Analysis.Rules.AnalysisRuleEngine>(sp =>
        {
            var rules = sp.GetServices<Nexo.Infrastructure.Analysis.Rules.IAnalysisRule>();
            var logger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<Nexo.Infrastructure.Analysis.Rules.AnalysisRuleEngine>>();
            return new Nexo.Infrastructure.Analysis.Rules.AnalysisRuleEngine(rules, logger);
        });

        return services;
    }

    /// <summary>
    /// Registers the platform-specific NCR (Node Capability Runtime) module.
    /// NCR probes local hardware (GPU, RAM, accelerators) and exposes
    /// capabilities used by <c>ICapabilityRouter</c> to decide
    /// whether a job can run locally or must be routed to a peer/cloud.
    /// Falls back to Linux when the OS is not recognised.
    /// </summary>
    private static void RegisterNodeCapabilityRuntime(IServiceCollection services, IConfiguration configuration)
    {
        services.AddNodeCapabilityRuntimeCore(configuration);
        if (OperatingSystem.IsWindows())
            services.AddNodeCapabilityRuntimeWindows(configuration);
        else if (OperatingSystem.IsMacOS())
            services.AddNodeCapabilityRuntimeMacOS(configuration);
        else if (OperatingSystem.IsLinux())
            services.AddNodeCapabilityRuntimeLinux(configuration);
        else if (OperatingSystem.IsIOS())
            services.AddNodeCapabilityRuntimeiOS(configuration);
        else if (OperatingSystem.IsAndroid())
            services.AddNodeCapabilityRuntimeAndroid(configuration);
        else
            services.AddNodeCapabilityRuntimeLinux(configuration);

        services.AddModelArtifactCatalog(configuration);
        if (OperatingSystem.IsWindows() || OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
        {
            services.AddDockerOllamaModelArtifactCatalogSource();
        }
    }

    /// <summary>
    /// Resolves the deployment profile from (in priority order):
    /// 1. Explicit <see cref="NexoHostingOptions.DeploymentProfile"/> set by the caller.
    /// 2. <c>NEXO_DEPLOYMENT_PROFILE</c> environment variable (case-insensitive;
    ///    accepts "full", "server", "edge", "airgapped"/"air-gapped", "system"/"core").
    /// 3. Falls back to <see cref="NexoDeploymentProfile.Full"/>.
    /// </summary>
    private static NexoDeploymentProfile ResolveDeploymentProfile(NexoHostingOptions options)
    {
        if (options.DeploymentProfile.HasValue)
            return options.DeploymentProfile.Value;

        var raw = Environment.GetEnvironmentVariable("NEXO_DEPLOYMENT_PROFILE");
        if (string.IsNullOrWhiteSpace(raw))
            return NexoDeploymentProfile.Full;

        if (TryParseDeploymentProfile(raw, out var parsed))
            return parsed;

        throw new InvalidOperationException(
            $"NEXO_DEPLOYMENT_PROFILE='{raw}' is not recognized. " +
            "Valid values: full, server, edge, air-gapped, system.");
    }

    private static bool TryParseDeploymentProfile(string? raw, out NexoDeploymentProfile profile)
    {
        profile = NexoDeploymentProfile.Full;
        if (string.IsNullOrWhiteSpace(raw))
            return false;

        var normalized = raw.Trim().ToLowerInvariant();
        profile = normalized switch
        {
            "full" => NexoDeploymentProfile.Full,
            "server" => NexoDeploymentProfile.Server,
            "edge" => NexoDeploymentProfile.Edge,
            "airgapped" => NexoDeploymentProfile.AirGapped,
            "air-gapped" => NexoDeploymentProfile.AirGapped,
            "system" => NexoDeploymentProfile.System,
            "core" => NexoDeploymentProfile.System,
            _ => profile
        };

        return normalized is "full" or "server" or "edge" or "airgapped" or "air-gapped" or "system" or "core";
    }

    /// <summary>
    /// Maps a deployment profile to the set of subsystem modules that should
    /// be registered.  The peeling order (Full → Server → Edge → AirGapped
    /// → System) progressively strips capabilities:
    /// <list type="bullet">
    ///   <item><c>Full</c>     — everything; used in development &amp; CI.</item>
    ///   <item><c>Server</c>   — same as Full (reserved for future server-specific gating).</item>
    ///   <item><c>Edge</c>     — persistence + pipelines only; no NCR, no agents.</item>
    ///   <item><c>AirGapped</c>— NCR + adaptation + persistence; no network transport.</item>
    ///   <item><c>System</c>   — bare minimum for CLI tooling; nothing optional.</item>
    /// </list>
    /// </summary>
    private static ModuleSelection GetModuleSelection(NexoDeploymentProfile profile)
    {
        return profile switch
        {
            NexoDeploymentProfile.Full => new ModuleSelection(
                IncludeNodeCapabilityRuntime: true,
                IncludeRuntimeTransport: true,
                IncludePersistence: true,
                IncludeAdaptation: true,
                IncludePipelineComposition: true,
                IncludeBackgroundAgents: true,
                IncludeBackgroundAgentRag: true,
                IncludeObservationPipeline: true,
                IncludeTrustServices: true,
                IncludeWorkflowIntegrations: true,
                IncludeTestingAdapters: true),
            NexoDeploymentProfile.Server => new ModuleSelection(
                IncludeNodeCapabilityRuntime: true,
                IncludeRuntimeTransport: true,
                IncludePersistence: true,
                IncludeAdaptation: true,
                IncludePipelineComposition: true,
                IncludeBackgroundAgents: true,
                IncludeBackgroundAgentRag: true,
                IncludeObservationPipeline: true,
                IncludeTrustServices: true,
                IncludeWorkflowIntegrations: true,
                IncludeTestingAdapters: true),
            NexoDeploymentProfile.Edge => new ModuleSelection(
                IncludeNodeCapabilityRuntime: false,
                IncludeRuntimeTransport: false,
                IncludePersistence: true,
                IncludeAdaptation: false,
                IncludePipelineComposition: true,
                IncludeBackgroundAgents: false,
                IncludeBackgroundAgentRag: false,
                IncludeObservationPipeline: false,
                IncludeTrustServices: false,
                IncludeWorkflowIntegrations: false,
                IncludeTestingAdapters: false),
            NexoDeploymentProfile.AirGapped => new ModuleSelection(
                IncludeNodeCapabilityRuntime: true,
                IncludeRuntimeTransport: false,
                IncludePersistence: true,
                IncludeAdaptation: true,
                IncludePipelineComposition: true,
                IncludeBackgroundAgents: false,
                IncludeBackgroundAgentRag: false,
                IncludeObservationPipeline: false,
                IncludeTrustServices: false,
                IncludeWorkflowIntegrations: false,
                IncludeTestingAdapters: false),
            NexoDeploymentProfile.System => new ModuleSelection(
                IncludeNodeCapabilityRuntime: false,
                IncludeRuntimeTransport: false,
                IncludePersistence: false,
                IncludeAdaptation: false,
                IncludePipelineComposition: false,
                IncludeBackgroundAgents: false,
                IncludeBackgroundAgentRag: false,
                IncludeObservationPipeline: false,
                IncludeTrustServices: false,
                IncludeWorkflowIntegrations: false,
                IncludeTestingAdapters: false),
            _ => throw new ArgumentOutOfRangeException(nameof(profile), profile, "Unknown Nexo deployment profile.")
        };
    }

    /// <summary>
    /// Applies the <c>NEXO_STRICT_MODE</c> ("1" / "true") environment variable
    /// when the caller has not already enabled strict mode programmatically.
    /// Strict mode turns configuration warnings into hard failures — intended
    /// for CI gates where misconfiguration should break the build.
    /// </summary>
    private static void ResolveStrictMode(NexoHostingOptions options)
    {
        if (!options.StrictMode.Enabled)
            options.StrictMode.Enabled = ParseBooleanEnvironmentVariable("NEXO_STRICT_MODE");
    }

    private static bool ParseBooleanEnvironmentVariable(string key)
    {
        var value = Environment.GetEnvironmentVariable(key);
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        return string.Equals(value, "1", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
    }
}
