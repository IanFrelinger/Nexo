using Microsoft.Extensions.DependencyInjection;
using Nexo.Feature.Analysis.Interfaces;
using Nexo.Feature.Analysis.Services;

namespace Nexo.Feature.Analysis
{
    /// <summary>
    /// Dependency injection configuration for the Analysis feature module.
    /// </summary>
    public static class DependencyInjection
    {
        /// <summary>
        /// Adds Analysis feature services to the service collection.
        /// </summary>
        /// <param name="services">The service collection</param>
        /// <returns>The service collection for chaining</returns>
        public static IServiceCollection AddAnalysisFeature(this IServiceCollection services)
        {
            // Core analysis services
            services.AddScoped<ICodeAnalyzer, CodeAnalyzerService>();
            services.AddScoped<IArchitectureAnalyzer, ArchitectureAnalyzerService>();
            services.AddScoped<IAIEnhancedAnalyzerService, AIEnhancedAnalyzerService>();

            // Coding standards services
            services.AddScoped<ICodingStandardAnalyzer, ConfigurableCodingStandardAnalyzer>();
            services.AddScoped<ICodingStandardConfigurationService, CodingStandardConfigurationService>();
            services.AddScoped<AgentCodingStandardsIntegrationService>();

            return services;
        }
    }
}
