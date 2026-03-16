using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Nexo.Abstractions.Barriers;
using Nexo.Abstractions.Routing;
using Nexo.Abstractions.Transport;
using Nexo.Runtime.Barriers;
using Nexo.Runtime.Routing;
using Nexo.Runtime.Transport;

namespace Nexo.Runtime;

/// <summary>
/// Runtime-layer DI helpers for transport routing.
/// </summary>
public static class RuntimeServiceCollectionExtensions
{
    /// <summary>
    /// Registers routing transport composition using explicitly-provided local and remote transport types.
    /// Uses TryAdd so host applications can fully override registration.
    /// </summary>
    public static IServiceCollection AddNexoRuntimeTransport<TInProcessTransport, TRemoteTransport>(
        this IServiceCollection services)
        where TInProcessTransport : class, IAgentTransport
        where TRemoteTransport : class, IAgentTransport
    {
        services.TryAddSingleton<TInProcessTransport>();
        services.TryAddSingleton<TRemoteTransport>();
        services.TryAddSingleton<IAgentTransport>(sp =>
            new RoutingAgentTransport(
                sp.GetRequiredService<TInProcessTransport>(),
                sp.GetRequiredService<TRemoteTransport>(),
                sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<RoutingAgentTransport>>()));
        return services;
    }

    /// <summary>
    /// Registers routing registry/options/monitor services from configuration.
    /// </summary>
    public static IServiceCollection AddNexoRuntimeRouting(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<BarrierOptions>()
            .Configure(options => configuration.GetSection("Nexo:Barriers").Bind(options));
        services.AddOptions<RoutingOptions>()
            .Configure(options => configuration.GetSection("Nexo:Routing").Bind(options));

        services.TryAddSingleton<BarrierHierarchy>(sp =>
        {
            var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<BarrierOptions>>().Value;
            return new BarrierHierarchy(options.Levels.Select((name, index) => new BarrierLevel(name, index)));
        });

        services.TryAddScoped<IBarrierContextAccessor, ScopedBarrierContextAccessor>();
        services.TryAddSingleton<IBarrierAuditLog, StructuredBarrierAuditLog>();
        services.TryAddSingleton<IEndpointRegistry, InMemoryEndpointRegistry>();
#if NET8_0_OR_GREATER
        services.AddHostedService<EndpointHealthMonitor>();
#endif
        return services;
    }
}
