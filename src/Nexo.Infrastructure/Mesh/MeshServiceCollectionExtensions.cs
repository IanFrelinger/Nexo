using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nexo.Core.Application.Mesh.Models;
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
    /// <param name="services">Service collection.</param>
    /// <param name="instancesPath">Path to instances.json. If null, uses ~/.nexo/instances.json.</param>
    /// <param name="peerId">This instance's peer ID. If null, uses NEXO_MESH_PEER_ID env or new GUID.</param>
    public static IServiceCollection AddMeshInfrastructure(this IServiceCollection services, string? instancesPath = null, string? peerId = null)
    {
        var path = instancesPath ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".nexo", "instances.json");
        var meshBasePath = Path.Combine(Path.GetDirectoryName(path) ?? path, "mesh");
        var resolvedPeerId = peerId ?? Environment.GetEnvironmentVariable("NEXO_MESH_PEER_ID") ?? Guid.NewGuid().ToString("N");

        services.AddSingleton(Options.Create(new MeshOptions { PeerId = resolvedPeerId }));
        services.AddSingleton<IInstanceDiscovery>(sp => new FileBasedInstanceDiscovery(path));
        services.AddSingleton<ICapabilityAdvertisement>(sp =>
            new FileBasedCapabilityAdvertisement(sp.GetRequiredService<IInstanceDiscovery>(), path, resolvedPeerId));
        services.AddSingleton<ILocalTransport>(sp => new FileBasedLocalTransport(meshBasePath, resolvedPeerId));
        services.AddSingleton<ICapabilityRequester>(sp =>
        {
            var adv = sp.GetRequiredService<ICapabilityAdvertisement>();
            var transport = sp.GetRequiredService<ILocalTransport>();
            var options = sp.GetRequiredService<IOptions<MeshOptions>>();
            var logger = sp.GetService<ILogger<MeshCapabilityRequester>>();
            return new MeshCapabilityRequester(adv, transport, options.Value.PeerId, logger);
        });
        services.AddSingleton<ICapabilityFulfiller>(sp =>
        {
            var transport = sp.GetRequiredService<ILocalTransport>();
            var logger = sp.GetService<ILogger<MeshCapabilityFulfiller>>();
            return new MeshCapabilityFulfiller(transport, logger);
        });
        return services;
    }
}
