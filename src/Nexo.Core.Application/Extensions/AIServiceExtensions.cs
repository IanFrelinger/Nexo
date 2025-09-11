using Microsoft.Extensions.DependencyInjection;
using Nexo.Core.Application.Services.AI.Engines;
using Nexo.Core.Application.Services.AI.Models;
using Nexo.Core.Application.Services.AI.Performance;
using Nexo.Core.Application.Services.AI.Pipeline;
using Nexo.Core.Application.Services.AI.Providers;
using Nexo.Core.Application.Services.AI.Runtime;

namespace Nexo.Core.Application.Extensions
{
    /// <summary>
    /// Extension methods for registering AI services
    /// </summary>
    public static class AIServiceExtensions
    {
        /// <summary>
        /// Adds AI services to the dependency injection container
        /// </summary>
        public static IServiceCollection AddAIServices(this IServiceCollection services)
        {
            // Register core AI services
            services.AddSingleton<IAIRuntimeSelector, AIRuntimeSelector>();
            services.AddSingleton<IModelManagementService, RealModelManagementService>();
            services.AddSingleton<AIPerformanceMonitor>();

            // Register AI providers
            services.AddSingleton<IAIProvider, MockAIProvider>();
            services.AddSingleton<IAIProvider, LlamaWebAssemblyProvider>();
            services.AddSingleton<IAIProvider, LlamaNativeProvider>();

            // Register AI engines
            services.AddTransient<IAIEngine, MockAIEngine>();
            services.AddTransient<IAIEngine, LlamaWebAssemblyEngine>();
            services.AddTransient<IAIEngine, LlamaNativeEngine>();

            // Register AI pipeline steps
            services.AddTransient<IPipelineStep<CodeGenerationRequest>, AICodeGenerationStep>();

            return services;
        }

        /// <summary>
        /// Adds AI services with custom configuration
        /// </summary>
        public static IServiceCollection AddAIServices(this IServiceCollection services, Action<AIServiceOptions> configure)
        {
            var options = new AIServiceOptions();
            configure(options);

            // Register core AI services
            services.AddSingleton<IAIRuntimeSelector, AIRuntimeSelector>();
            services.AddSingleton<IModelManagementService, ModelManagementService>();

            // Register AI providers based on configuration
            if (options.EnableMockProvider)
            {
                services.AddSingleton<IAIProvider, MockAIProvider>();
            }

            if (options.EnableWasmProvider)
            {
                services.AddSingleton<IAIProvider, LlamaWebAssemblyProvider>();
            }

            if (options.EnableNativeProvider)
            {
                services.AddSingleton<IAIProvider, LlamaNativeProvider>();
            }

            if (options.EnableRemoteProvider)
            {
                services.AddSingleton<IAIProvider, MockAIProvider>(); // Fallback to mock for now
            }

            // Register AI engines
            services.AddTransient<IAIEngine, MockAIEngine>();

            // Register AI pipeline steps
            services.AddTransient<IPipelineStep<CodeGenerationRequest>, AICodeGenerationStep>();

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
