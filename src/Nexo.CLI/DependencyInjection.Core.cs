using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nexo.Observability;
using Nexo.Core.Application.Interfaces;
using Nexo.Core.Application.Models;
using Nexo.Core.Application.Services;
using Nexo.Feature.AI.Interfaces;
using Nexo.Feature.AI.Services;
using Nexo.Feature.Platform.Interfaces;
using Nexo.Feature.Platform.Services;
using Nexo.Feature.Analysis.Interfaces;
using Nexo.Feature.Analysis.Services;
using Nexo.Feature.Agent.Interfaces;
using Nexo.Feature.Agent.Services;
using Nexo.Feature.Template.Interfaces;
using Nexo.Feature.Template.Services;
using Nexo.Infrastructure.Services;
using Nexo.Infrastructure.Services.AI;
using Nexo.Infrastructure.Services.Caching;
using Nexo.Shared;
using Nexo.Shared.Models;
using Nexo.Shared.Services;
using Nexo.Shared.Interfaces;
using Nexo.Feature.Pipeline.Interfaces;
using Nexo.Feature.Pipeline.Models;
using Nexo.Feature.Pipeline.Services;
using Nexo.Infrastructure.Services.Resource;
using Nexo.Shared.Interfaces.Resource;
using Nexo.Feature.Factory;
using Nexo.Feature.Unity;
using Nexo.Core.Application.Services.Adaptation;
using Nexo.Core.Extensions;
using System;
using System.Net.Http;
using System.Collections.Generic;
using System.Threading;

