using Microsoft.Extensions.DependencyInjection;
using Nexo.Core.Application.Composition.Ports;
using Nexo.Core.Application.ParallelTesting.Ports;
using Nexo.Infrastructure.Composition;

namespace Nexo.Infrastructure.Composition.Sdk.Extensions;
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

    /// <summary>
    /// Adds IComposedTestRunner. Requires AddCompositionInfrastructure and AddParallelTestingInfrastructure.
    /// Phase D: Composition-driven testing.
    /// </summary>
    public static IServiceCollection AddComposedTestRunner(this IServiceCollection services)
    {
        services.AddSingleton<IComposedTestRunner, ComposedTestRunner>();
        return services;
    }
}
