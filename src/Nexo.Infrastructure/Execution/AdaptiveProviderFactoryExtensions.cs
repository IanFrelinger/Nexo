using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Nexo.Infrastructure.Execution.LoadPolicy;

namespace Nexo.Infrastructure.Execution;

/// <summary>
/// Extension methods for registering adaptive load-balancing components.
/// When using AddNexo(), adaptive load balancing is enabled automatically when NEXO_LOAD_PREFERENCE is set.
/// Use this extension when building a custom host that needs ILoadPolicy without AddNexo.
/// </summary>
public static class AdaptiveProviderFactoryExtensions
{
    /// <summary>
    /// Registers ILoadPolicy (PreferenceLoadPolicy) for edge/server routing.
    /// Reads NEXO_LOAD_PREFERENCE (edge|server|auto). Does not register AdaptiveProviderFactory;
    /// use AddNexo() with UseAdaptiveLoadBalancing for full integration.
    /// </summary>
    public static IServiceCollection AddLoadPolicy(this IServiceCollection services)
    {
        services.TryAddSingleton<ILoadPolicy, PreferenceLoadPolicy>();
        return services;
    }
}
