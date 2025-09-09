using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Nexo.Core.Domain.Interfaces.Infrastructure;
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
        services.AddSingleton<Nexo.Core.Application.Services.Adaptation.IAdaptationEngine, AdaptationEngine>();
        services.AddSingleton<Nexo.Core.Application.Services.Adaptation.Strategies.IAdaptationStrategyRegistry, AdaptationStrategyRegistry>();
        services.AddSingleton<Nexo.Core.Application.Services.Adaptation.IAdaptationDataStore, InMemoryAdaptationDataStore>();
        
        // Adaptation strategies
        services.AddSingleton<Nexo.Core.Application.Services.Adaptation.Strategies.IAdaptationStrategy, PerformanceAdaptationStrategy>();
        services.AddSingleton<Nexo.Core.Application.Services.Adaptation.Strategies.IAdaptationStrategy, ResourceAdaptationStrategy>();
        services.AddSingleton<Nexo.Core.Application.Services.Adaptation.Strategies.IAdaptationStrategy, UserExperienceAdaptationStrategy>();
        
        // Learning services
        services.AddSingleton<Nexo.Core.Application.Services.Learning.IContinuousLearningSystem, ContinuousLearningSystem>();
        services.AddSingleton<Nexo.Core.Application.Services.Learning.IUserFeedbackCollector, UserFeedbackCollector>();
        services.AddSingleton<Nexo.Core.Application.Services.Learning.IPatternRecognitionEngine, PatternRecognitionEngine>();
        services.AddSingleton<Nexo.Core.Application.Services.Learning.IAdaptationRecommender, AdaptationRecommender>();
        services.AddSingleton<Nexo.Core.Application.Services.Adaptation.IPerformanceDataStore, InMemoryPerformanceDataStore>();
        services.AddSingleton<Nexo.Core.Application.Services.Learning.IFeedbackStore, InMemoryFeedbackStore>();
        services.AddSingleton<Nexo.Core.Application.Services.Learning.IFeedbackAnalyzer, FeedbackAnalyzer>();
        
        // Environment services
        services.AddSingleton<Nexo.Core.Application.Services.Environment.IEnvironmentDetector, EnvironmentDetector>();
        services.AddSingleton<Nexo.Core.Application.Services.Environment.IEnvironmentAdaptationService, EnvironmentAdaptationService>();
        services.AddSingleton<Nexo.Core.Application.Services.Environment.IEnvironmentDataStore, InMemoryEnvironmentDataStore>();
        services.AddSingleton<Nexo.Core.Application.Services.Adaptation.IConfigurationManager, ConfigurationManager>();
        
        // Resource management
        services.AddSingleton<Nexo.Core.Application.Services.Adaptation.IResourceManager, ResourceManager>();
        services.AddSingleton<Nexo.Core.Application.Services.Adaptation.IUserExperienceAnalyzer, UserExperienceAnalyzer>();
        services.AddSingleton<Nexo.Core.Application.Services.Adaptation.ICodeGenerationOptimizer, CodeGenerationOptimizer>();
        
        // Performance monitoring
        services.AddSingleton<Nexo.Core.Application.Services.Adaptation.IPerformanceMonitor, PerformanceMonitor>();
        services.AddSingleton<Nexo.Core.Application.Services.Adaptation.IMetricsAggregator, MetricsAggregator>();
        
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