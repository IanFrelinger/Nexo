using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Http;
using Nexo.Abstractions.Routing;
using Nexo.Abstractions.Transport;
using Nexo.BackgroundAgents;
using Nexo.BackgroundAgents.Trust;
using Nexo.Core.Application.Adaptation.Ports;
using Nexo.Core.Application.Analysis.UseCases.AnalyzeCode;
using Nexo.Core.Application.Common.Ports;
using Nexo.Core.Application.Common.Services;
using Nexo.Core.Application.Copilot.Ports;
using Nexo.Core.Application.Ephemeral.Ports;
using Nexo.Core.Application.Knowledge.Ports;
using Nexo.Core.Application.Observation.Ports;
using Nexo.Core.Application.Paths;
using Nexo.Core.Application.Testing.UseCases.RunTests;
using Nexo.Core.Application.Trust.Ports;
using Nexo.Core.Application.Validation.UseCases.RunValidation;
using Nexo.Infrastructure;
using Nexo.Infrastructure.Copilot;
using Nexo.Infrastructure.Execution;
using Nexo.Infrastructure.Execution.Ephemeral;
using Nexo.Infrastructure.Execution.LoadPolicy;
using Nexo.Infrastructure.Execution.Routing;
using Nexo.Infrastructure.Knowledge;
using Nexo.Infrastructure.Maintenance;
using Nexo.Infrastructure.ModelArtifacts;
using Nexo.Infrastructure.NodeCapabilityRuntime;
using Nexo.Infrastructure.Persistence;
using Nexo.Infrastructure.Persistence.Ephemeral;
using Nexo.Infrastructure.Pipelines;
using Nexo.Orchestration;
using Nexo.Orchestration.Models;
using Nexo.Orchestration.Transport;
using Nexo.Runtime;
using Nexo.Runtime.Routing;
using Nexo.Transport.Grpc;

namespace Nexo.Hosting;

/// <summary>Extracted kernel DI phases from <see cref="NexoServiceCollectionExtensions.AddNexo"/>. Registration order is preserved.</summary>
internal static class NexoKernelRegistrar
{
    public static void Register(
        IServiceCollection services,
        NexoHostingOptions options,
        ModuleSelection modules,
        IConfiguration configuration)
    {
        // ── Configuration & Node Capability Runtime ────────────────────
        // Environment variables are the primary config source; appsettings
        // is intentionally NOT loaded here so that containerised deployments
        // stay 12-factor compliant.  RemoteCapabilitiesOptions binds from
        // the "Nexo:RemoteCapabilities" section for RunPod/cloud routing.
        services.AddOptions<Nexo.Infrastructure.Execution.RemoteCapabilitiesOptions>()
        .Bind(configuration.GetSection("Nexo:RemoteCapabilities"));
        if (modules.IncludeNodeCapabilityRuntime)
        {
            services.AddRunPodCapabilityRouting(configuration);
            NexoServiceCollectionExtensions.RegisterNodeCapabilityRuntime(services, configuration);
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
            {
                k = new ParallelLoopKernel(k);
            }

            var instrument = string.Equals(Environment.GetEnvironmentVariable("NEXO_LOOP_INSTRUMENT"), "1", StringComparison.OrdinalIgnoreCase);
            if (instrument)
            {
                k = new InstrumentedLoopKernel(k, sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<InstrumentedLoopKernel>>());
            }

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
        {
            services.AddAdaptationInfrastructure(options.PatternStorePath);
        }

        if (modules.IncludeAdaptation)
        {
            services.AddNexoFederatedBrickMesh(configuration);
        }

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
        {
            services.AddPipelineCompositionLayer();
        }

        // ── Background agents & RAG ────────────────────────────────────
        if (modules.IncludeBackgroundAgents)
        {
            services.AddBackgroundAgents(registerHostedService: options.RegisterBackgroundAgentHostedService);
        }

        if (modules.IncludeBackgroundAgentRag)
        {
            services.AddBackgroundAgentsRAG();
        }

        // ── Observation pipeline ───────────────────────────────────────
        // Captures runtime telemetry and persists it alongside patterns.
        // NEXO_OBSERVATION_FAIL_OPEN ("1" / "true"): when set, store I/O
        //   errors are swallowed instead of failing the pipeline — safe
        //   for edge nodes with unreliable storage.
        if (modules.IncludeObservationPipeline && !options.DisableObservationPipeline)
        {
            var repoRoot = RepoPathResolver.FindRepoRoot();
            var observationFailOpen = options.ObservationFailOpen ?? NexoServiceCollectionExtensions.ParseBooleanEnvironmentVariable("NEXO_OBSERVATION_FAIL_OPEN");
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
        {
            services.TryAddSingleton<Nexo.BackgroundAgents.WebSearch.IWebSearchProvider, Nexo.BackgroundAgents.WebSearch.MockWebSearchProvider>();
        }

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
        {
            services.AddSingleton<IEphemeralModelLifecycle, OllamaEphemeralLifecycle>();
        }

        var ephemeralDb = Environment.GetEnvironmentVariable("NEXO_EPHEMERAL_DB")?.Trim();
        if (modules.IncludePersistence && string.Equals(ephemeralDb, "postgres", StringComparison.OrdinalIgnoreCase))
        {
            services.AddSingleton<Nexo.Core.Application.Persistence.Ports.IEphemeralDatabaseLifecycle, PostgresEphemeralLifecycle>();
        }

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
    }
}
