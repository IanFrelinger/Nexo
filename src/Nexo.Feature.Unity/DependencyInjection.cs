using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nexo.Feature.Unity.Interfaces;
using Nexo.Feature.Unity.Services;
using Nexo.Feature.Unity.AI.Agents;
using Nexo.Feature.Unity.Workflows;
using Nexo.Feature.Unity.Monitoring;
using Nexo.Feature.AI.Interfaces;
using Nexo.Core.Application.Services.Adaptation;

namespace Nexo.Feature.Unity
{
    /// <summary>
    /// Dependency injection configuration for Unity feature
    /// This class acts as an orchestrator, delegating specific functionalities to partial class implementations.
    /// </summary>
    public static partial class DependencyInjection
    {
        /// <summary>
        /// Adds Unity feature services to the service collection
        /// </summary>
        public static IServiceCollection AddUnityFeature(this IServiceCollection services)
        {
            // Core Unity services
            services.AddTransient<IFileSystemService, FileSystemService>();
            services.AddTransient<IUnityProjectAnalyzer, UnityProjectAnalyzer>();
            services.AddTransient<IUnityPerformanceProfiler, UnityPerformanceProfiler>();
            services.AddTransient<IUnityBuildOptimizer, UnityBuildOptimizer>();
            
            // Unity analysis services
            services.AddTransient<IUnityAssetAnalyzer, UnityAssetAnalyzer>();
            services.AddTransient<IUnityScriptAnalyzer, UnityScriptAnalyzer>();
            services.AddTransient<IUnitySceneAnalyzer, UnitySceneAnalyzer>();
            
            // Unity profiler integration
            services.AddTransient<IUnityProfilerAPI, UnityProfilerAPI>();
            services.AddTransient<IPerformanceDataCollector, PerformanceDataCollector>();
            
            // Unity build pipeline
            services.AddTransient<IUnityBuildPipeline, UnityBuildPipeline>();
            services.AddTransient<IPlatformOptimizer, PlatformOptimizer>();
            services.AddTransient<IAssetOptimizer, AssetOptimizer>();
            
            // Game-specific AI agents
            services.AddTransient<GameplayBalanceAgent>();
            services.AddTransient<GameMechanicsGenerationAgent>();
            services.AddTransient<UnityOptimizationAgent>();
            
            // Game development workflows
            services.AddTransient<GameDevelopmentWorkflow>();
            services.AddTransient<GameTestingWorkflow>();
            
            // Game testing services
            services.AddTransient<IUnityTestRunner, UnityTestRunner>();
            services.AddTransient<IGameplayTester, GameplayTester>();
            services.AddTransient<IPerformanceTester, PerformanceTester>();
            services.AddTransient<IBalanceTester, BalanceTester>();
            
            // Game performance monitoring
            services.AddTransient<IGamePerformanceMonitor, GamePerformanceMonitor>();
            services.AddTransient<IUnityProfilerIntegration, UnityProfilerIntegration>();
            services.AddTransient<IPerformanceAnalyzer, PerformanceAnalyzer>();
            
            // Gameplay analysis services
            services.AddTransient<IGameplayAnalyzer, GameplayAnalyzer>();
            services.AddTransient<IBalanceCalculator, BalanceCalculator>();
            
            // Unity code generation
            services.AddTransient<IUnityCodeGenerator, UnityCodeGenerator>();
            
            return services;
        }
        // This class acts as an orchestrator for various Unity dependency injection functionalities,
        // with specific categories defined in partial classes.
    }
}
