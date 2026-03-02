using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Mesh.Ports;
using Nexo.Infrastructure.Mesh;

namespace Nexo.Infrastructure;

/// <summary>
/// DI extensions for Block 9 mesh.
/// </summary>
public static class MeshServiceCollectionExtensions
{
    /// <summary>
    /// Adds instance discovery, capability advertisement, file-based transport, and capability requester.
    /// Transport uses a shared directory for peer inboxes; requester discovers peers and sends requests via transport.
    /// </summary>
    public static IServiceCollection AddMeshInfrastructure(this IServiceCollection services, string? instancesPath = null)
    {
        var path = instancesPath ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".nexo", "instances.json");
        var meshBasePath = Path.Combine(Path.GetDirectoryName(path) ?? path, "mesh");
        var peerId = Environment.GetEnvironmentVariable("NEXO_MESH_PEER_ID") ?? Guid.NewGuid().ToString("N");

        services.AddSingleton<IInstanceDiscovery>(sp => new FileBasedInstanceDiscovery(path));
        services.AddSingleton<ICapabilityAdvertisement>(sp =>
            new FileBasedCapabilityAdvertisement(sp.GetRequiredService<IInstanceDiscovery>(), path, peerId));
        services.AddSingleton<ILocalTransport>(sp => new FileBasedLocalTransport(meshBasePath, peerId));
        services.AddSingleton<ICapabilityRequester>(sp =>
        {
            var adv = sp.GetRequiredService<ICapabilityAdvertisement>();
            var transport = sp.GetRequiredService<ILocalTransport>();
            var logger = sp.GetService<ILogger<MeshCapabilityRequester>>();
            return new MeshCapabilityRequester(adv, transport, logger);
        });
        return services;
    }
}
