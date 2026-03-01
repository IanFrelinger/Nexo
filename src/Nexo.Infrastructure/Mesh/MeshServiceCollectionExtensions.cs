using Microsoft.Extensions.DependencyInjection;
using Nexo.Core.Application.Mesh.Ports;
using Nexo.Infrastructure.Mesh;

namespace Nexo.Infrastructure;

/// <summary>
/// DI extensions for Block 9 mesh.
/// </summary>
public static class MeshServiceCollectionExtensions
{
    /// <summary>
    /// Adds instance discovery, capability advertisement, transport, requester.
    /// </summary>
    public static IServiceCollection AddMeshInfrastructure(this IServiceCollection services, string? instancesPath = null)
    {
        var path = instancesPath ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".nexo", "instances.json");
        services.AddSingleton<IInstanceDiscovery>(sp => new FileBasedInstanceDiscovery(path));
        services.AddSingleton<ICapabilityAdvertisement>(sp => new FileBasedCapabilityAdvertisement(sp.GetRequiredService<IInstanceDiscovery>(), path));
        services.AddSingleton<ILocalTransport, StubLocalTransport>();
        services.AddSingleton<ICapabilityRequester, StubCapabilityRequester>();
        return services;
    }
}
