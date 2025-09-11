using Microsoft.Extensions.DependencyInjection;
using Nexo.Core.Application.Services.AI.Analytics;
using Nexo.Core.Application.Services.AI.Caching;
using Nexo.Core.Application.Services.AI.Distributed;
using Nexo.Core.Application.Services.AI.Engines;
using Nexo.Core.Application.Services.AI.ModelFineTuning;
using Nexo.Core.Application.Services.AI.Models;
using Nexo.Core.Application.Services.AI.Monitoring;
using Nexo.Core.Application.Services.AI.Performance;
using Nexo.Core.Application.Services.AI.Pipeline;
using Nexo.Core.Application.Services.AI.Providers;
using Nexo.Core.Application.Services.AI.Rollback;
using Nexo.Core.Application.Services.AI.Runtime;
using Nexo.Core.Application.Services.AI.Safety;
using Nexo.Core.Application.Interfaces.AI;
using Nexo.Core.Application.Interfaces.Services;
using Nexo.Core.Domain.Entities.AI;
using Nexo.Core.Domain.Entities.Pipeline;
using Nexo.Core.Domain.Enums.Code;
using Nexo.Core.Domain.Enums.Safety;
using Nexo.Core.Domain.Results;

namespace Nexo.Core.Application.Extensions
{
    /// <summary>
    /// Extension methods for registering AI services
    /// </summary>
    public static class AIServiceExtensions
    {
        /// <summary>
        /// Adds AI Application services to the dependency injection container
        /// Note: Infrastructure services should be registered in the Infrastructure layer
        /// </summary>
        public static IServiceCollection AddAIServices(this IServiceCollection services)
        {
            // Register Application layer AI services only
            services.AddSingleton<IAIRuntimeSelector, AIRuntimeSelector>();
            services.AddSingleton<IModelManagementService, RealModelManagementService>();
            services.AddSingleton<AIPerformanceMonitor>();

            // Register AI engines (Application layer implementations)
            services.AddTransient<IAIEngine, MockAIEngine>();
            services.AddTransient<IAIEngine, LlamaWebAssemblyEngine>();
            services.AddTransient<IAIEngine, LlamaNativeEngine>();

            // Register AI pipeline steps
            services.AddTransient<IPipelineStep<Nexo.Core.Domain.Entities.AI.CodeGenerationRequest>, AICodeGenerationStep>();
            services.AddTransient<IPipelineStep<Nexo.Core.Domain.Entities.AI.CodeReviewRequest>, AICodeReviewStep>();
            services.AddTransient<IPipelineStep<Nexo.Core.Domain.Entities.AI.CodeOptimizationRequest>, AIOptimizationStep>();
            services.AddTransient<IPipelineStep<Nexo.Core.Domain.Results.DocumentationRequest>, AIDocumentationStep>();
            services.AddTransient<IPipelineStep<Nexo.Core.Domain.Results.TestingRequest>, AITestingStep>();

            // Register Phase 3 services
            services.AddSingleton<AISafetyValidator>();
            services.AddSingleton<AIUsageMonitor>();

            // Register Phase 4 services
            services.AddSingleton<AIModelFineTuner>();
            services.AddSingleton<AIAdvancedAnalytics>();
            services.AddSingleton<AIDistributedProcessor>();
            services.AddSingleton<AIAdvancedCache>();
            services.AddSingleton<AIOperationRollback>();

            return services;
        }

        /// <summary>
        /// Adds AI Application services with custom configuration
        /// Note: Infrastructure services should be registered in the Infrastructure layer
        /// </summary>
        public static IServiceCollection AddAIServices(this IServiceCollection services, Action<AIServiceOptions> configure)
        {
            var options = new AIServiceOptions();
            configure(options);

            // Register Application layer AI services
            services.AddSingleton<IAIRuntimeSelector, AIRuntimeSelector>();
            services.AddSingleton<IModelManagementService, RealModelManagementService>();

            // Register AI engines (Application layer implementations)
            services.AddTransient<IAIEngine, MockAIEngine>();
            services.AddTransient<IAIEngine, LlamaWebAssemblyEngine>();
            services.AddTransient<IAIEngine, LlamaNativeEngine>();

            // Register AI pipeline steps
            services.AddTransient<IPipelineStep<Nexo.Core.Domain.Entities.AI.CodeGenerationRequest>, AICodeGenerationStep>();
            services.AddTransient<IPipelineStep<Nexo.Core.Domain.Entities.AI.CodeReviewRequest>, AICodeReviewStep>();
            services.AddTransient<IPipelineStep<Nexo.Core.Domain.Entities.AI.CodeOptimizationRequest>, AIOptimizationStep>();
            services.AddTransient<IPipelineStep<Nexo.Core.Domain.Results.DocumentationRequest>, AIDocumentationStep>();
            services.AddTransient<IPipelineStep<Nexo.Core.Domain.Results.TestingRequest>, AITestingStep>();

            // Register Phase 3 services
            services.AddSingleton<AISafetyValidator>();
            services.AddSingleton<AIUsageMonitor>();

            // Register Phase 4 services
            services.AddSingleton<AIModelFineTuner>();
            services.AddSingleton<AIAdvancedAnalytics>();
            services.AddSingleton<AIDistributedProcessor>();
            services.AddSingleton<AIAdvancedCache>();
            services.AddSingleton<AIOperationRollback>();

            // Register configuration
            services.AddSingleton(options);

            return services;
        }
    }

    /// <summary>
    /// Configuration options for AI services
    /// </summary>
    public class AIServiceOptions
    {
        public bool EnableMockProvider { get; set; } = true;
        public bool EnableWasmProvider { get; set; } = false;
        public bool EnableNativeProvider { get; set; } = false;
        public bool EnableRemoteProvider { get; set; } = false;
        public bool EnableDockerProvider { get; set; } = false;
        public string DefaultModelId { get; set; } = "codellama-7b";
        public string ModelCachePath { get; set; } = "./models";
        public TimeSpan DefaultTimeout { get; set; } = TimeSpan.FromMinutes(5);
        public int MaxConcurrentOperations { get; set; } = 10;
        public Dictionary<string, object> CustomSettings { get; set; } = new();
    }
}
