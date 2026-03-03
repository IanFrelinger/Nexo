using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Nexo.BackgroundAgents;
using Nexo.Core.Application.Analysis.UseCases.AnalyzeCode;
using Nexo.Core.Application.Ephemeral.Ports;
using Nexo.Core.Application.Validation.UseCases.RunValidation;
using Nexo.Core.Application.Testing.UseCases.RunTests;
using Nexo.Core.Application.Common.Ports;
using Nexo.Core.Application.Common.Services;
using Nexo.Infrastructure;
using Nexo.Infrastructure.Execution.Ephemeral;
using Nexo.Infrastructure.Maintenance;
using Nexo.Infrastructure.Persistence.Ephemeral;
using Nexo.Infrastructure.Persistence;
using Nexo.Orchestration;
using Nexo.Orchestration.Models;

namespace Nexo.Hosting;

/// <summary>
/// Extension methods for registering the Nexo kernel in dependency injection.
/// Call AddNexo() to register all kernel services (orchestration, adaptation, persistence, trust, etc.)
/// with sensible defaults. Use options to override config path, pattern store, and trust settings.
/// </summary>
public static class NexoServiceCollectionExtensions
{
    /// <summary>
    /// Adds the Nexo kernel to the service collection. Registers orchestration, adaptation,
    /// persistence, trust services, provider factory, workflow executor, analysis, validation,
    /// and agent execution. Use <paramref name="configure"/> to override defaults.
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

        services.AddHttpClient();

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
        services.AddNexoPersistence();
        services.AddAdaptationInfrastructure(options.PatternStorePath);
        services.AddBackgroundAgents(registerHostedService: options.RegisterBackgroundAgentHostedService);
        services.AddBackgroundAgentsRAG();
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
        if (string.Equals(ephemeralDb, "postgres", StringComparison.OrdinalIgnoreCase))
            services.AddSingleton<Nexo.Core.Application.Persistence.Ports.IEphemeralDatabaseLifecycle, PostgresEphemeralLifecycle>();

        var trustEnabled = options.TrustEnabled ?? string.Equals(Environment.GetEnvironmentVariable("NEXO_TRUST_ENABLED"), "1", StringComparison.OrdinalIgnoreCase);
        services.AddTrustServices(useSanitizingProviderFactory: trustEnabled, ephemeralLifecycle: ephemeralModels);
        if (!trustEnabled)
        {
            services.AddSingleton<Nexo.Infrastructure.Execution.IProviderFactory>(sp =>
            {
                var logger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<Nexo.Infrastructure.Execution.ProviderFactory>>();
                var lifecycle = sp.GetService<IEphemeralModelLifecycle>();
                return new Nexo.Infrastructure.Execution.ProviderFactory(logger, lifecycle);
            });
        }

        services.AddSingleton<Nexo.Core.Application.Common.Ports.ITextFileSystem, Nexo.Infrastructure.IO.LocalTextFileSystem>();
        services.AddSingleton<Nexo.Core.Application.Common.Ports.IWorkflowPdfExporter, Nexo.Infrastructure.Workflows.QuestPdfWorkflowExporter>();
        services.AddSingleton<Nexo.Core.Application.Common.Ports.IWorkflowWebhookClient, Nexo.Infrastructure.Workflows.HttpWorkflowWebhookClient>();
        services.AddSingleton<Nexo.Core.Application.Common.Ports.IWorkflowDatabaseReader, Nexo.Infrastructure.Workflows.DapperWorkflowDatabaseReader>();
        services.AddSingleton<Nexo.Core.Application.Common.Ports.IWorkflowDatabaseWriter, Nexo.Infrastructure.Workflows.DapperWorkflowDatabaseWriter>();
        services.AddSingleton<Nexo.Infrastructure.Execution.IClusterRegistry, Nexo.Infrastructure.Execution.ClusterRegistry>();
        services.AddSingleton<Nexo.Core.Application.Common.Ports.IClusterStore, Nexo.Infrastructure.Workflows.ClusterStoreAdapter>();

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
                sp.GetService<Nexo.Core.Application.Execution.Ports.IStepExecutionMode>()));
        services.TryAddSingleton<Nexo.Core.Domain.Execution.IAgentRegistry>(_ =>
            new Nexo.Infrastructure.Execution.AgentRegistry(Array.Empty<Nexo.Core.Domain.Agents.AgentCard>()));
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
        services.AddSingleton<Nexo.Infrastructure.Testing.ExecutionPlatform.IExecutionPlatform>(sp =>
            new Nexo.Infrastructure.Testing.ExecutionPlatform.DockerExecutionPlatform(sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<Nexo.Infrastructure.Testing.ExecutionPlatform.DockerExecutionPlatform>>()));
        services.AddSingleton<Nexo.Infrastructure.Testing.Docker.IDockerService>(sp =>
            new Nexo.Infrastructure.Testing.Docker.DockerService(sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<Nexo.Infrastructure.Testing.Docker.DockerService>>()));
        services.AddSingleton<Nexo.Infrastructure.Testing.CodeAnalysis.ICodeAnalysisService>(sp =>
            new Nexo.Infrastructure.Testing.CodeAnalysis.RoslynCodeAnalysisService(sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<Nexo.Infrastructure.Testing.CodeAnalysis.RoslynCodeAnalysisService>>()));
        services.AddArtifactCleanup();
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
}
