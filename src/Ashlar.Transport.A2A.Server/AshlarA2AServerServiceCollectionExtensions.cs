using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Ashlar.Transport.A2A.Server;

/// <summary>
/// DI composition for the A2A server core. Host-composed (never kernel-registered), keeping the
/// preview A2A dependency out of the AddAshlar package graph. The host must additionally register
/// an <see cref="IAshlarA2AAgentCatalog"/> implementation over its agent registry — this project
/// cannot reference the domain model (transport layering rules), so the catalog is the seam.
/// </summary>
public static class AshlarA2AServerServiceCollectionExtensions
{
    /// <summary>Registers options + projector; endpoints are mapped separately via
    /// <see cref="AshlarA2AEndpointRouteBuilderExtensions.MapAshlarA2AEndpoints"/>.</summary>
    public static IServiceCollection AddAshlarA2AServer(
        this IServiceCollection services,
        IConfiguration configuration,
        string sectionPath = AshlarA2AServerOptions.SectionPath)
    {
        services.AddOptions<AshlarA2AServerOptions>()
            .Bind(configuration.GetSection(sectionPath))
            .ValidateOnStart();
        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IValidateOptions<AshlarA2AServerOptions>, ValidateAshlarA2AServerOptions>());

        services.TryAddSingleton<IAshlarA2ACardProjector, AshlarA2ACardProjector>();
        return services;
    }
}
