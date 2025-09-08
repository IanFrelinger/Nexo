using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Services.Iteration;
using Nexo.Feature.AI.Interfaces;
using Nexo.Feature.AI.Agents.Specialized;
using Nexo.Feature.AI.Agents.Coordination;
using Nexo.Feature.AI.Learning;
using Nexo.Feature.AI.Monitoring;
using Nexo.Feature.AI.Services;

namespace Nexo.Feature.AI.Services;

/// <summary>
/// Extension methods for registering AI services with dependency injection
/// </summary>
public static class AIServiceExtensions
{
    /// <summary>
    /// Adds AI services to the service collection
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddAIServices(this IServiceCollection services)
    {
        // Core AI services
        services.AddSingleton<IModelOrchestrator, ModelOrchestrator>();
        services.AddTransient<Nexo.Feature.AI.Interfaces.IIterationCodeGenerator, IterationCodeGenerator>();
        
        // Mock provider for development and testing
        services.AddTransient<IModelProvider, MockModelProvider>();
        
        return services;
    }

    /// <summary>
    /// Adds AI services with custom configuration
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configureOptions">Action to configure AI options</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddAIServices(
        this IServiceCollection services, 
        Action<AIServiceOptions> configureOptions)
    {
        services.Configure(configureOptions);
        return services.AddAIServices();
    }

    /// <summary>
    /// Adds AI services with iteration strategy integration
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddAIWithIterationStrategies(this IServiceCollection services)
    {
        // Add iteration strategies first
        services.AddIterationStrategies();
        
        // Add AI services
        services.AddAIServices();
        
        // Register the iteration code generator with the iteration system
        services.AddTransient<Nexo.Feature.AI.Interfaces.IIterationCodeGenerator, IterationCodeGenerator>();
        
        return services;
    }

    /// <summary>
    /// Adds specialized AI agent services with coordination capabilities
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddSpecializedAIAgents(this IServiceCollection services)
    {
        // Add core AI services first
        services.AddAIServices();
        
        // Register specialized agents
        services.AddTransient<ISpecializedAgent, PerformanceOptimizationAgent>();
        services.AddTransient<ISpecializedAgent, SecurityAnalysisAgent>();
        services.AddTransient<ISpecializedAgent, UnityOptimizationAgent>();
        services.AddTransient<ISpecializedAgent, WebOptimizationAgent>();
        services.AddTransient<ISpecializedAgent, MobileOptimizationAgent>();
        
        // Register agent coordination services
        services.AddTransient<IAgentCoordinator, AgentCoordinator>();
        services.AddTransient<IAgentWorkflowPlanner, AgentWorkflowPlanner>();
        services.AddTransient<IAgentCommunicationHub, AgentCommunicationHub>();
        
        // Register agent registry
        services.AddSingleton<ISpecializedAgentRegistry, SpecializedAgentRegistry>();
        
        // Register learning and adaptation services
        services.AddTransient<IAgentLearningSystem, AgentLearningSystem>();
        services.AddTransient<IPerformanceFeedbackCollector, PerformanceFeedbackCollector>();
        services.AddTransient<IAgentKnowledgeStore, AgentKnowledgeStore>();
        
        // Register monitoring services
        services.AddTransient<IPerformanceMetricsCollector, PerformanceMetricsCollector>();
        services.AddHostedService<RealTimeAdaptationService>();
        
        // Register enhanced Feature Factory orchestrator
        services.AddTransient<IEnhancedFeatureFactoryOrchestrator, EnhancedFeatureFactoryOrchestrator>();
        
        return services;
    }

    /// <summary>
    /// Adds specialized AI agents with custom configuration
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configureOptions">Action to configure agent options</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddSpecializedAIAgents(
        this IServiceCollection services, 
        Action<SpecializedAgentOptions> configureOptions)
    {
        services.Configure(configureOptions);
        return services.AddSpecializedAIAgents();
    }
}

/// <summary>
/// Configuration options for AI services
/// </summary>
public class AIServiceOptions
{
    /// <summary>
    /// Whether to enable AI code generation
    /// </summary>
    public bool EnableCodeGeneration { get; set; } = true;
    
    /// <summary>
    /// Default temperature for AI requests
    /// </summary>
    public double DefaultTemperature { get; set; } = 0.7;
    
    /// <summary>
    /// Default maximum tokens for AI requests
    /// </summary>
    public int DefaultMaxTokens { get; set; } = 1000;
    
    /// <summary>
    /// Whether to use mock providers in development
    /// </summary>
    public bool UseMockProviders { get; set; } = true;
    
    /// <summary>
    /// Timeout for AI requests in milliseconds
    /// </summary>
    public int RequestTimeoutMs { get; set; } = 30000;
}

/// <summary>
/// Configuration options for specialized AI agents
/// </summary>
public class SpecializedAgentOptions
{
    /// <summary>
    /// Whether to enable specialized agent coordination
    /// </summary>
    public bool EnableAgentCoordination { get; set; } = true;
    
    /// <summary>
    /// Whether to enable real-time adaptation
    /// </summary>
    public bool EnableRealTimeAdaptation { get; set; } = true;
    
    /// <summary>
    /// Whether to enable agent learning
    /// </summary>
    public bool EnableAgentLearning { get; set; } = true;
    
    /// <summary>
    /// Adaptation interval in minutes
    /// </summary>
    public int AdaptationIntervalMinutes { get; set; } = 5;
    
    /// <summary>
    /// Minimum records required for learning cycle
    /// </summary>
    public int MinimumRecordsForLearning { get; set; } = 10;
    
    /// <summary>
    /// Maximum learning records to keep per agent
    /// </summary>
    public int MaxLearningRecordsPerAgent { get; set; } = 1000;
}
