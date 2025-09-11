using Microsoft.Extensions.DependencyInjection;
using Nexo.Core.Application.Interfaces.AI;
using Nexo.Core.Application.Interfaces.Services;
using Nexo.Infrastructure.Services.AI;
using Nexo.Infrastructure.Services.Caching;
using Nexo.Infrastructure.Services.Monitoring;
using Nexo.Infrastructure.Services.Platform;
using Nexo.Infrastructure.Services.Security;
using Nexo.Infrastructure.Services.Performance;
using Nexo.Infrastructure.Services.Analytics;
using Nexo.Infrastructure.Services.Learning;
using Nexo.Infrastructure.Services.Enterprise;
using Nexo.Infrastructure.Services.Collaboration;
using Nexo.Infrastructure.Services.Resource;

namespace Nexo.Infrastructure.Extensions
{
    /// <summary>
    /// Extension methods for registering Infrastructure services
    /// </summary>
    public static class InfrastructureServiceExtensions
    {
        /// <summary>
        /// Adds Infrastructure services to the dependency injection container
        /// </summary>
        public static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
        {
            // Register AI Infrastructure services
            services.AddSingleton<IAIProvider, OllamaProvider>();
            services.AddSingleton<IAIProvider, LlamaNativeProvider>();
            services.AddSingleton<IModelOrchestrator, ModelOrchestrator>();
            services.AddSingleton<AdvancedAIService>();
            services.AddSingleton<AIAnalyticsService>();

            // Register Platform Infrastructure services
            services.AddSingleton<IDesktopCodeGenerator, DesktopCodeGenerator>();
            services.AddSingleton<IWebCodeGenerator, WebCodeGenerator>();
            services.AddSingleton<IAndroidCodeGenerator, AndroidCodeGenerator>();
            services.AddSingleton<iOSCodeGenerator>();
            services.AddSingleton<PlatformFeatureDetectionService>();
            services.AddSingleton<PlatformPerformanceOptimizationService>();
            services.AddSingleton<NativeApiIntegrationService>();

            // Register Caching Infrastructure services
            services.AddSingleton<ICacheConfigurationService, CacheConfigurationService>();
            services.AddSingleton<AdvancedCacheConfigurationService>();
            services.AddSingleton<CachePerformanceMonitor>();

            // Register Monitoring Infrastructure services
            services.AddSingleton<IProductionMonitoringService, ProductionMonitoringService>();
            services.AddSingleton<ProductionSecurityAuditor>();

            // Register Performance Infrastructure services
            services.AddSingleton<IProductionPerformanceOptimizer, ProductionPerformanceOptimizer>();
            services.AddSingleton<ModelLoadingOptimizationService>();
            services.AddSingleton<PredictiveDevelopmentService>();

            // Register Analytics Infrastructure services
            services.AddSingleton<IComprehensiveReportingService, ComprehensiveReportingService>();

            // Register Learning Infrastructure services
            services.AddSingleton<OptimizationRecommendationService>();
            services.AddSingleton<CollectiveIntelligenceService>();
            services.AddSingleton<AILearningSystem>();

            // Register Enterprise Infrastructure services
            services.AddSingleton<EnterpriseSecurityService>();

            // Register Collaboration Infrastructure services
            services.AddSingleton<TeamCollaborationService>();

            // Register Resource Infrastructure services
            services.AddSingleton<BasicResourceManager>();

            return services;
        }

        /// <summary>
        /// Adds Infrastructure services with custom configuration
        /// </summary>
        public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, Action<InfrastructureServiceOptions> configure)
        {
            var options = new InfrastructureServiceOptions();
            configure(options);

            // Register core infrastructure services
            services.AddInfrastructureServices();

            // Register configuration
            services.AddSingleton(options);

            return services;
        }
    }

    /// <summary>
    /// Configuration options for Infrastructure services
    /// </summary>
    public class InfrastructureServiceOptions
    {
        public bool EnableOllamaProvider { get; set; } = true;
        public bool EnableNativeProvider { get; set; } = true;
        public bool EnableRedisCaching { get; set; } = false;
        public bool EnableAdvancedAnalytics { get; set; } = true;
        public bool EnableEnterpriseFeatures { get; set; } = false;
        public string DefaultCacheBackend { get; set; } = "inmemory";
        public string RedisConnectionString { get; set; } = "localhost:6379";
        public Dictionary<string, object> CustomSettings { get; set; } = new();
    }
}
