using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Nexo.BackgroundAgents;
using Nexo.BackgroundAgents.Trust;
using Nexo.Core.Application.Analysis.UseCases.AnalyzeCode;
using Nexo.Core.Application.Ephemeral.Ports;
using Nexo.Core.Application.Validation.UseCases.RunValidation;
using Nexo.Core.Application.Testing.UseCases.RunTests;
using Nexo.Core.Application.Common.Ports;
using Nexo.Core.Application.Common.Services;
using Nexo.Core.Application.Paths;
using Nexo.Infrastructure;
using Nexo.Infrastructure.Execution;
using Nexo.Infrastructure.Execution.Ephemeral;
using Nexo.Infrastructure.Execution.LoadPolicy;
using Nexo.Infrastructure.Maintenance;
using Nexo.Infrastructure.NodeCapabilityRuntime;
using Nexo.Infrastructure.Pipelines;
using Nexo.Infrastructure.Persistence.Ephemeral;
using Nexo.Infrastructure.Persistence;
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
/// Extension methods for registering the Nexo kernel in dependency injection.
/// Call AddNexo() to register all kernel services (orchestration, adaptation, persistence, trust, etc.)
/// with sensible defaults. Use options to override config path, pattern store, and trust settings.
/// </summary>
public static class NexoServiceCollectionExtensions
{
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
    /// Adds the Nexo kernel to the service collection. Registers orchestration, adaptation,
    /// persistence, trust services, provider factory, workflow executor, analysis, validation,
    /// and agent execution. Use <paramref name="configure"/> to override defaults, including
    /// deployment profile selection for environment-specific dependency peeling.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional action to configure NexoHostingOptions.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddNexo(
        this IServiceCollection services,
        Action<NexoHostingOptions>? configure = null)
    {
        var options = new NexoHostingOptions();
        configure?.Invoke(options);
        var deploymentProfile = ResolveDeploymentProfile(options);
        var modules = GetModuleSelection(deploymentProfile);

        services.AddHttpClient();
        var configuration = new ConfigurationBuilder()
            .AddEnvironmentVariables()
            .Build();
        services.AddOptions<Nexo.Infrastructure.Execution.RemoteCapabilitiesOptions>()
            .Bind(configuration.GetSection("Nexo:RemoteCapabilities"));
        if (modules.IncludeNodeCapabilityRuntime)
            RegisterNodeCapabilityRuntime(services, configuration);

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(AnalyzeCodeCommand).Assembly);
            cfg.RegisterServicesFromAssembly(typeof(RunTestsCommand).Assembly);
        });

        services.AddValidatorsFromAssembly(typeof(AnalyzeCodeValidator).Assembly);
        services.AddTransient(typeof(MediatR.IPipelineBehavior<,>), typeof(Nexo.Core.Application.Behaviors.ValidationBehavior<,>));

        services.AddSingleton<Nexo.Core.Application.Configuration.Ports.IConfigurationService>(sp =>
        {
            var logger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<Nexo.Infrastructure.Configuration.ConfigurationServiceAdapter>>();
            return new Nexo.Infrastructure.Configuration.ConfigurationServiceAdapter(logger);
        });

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

        services.AddNexoOrchestration();
        if (modules.IncludeRuntimeTransport)
        {
            services.AddOptions<GrpcTransportOptions>();
            services.AddOptions<RoutingOptions>();
            services.TryAddSingleton<IEndpointRegistry, InMemoryEndpointRegistry>();
            services.TryAddSingleton<IGrpcChannelFactory, DefaultGrpcChannelFactory>();
            services.AddNexoRuntimeTransport<InProcessAgentTransport, GrpcAgentTransport>();
        }

        if (modules.IncludePersistence)
            services.AddNexoPersistence();

        if (modules.IncludeAdaptation)
            services.AddAdaptationInfrastructure(options.PatternStorePath);

        if (modules.IncludePipelineComposition)
            services.AddPipelineCompositionLayer();

        if (modules.IncludeBackgroundAgents)
            services.AddBackgroundAgents(registerHostedService: options.RegisterBackgroundAgentHostedService);

        if (modules.IncludeBackgroundAgentRag)
            services.AddBackgroundAgentsRAG();

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

        if (modules.IncludeBackgroundAgents)
            services.TryAddSingleton<Nexo.BackgroundAgents.WebSearch.IWebSearchProvider, Nexo.BackgroundAgents.WebSearch.MockWebSearchProvider>();

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

        var ephemeralAll = string.Equals(Environment.GetEnvironmentVariable("NEXO_EPHEMERAL"), "1", StringComparison.OrdinalIgnoreCase);
        var ephemeralModels = ephemeralAll || string.Equals(Environment.GetEnvironmentVariable("NEXO_EPHEMERAL_MODELS"), "1", StringComparison.OrdinalIgnoreCase);
        if (ephemeralModels)
            services.AddSingleton<IEphemeralModelLifecycle, OllamaEphemeralLifecycle>();

        var ephemeralDb = Environment.GetEnvironmentVariable("NEXO_EPHEMERAL_DB")?.Trim();
        if (modules.IncludePersistence && string.Equals(ephemeralDb, "postgres", StringComparison.OrdinalIgnoreCase))
            services.AddSingleton<Nexo.Core.Application.Persistence.Ports.IEphemeralDatabaseLifecycle, PostgresEphemeralLifecycle>();

        var trustEnabledByConfig = options.TrustEnabled ?? string.Equals(Environment.GetEnvironmentVariable("NEXO_TRUST_ENABLED"), "1", StringComparison.OrdinalIgnoreCase);
        var trustEnabled = modules.IncludeTrustServices && trustEnabledByConfig;
        var loadPref = Environment.GetEnvironmentVariable("NEXO_LOAD_PREFERENCE")?.Trim();
        var useAdaptive = options.UseAdaptiveLoadBalancing ?? !string.IsNullOrEmpty(loadPref);

        if (modules.IncludeTrustServices)
        {
            services.AddTrustServices(useSanitizingProviderFactory: trustEnabled, ephemeralLifecycle: ephemeralModels, skipProviderRegistration: useAdaptive);
        }

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
        else if (!trustEnabled)
        {
            services.AddSingleton<IProviderFactory>(sp =>
            {
                var logger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<ProviderFactory>>();
                var lifecycle = sp.GetService<IEphemeralModelLifecycle>();
                return new ProviderFactory(logger, lifecycle);
            });
        }

        services.AddSingleton<Nexo.Core.Application.Common.Ports.ITextFileSystem, Nexo.Infrastructure.IO.LocalTextFileSystem>();

        if (modules.IncludeWorkflowIntegrations)
        {
            services.AddSingleton<Nexo.Core.Application.Common.Ports.IWorkflowPdfExporter, Nexo.Infrastructure.Workflows.QuestPdfWorkflowExporter>();
            services.AddSingleton<Nexo.Core.Application.Common.Ports.IWorkflowWebhookClient, Nexo.Infrastructure.Workflows.HttpWorkflowWebhookClient>();
            services.AddSingleton<Nexo.Core.Application.Common.Ports.IWorkflowDatabaseReader, Nexo.Infrastructure.Workflows.DapperWorkflowDatabaseReader>();
            services.AddSingleton<Nexo.Core.Application.Common.Ports.IWorkflowDatabaseWriter, Nexo.Infrastructure.Workflows.DapperWorkflowDatabaseWriter>();
            services.AddSingleton<Nexo.Infrastructure.Execution.IClusterRegistry, Nexo.Infrastructure.Execution.ClusterRegistry>();
            services.AddSingleton<Nexo.Core.Application.Common.Ports.IClusterStore, Nexo.Infrastructure.Workflows.ClusterStoreAdapter>();
        }

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
        services.TryAddSingleton<Nexo.Core.Domain.Execution.IAgentRegistry>(sp =>
        {
            var sdkOptions = sp.GetService<Nexo.Hosting.Sdk.NexoSdkOptions>();
            var cards = sdkOptions?.AgentCards?.ToList() ?? new List<Nexo.Core.Domain.Agents.AgentCard>();
            return new Nexo.Infrastructure.Execution.AgentRegistry(cards);
        });
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
        services.AddScoped<Nexo.Core.Application.Testing.Ports.ITestRunner, Nexo.Infrastructure.Testing.TestRunnerAdapter>();

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
    }

    private static NexoDeploymentProfile ResolveDeploymentProfile(NexoHostingOptions options)
    {
        if (options.DeploymentProfile.HasValue)
            return options.DeploymentProfile.Value;

        var raw = Environment.GetEnvironmentVariable("NEXO_DEPLOYMENT_PROFILE");
        if (TryParseDeploymentProfile(raw, out var parsed))
            return parsed;

        return NexoDeploymentProfile.Full;
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
