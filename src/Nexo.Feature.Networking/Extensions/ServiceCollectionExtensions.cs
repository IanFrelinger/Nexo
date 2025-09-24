using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nexo.Agent.Contracts;
using Nexo.Feature.Networking.Interfaces;
using Nexo.Feature.Networking.Providers;
using Nexo.Feature.Networking.Services;
using Nexo.Feature.Networking.Tools;

namespace Nexo.Feature.Networking.Extensions;

/// <summary>
/// Extension methods for registering networking services
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds networking services to the dependency injection container
    /// </summary>
    public static IServiceCollection AddNetworkingServices(this IServiceCollection services)
    {
        // Register providers
        services.AddSingleton<INetworkingProvider, OllamaNetworkingProvider>();
        
        // Register orchestrator
        services.AddSingleton<NetworkingOrchestrator>();
        
        // Register agent tool
        services.AddTransient<ITool, NetworkingTool>();
        
        return services;
    }

    /// <summary>
    /// Adds offline-only networking services
    /// </summary>
    public static IServiceCollection AddOfflineNetworking(this IServiceCollection services)
    {
        // Register only offline providers
        services.AddSingleton<INetworkingProvider, OllamaNetworkingProvider>();
        
        // Register orchestrator
        services.AddSingleton<NetworkingOrchestrator>();
        
        // Register agent tool
        services.AddTransient<ITool, NetworkingTool>();
        
        return services;
    }
}

