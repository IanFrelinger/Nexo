using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Nexo.Transport.A2A.Server;

/// <summary>
/// DI composition for the A2A server core. Host-composed (never kernel-registered), keeping the
/// preview A2A dependency out of the AddNexo package graph. The host must additionally register
/// an <see cref="INexoA2AAgentCatalog"/> implementation over its agent registry — this project
/// cannot reference the domain model (transport layering rules), so the catalog is the seam.
/// </summary>
public static class NexoA2AServerServiceCollectionExtensions
{
    /// <summary>Registers options + projector; endpoints are mapped separately via
    /// <see cref="NexoA2AEndpointRouteBuilderExtensions.MapNexoA2AEndpoints"/>.</summary>
    public static IServiceCollection AddNexoA2AServer(
        this IServiceCollection services,
        IConfiguration configuration,
        string sectionPath = NexoA2AServerOptions.SectionPath)
    {
        services.AddOptions<NexoA2AServerOptions>()
            .Bind(configuration.GetSection(sectionPath))
            .ValidateOnStart();
        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IValidateOptions<NexoA2AServerOptions>, ValidateNexoA2AServerOptions>());

        services.TryAddSingleton<INexoA2ACardProjector, NexoA2ACardProjector>();
        return services;
    }
}
