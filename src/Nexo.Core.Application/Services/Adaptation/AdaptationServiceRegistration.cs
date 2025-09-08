using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Nexo.Core.Application.Services.Adaptation.Strategies;
using Nexo.Core.Application.Services.Environment;
using Nexo.Core.Application.Services.Learning;

namespace Nexo.Core.Application.Services.Adaptation;

/// <summary>
/// Service registration for adaptation capabilities
/// </summary>
public static class AdaptationServiceRegistration
{
    /// <summary>
    /// Register all adaptation services
    /// </summary>
    public static IServiceCollection AddAdaptationServices(this IServiceCollection services)
    {
        // Core adaptation services
        services.AddSingleton<IAdaptationEngine, AdaptationEngine>();
        services.AddSingleton<IAdaptationStrategyRegistry, AdaptationStrategyRegistry>();
        services.AddSingleton<IAdaptationDataStore, InMemoryAdaptationDataStore>();
        
        // Adaptation strategies
        services.AddSingleton<IAdaptationStrategy, PerformanceAdaptationStrategy>();
        services.AddSingleton<IAdaptationStrategy, ResourceAdaptationStrategy>();
        services.AddSingleton<IAdaptationStrategy, UserExperienceAdaptationStrategy>();
        
        // Learning services
        services.AddSingleton<IContinuousLearningSystem, ContinuousLearningSystem>();
        services.AddSingleton<IUserFeedbackCollector, UserFeedbackCollector>();
        services.AddSingleton<IPatternRecognitionEngine, PatternRecognitionEngine>();
        services.AddSingleton<IAdaptationRecommender, AdaptationRecommender>();
        services.AddSingleton<IPerformanceDataStore, InMemoryPerformanceDataStore>();
        services.AddSingleton<IFeedbackStore, InMemoryFeedbackStore>();
        services.AddSingleton<IFeedbackAnalyzer, FeedbackAnalyzer>();
        
        // Environment services
        services.AddSingleton<IEnvironmentDetector, EnvironmentDetector>();
        services.AddSingleton<IEnvironmentAdaptationService, EnvironmentAdaptationService>();
        services.AddSingleton<IEnvironmentDataStore, InMemoryEnvironmentDataStore>();
        services.AddSingleton<IConfigurationManager, ConfigurationManager>();
        
        // Resource management
        services.AddSingleton<IResourceManager, ResourceManager>();
        services.AddSingleton<IUserExperienceAnalyzer, UserExperienceAnalyzer>();
        services.AddSingleton<ICodeGenerationOptimizer, CodeGenerationOptimizer>();
        
        // Performance monitoring
        services.AddSingleton<IPerformanceMonitor, PerformanceMonitor>();
        services.AddSingleton<IMetricsAggregator, MetricsAggregator>();
        
        // Register as hosted service
        services.AddHostedService<AdaptationEngine>();
        
        return services;
    }
    
    /// <summary>
    /// Configure adaptation settings
    /// </summary>
    public static IServiceCollection ConfigureAdaptationSettings(this IServiceCollection services, Action<AdaptationSettings> configure)
    {
        services.Configure(configure);
        return services;
    }
}

/// <summary>
/// Adaptation settings configuration
/// </summary>
public class AdaptationSettings
{
    public TimeSpan AdaptationInterval { get; set; } = TimeSpan.FromSeconds(30);
    public TimeSpan LearningCycleInterval { get; set; } = TimeSpan.FromHours(1);
    public TimeSpan FeedbackAnalysisInterval { get; set; } = TimeSpan.FromMinutes(15);
    public TimeSpan EnvironmentCheckInterval { get; set; } = TimeSpan.FromMinutes(5);
    public bool EnableRealTimeAdaptation { get; set; } = true;
    public bool EnableLearningSystem { get; set; } = true;
    public bool EnableEnvironmentAdaptation { get; set; } = true;
    public bool EnablePerformanceMonitoring { get; set; } = true;
    public int MaxConcurrentAdaptations { get; set; } = 3;
    public double MinConfidenceThreshold { get; set; } = 0.6;
    public double MinImprovementThreshold { get; set; } = 0.1;
}