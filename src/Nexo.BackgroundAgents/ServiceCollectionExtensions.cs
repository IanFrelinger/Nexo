using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Nexo.BackgroundAgents.Configuration;
using Nexo.BackgroundAgents.DataSensitivity;
using Nexo.BackgroundAgents.Logging;
using Nexo.BackgroundAgents.Extending;
using Nexo.BackgroundAgents.Optimization;
using Nexo.BackgroundAgents.Registry;
using Nexo.BackgroundAgents.Testing;
using Nexo.BackgroundAgents.RAG;
using Nexo.BackgroundAgents.Scheduling;
using Nexo.BackgroundAgents.Services;
using Nexo.BackgroundAgents.Tools;
using Nexo.Orchestration.Agents;

namespace Nexo.BackgroundAgents;

/// <summary>
/// Extension methods for registering background agent services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds background agent services including scheduler, registry, and policy integration.
    /// Requires orchestration (AgentFactory, LifecycleManager for BackgroundAgentService) and configuration to be registered.
    /// </summary>
    /// <param name="registerHostedService">If true, registers BackgroundAgentService as a hosted service (default true). Set false for CLI-only usage.</param>
    public static IServiceCollection AddBackgroundAgents(this IServiceCollection services, bool registerHostedService = true)
    {
        services.TryAddSingleton<IDataSensitivityRegistry, DataSensitivityRegistry>();
        services.TryAddSingleton<IBackgroundAgentLogStore, InMemoryAgentLogStore>();
        services.TryAddSingleton<IScheduleExecutor, ScheduleExecutor>();
        services.AddSingleton<IAgentScheduler, AgentScheduler>();
        services.AddSingleton<IBackgroundAgentRegistry>(sp =>
        {
            var scheduler = sp.GetRequiredService<IAgentScheduler>();
            var logger = sp.GetService<Microsoft.Extensions.Logging.ILogger<BackgroundAgentRegistry>>();
            var logStore = sp.GetService<IBackgroundAgentLogStore>();
            var codeAnalysisRunner = sp.GetService<ICodeAnalysisRunner>();
            var testRunRunner = sp.GetService<ITestRunRunner>();
            var selfExtendRunner = sp.GetService<ISelfExtendRunner>();
            return new BackgroundAgentRegistry(scheduler, logger, logStore, codeAnalysisRunner, testRunRunner, selfExtendRunner);
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
}
