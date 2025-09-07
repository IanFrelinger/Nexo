using Microsoft.Extensions.DependencyInjection;
using Nexo.Feature.Factory.Application.Interfaces;
using Nexo.Feature.Factory.Application.Services;
using Nexo.Feature.Factory.Application.Agents;

namespace Nexo.Feature.Factory
{
    /// <summary>
    /// Dependency injection configuration for the Feature Factory module.
    /// </summary>
    public static class DependencyInjection
    {
        /// <summary>
        /// Adds Feature Factory services to the service collection.
        /// </summary>
        /// <param name="services">The service collection</param>
        /// <returns>The service collection for chaining</returns>
        public static IServiceCollection AddFeatureFactory(this IServiceCollection services)
        {
            // Core services
            services.AddScoped<IFeatureOrchestrator, FeatureOrchestrator>();
            services.AddScoped<IAgentCoordinator, AgentCoordinator>();
            services.AddScoped<IDecisionEngine, DecisionEngine>();
            
            // AI Agents
            services.AddScoped<DomainAnalysisAgent>();
            services.AddScoped<CodeGenerationAgent>();
            
            return services;
        }
        
        /// <summary>
        /// Configures and initializes the Feature Factory agents.
        /// </summary>
        /// <param name="serviceProvider">The service provider</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Task representing the initialization</returns>
        public static async Task InitializeFeatureFactoryAgentsAsync(this IServiceProvider serviceProvider, CancellationToken cancellationToken = default)
        {
            var agentCoordinator = serviceProvider.GetRequiredService<IAgentCoordinator>();
            
            // Register domain analysis agent
            var domainAnalysisAgent = serviceProvider.GetRequiredService<DomainAnalysisAgent>();
            await agentCoordinator.RegisterAgentAsync(domainAnalysisAgent, cancellationToken);
            
            // Register code generation agent
            var codeGenerationAgent = serviceProvider.GetRequiredService<CodeGenerationAgent>();
            await agentCoordinator.RegisterAgentAsync(codeGenerationAgent, cancellationToken);
        }
    }
}
