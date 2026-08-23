using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Ashlar.Infrastructure.Execution.LoadPolicy;

namespace Ashlar.Infrastructure.Execution;

/// <summary>
/// Extension methods for registering adaptive load-balancing components.
/// When using AddAshlar(), adaptive load balancing is enabled automatically when ASHLAR_LOAD_PREFERENCE is set.
/// Use this extension when building a custom host that needs ILoadPolicy without AddAshlar.
/// </summary>
public static class AdaptiveProviderFactoryExtensions
{
    /// <summary>
    /// Registers ILoadPolicy (PreferenceLoadPolicy) for edge/server routing.
    /// Reads ASHLAR_LOAD_PREFERENCE (edge|server|auto). Does not register AdaptiveProviderFactory;
    /// use AddAshlar() with UseAdaptiveLoadBalancing for full integration.
    /// </summary>
    public static IServiceCollection AddLoadPolicy(this IServiceCollection services)
    {
        services.TryAddSingleton<ILoadPolicy, PreferenceLoadPolicy>();
        return services;
    }
}
