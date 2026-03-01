using Microsoft.Extensions.DependencyInjection;
using Nexo.Core.Application.Composition.Ports;
using Nexo.Infrastructure.Composition;

namespace Nexo.Infrastructure;

/// <summary>
/// DI extensions for Block 7 composition.
/// </summary>
public static class CompositionServiceCollectionExtensions
{
    /// <summary>
    /// Adds capability registry, composition engine, and cache.
    /// </summary>
    public static IServiceCollection AddCompositionInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<ICapabilityComponentRegistry, CapabilityComponentRegistry>();
        services.AddSingleton<ICompositionEngine, CompositionEngine>();
        services.AddSingleton<ICompositionCache, InMemoryCompositionCache>();
        return services;
    }
}