namespace Nexo.CLI
{
    /// <summary>
    /// Core dependency injection configuration for the Nexo CLI application
    /// </summary>
    public static partial class DependencyInjection
    {
        /// <summary>
        /// Configures the service collection with all required dependencies.
        /// </summary>
        /// <param name="services">The service collection to configure.</param>
        /// <param name="configuration">The configuration instance.</param>
        /// <returns>The configured service collection.</returns>
        public static IServiceCollection AddNexoCliServices(this IServiceCollection services, IConfiguration configuration)
        {
            // Add configuration
            services.AddSingleton<IConfiguration>(configuration);
            
            // Add logging
            services.AddLogging(builder =>
            {
                builder.AddConsole();
                builder.AddDebug();
                builder.SetMinimumLevel(LogLevel.Information);
            });

            // Add observability
            services.AddNexoObservability(configuration);

            // Add HTTP client
            services.AddHttpClient();

            // Add cache settings
            services.Configure<Nexo.Shared.Models.CacheSettings>(configuration.GetSection("Cache"));
            services.AddSingleton<Nexo.Shared.Models.CacheSettings>(provider =>
            {
                var config = provider.GetRequiredService<IConfiguration>();
                var cacheSection = config.GetSection("Cache");
                
                var cacheSettings = new Nexo.Shared.Models.CacheSettings
                {
                    Backend = cacheSection["Backend"] ?? Environment.GetEnvironmentVariable(Constants.EnvironmentVariables.CacheBackend) ?? Constants.Defaults.DefaultCacheBackend,
                    RedisConnectionString = cacheSection["RedisConnectionString"] ?? Environment.GetEnvironmentVariable(Constants.EnvironmentVariables.RedisConnectionString) ?? Constants.Cache.DefaultRedisConnectionString,
                    RedisKeyPrefix = cacheSection["RedisKeyPrefix"] ?? Environment.GetEnvironmentVariable(Constants.EnvironmentVariables.RedisKeyPrefix) ?? Constants.Cache.DefaultRedisKeyPrefix,
                    DefaultTtlSeconds = int.TryParse(cacheSection["DefaultTtlSeconds"], out var configTtl) ? configTtl : 
                                      (int.TryParse(Environment.GetEnvironmentVariable(Constants.EnvironmentVariables.CacheTtlSeconds), out var envTtl) ? envTtl : Constants.Cache.DefaultTtlSeconds)
                };
                return cacheSettings;
            });

            // Add AI settings
            services.AddSingleton<Nexo.Feature.AI.Models.AiSettings>(provider =>
            {
                return new Nexo.Feature.AI.Models.AiSettings
                {
                    PreferredProvider = Environment.GetEnvironmentVariable(Constants.EnvironmentVariables.AiProvider) ?? string.Empty,
                    PreferredModel = Environment.GetEnvironmentVariable(Constants.EnvironmentVariables.AiModel) ?? string.Empty
                };
            });

            // Add cache services
            services.AddSingleton<Nexo.Infrastructure.Services.Caching.CacheConfigurationService>();
            services.AddSingleton<Nexo.Core.Application.Interfaces.ICacheStrategy<string, Nexo.Feature.AI.Models.ModelResponse>>(provider =>
            {
                var cacheSettings = provider.GetRequiredService<Nexo.Shared.Models.CacheSettings>();
                var cacheConfigService = provider.GetRequiredService<Nexo.Infrastructure.Services.Caching.CacheConfigurationService>();
                return cacheConfigService.CreateCacheStrategy<string, Nexo.Feature.AI.Models.ModelResponse>();
            });

            // Add AI services
            services.AddSingleton<IModelOrchestrator, ModelOrchestrator>();
            services.AddSingleton<OpenAiModelProvider>();
            services.AddSingleton<OllamaModelProvider>();
            services.AddSingleton<AzureOpenAiModelProvider>();
            services.AddSingleton<IAiConfigurationService, AiConfigurationService>();

            // Add Natural Language Processing services
            services.AddTransient<INaturalLanguageProcessor, NaturalLanguageProcessor>();

            // Add Feature Requirements Extraction services (Phase 5.1.2)
            services.AddTransient<IFeatureRequirementsExtractor, FeatureRequirementsExtractor>();

            // Add Domain Context Processing services
            services.AddTransient<IDomainContextProcessor, DomainContextProcessor>();

            // Add Domain Logic Generation services
            services.AddTransient<IDomainLogicGenerator, DomainLogicGenerator>();

            // Add Application Logic Standardization services (Phase 5.3)
            services.AddTransient<IApplicationLogicStandardizer, ApplicationLogicStandardizer>();

            // Add Platform-Specific Implementation services (Phase 6.1)
            services.AddTransient<IIOSCodeGenerator, IOSCodeGenerator>();
            services.AddTransient<IAndroidCodeGenerator, AndroidCodeGenerator>();

            // Add AI-enhanced services for Phase 2
            services.AddTransient<IAIEnhancedAnalyzerService, AIEnhancedAnalyzerService>();
            services.AddTransient<IDevelopmentAccelerator, DevelopmentAccelerator>();
            services.AddTransient<ITemplateService, TemplateService>();
            services.AddTransient<IIntelligentTemplateService, IntelligentTemplateService>();

            // Add Analysis feature services including coding standards
            services.AddAnalysisFeature();

            // Add Phase 4: Development Acceleration services
            services.AddTransient<IAdvancedCachingService, AdvancedCachingService>();
            services.AddTransient<IParallelProcessingOptimizer, ParallelProcessingOptimizer>();
            services.AddTransient<ISemanticCacheKeyGenerator, SemanticCacheKeyGeneratorService>();

            // Add AI-enhanced agents
            services.AddTransient<IAiEnhancedAgent, AiEnhancedArchitectAgent>();
            services.AddTransient<IAiEnhancedAgent, AiEnhancedDeveloperAgent>();

            // Add performance monitoring
            services.AddSingleton<PerformanceMonitor>();

            // Add pipeline services
            services.AddTransient<IPipelineConfiguration, StubPipelineConfiguration>();
            services.AddTransient<IPipelineConfigurationService, PipelineConfigurationService>();
            services.AddTransient<IPipelineExecutionEngine, PipelineExecutionEngine>();
            services.AddTransient<IPipelineContext>(provider =>
            {
                var logger = provider.GetRequiredService<ILogger<PipelineContext>>();
                var configuration = provider.GetRequiredService<IPipelineConfiguration>();
                return new PipelineContext(logger, configuration, System.Threading.CancellationToken.None);
            });

            // Add workflow services
            services.AddTransient<IWorkflowConfigurationService, WorkflowConfigurationService>();
            services.AddTransient<IWorkflowExecutionService, WorkflowExecutionService>();

            // Add Web feature services
            services.AddTransient<Nexo.Feature.Web.Interfaces.IWebCodeGenerator, Nexo.Feature.Web.Services.WebCodeGenerator>();
            services.AddTransient<Nexo.Feature.Web.Interfaces.IWebAssemblyOptimizer, Nexo.Feature.Web.Services.WebAssemblyOptimizer>();
            services.AddTransient<Nexo.Feature.Web.Interfaces.IFrameworkTemplateProvider, Nexo.Feature.Web.Services.FrameworkTemplateProvider>();
            services.AddTransient<Nexo.Feature.Web.UseCases.GenerateWebCodeUseCase>();

            // Add resource management services
            services.AddSingleton<Nexo.Shared.Interfaces.Resource.IResourceMonitor, SystemResourceMonitor>();
            services.AddSingleton<IResourceOptimizer, ResourceOptimizer>();
            services.AddSingleton<IResourceManager, BasicResourceManager>();

            // Add Feature Factory services
            services.AddFeatureFactory();

            // Add simple test runner services (prevents hanging)
            services.AddTransient<Nexo.CLI.Commands.SimpleTestRunner, Nexo.CLI.Commands.SimpleTestRunner>();
            services.AddTransient<Nexo.Feature.Factory.Testing.Progress.IProgressReporter, Nexo.Feature.Factory.Testing.Progress.ConsoleProgressReporter>();
            services.AddTransient<Nexo.Feature.Factory.Testing.Coverage.ITestCoverageAnalyzer, Nexo.Feature.Factory.Testing.Coverage.ReflectionBasedCoverageAnalyzer>();
            services.AddTransient<Nexo.Feature.Factory.Testing.Timeout.ITimeoutManager, Nexo.Feature.Factory.Testing.Timeout.AggressiveTimeoutManager>();

            // Add Enhanced CLI services
            services.AddEnhancedCLIServices();

            // Add Unity feature services
            services.AddUnityFeature();

            // Add adaptation services
            services.AddAdaptationServices();

            // Add specialized AI agents
            services.AddSpecializedAIAgents();

            // Add iteration strategy services
            services.AddNexoIterationStrategies();

            // Add tool generation services
            services.AddToolGenerationServices();

            // Add policy engine services
            services.AddPolicyEngineServices();

            // Add pipeline services
            services.AddPipelineServices();

            // Add FeatureSpec validation services
            services.AddTransient<FeatureSpecValidator>();

            return services;
        }
    }
}
