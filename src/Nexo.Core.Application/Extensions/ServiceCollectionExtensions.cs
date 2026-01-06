using Microsoft.Extensions.DependencyInjection;
using Nexo.Core.Application.Agents;

namespace Nexo.Core.Application.Extensions;

/// <summary>
/// Extension methods for registering Nexo application services.
/// 
/// Provides dependency injection registration for:
/// - Agent factory and agent implementations
/// - Application layer services
/// 
/// Used during application startup to configure the service container.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds Nexo agent services to the service collection.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddNexoAgents(this IServiceCollection services)
    {
        services.AddSingleton<IAgentFactory, AgentFactory>();
        // services.AddTransient<YourAgent>(); // register concrete agents here
        return services;
    }
}
