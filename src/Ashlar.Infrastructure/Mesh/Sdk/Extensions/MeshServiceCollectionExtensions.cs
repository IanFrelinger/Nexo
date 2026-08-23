using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Ashlar.Core.Application.Mesh.Models;
using Ashlar.Core.Application.Mesh.Ports;
using Ashlar.Infrastructure.Mesh;

namespace Ashlar.Infrastructure.Mesh.Sdk.Extensions;
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
    /// <param name="instancesPath">Path to instances.json. If null, uses ~/.ashlar/instances.json.</param>
    /// <param name="peerId">This instance's peer ID. If null, uses ASHLAR_MESH_PEER_ID env or new GUID.</param>
    public static IServiceCollection AddMeshInfrastructure(this IServiceCollection services, string? instancesPath = null, string? peerId = null)
    {
        var path = instancesPath ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".ashlar", "instances.json");
        var meshBasePath = Path.Combine(Path.GetDirectoryName(path) ?? path, "mesh");
        var resolvedPeerId = peerId ?? Environment.GetEnvironmentVariable("ASHLAR_MESH_PEER_ID") ?? Guid.NewGuid().ToString("N");
        var trustedPeerIdsCsv = Environment.GetEnvironmentVariable("ASHLAR_TRUSTED_PEER_IDS");
        var untrustedPeerIdsCsv = Environment.GetEnvironmentVariable("ASHLAR_UNTRUSTED_PEER_IDS");
        var meshTrustPolicy = Ashlar.Core.Application.Mesh.MeshTrustPolicyConfiguration.ResolveDiscoveryPolicy();
        var capabilityTrustPolicy = Ashlar.Core.Application.Mesh.MeshTrustPolicyConfiguration.ResolveCapabilityRequestPolicy();

        services.AddSingleton(Options.Create(new MeshOptions
        {
            PeerId = resolvedPeerId,
            TrustedPeerIdsCsv = trustedPeerIdsCsv,
            UntrustedPeerIdsCsv = untrustedPeerIdsCsv
        }));
        services.AddSingleton<IInstanceDiscovery>(sp =>
            new FileBasedInstanceDiscovery(path, trustedPeerIdsCsv, untrustedPeerIdsCsv, meshTrustPolicy));
        services.AddSingleton<ICapabilityAdvertisement>(sp =>
            new FileBasedCapabilityAdvertisement(
                sp.GetRequiredService<IInstanceDiscovery>(),
                path,
                resolvedPeerId,
                trustedPeerIdsCsv,
                untrustedPeerIdsCsv));
        services.AddSingleton<ILocalTransport>(sp => new FileBasedLocalTransport(meshBasePath, resolvedPeerId));
        services.AddSingleton<ICapabilityRequester>(sp =>
        {
            var adv = sp.GetRequiredService<ICapabilityAdvertisement>();
            var transport = sp.GetRequiredService<ILocalTransport>();
            var options = sp.GetRequiredService<IOptions<MeshOptions>>();
            var logger = sp.GetService<ILogger<MeshCapabilityRequester>>();
            return new MeshCapabilityRequester(
                adv,
                transport,
                options.Value.PeerId,
                capabilityTrustPolicy,
                trustedPeerIdsCsv,
                untrustedPeerIdsCsv,
                logger);
        });
        services.AddSingleton<ICapabilityFulfiller>(sp =>
        {
            var transport = sp.GetRequiredService<ILocalTransport>();
            var logger = sp.GetService<ILogger<MeshCapabilityFulfiller>>();
            return new MeshCapabilityFulfiller(transport, logger);
        });
        services.AddSingleton<IArtifactNegotiator, ArtifactNegotiator>();
        services.AddSingleton<IInstanceCapabilitiesProvider, LocalAshlarInstanceCapabilitiesProvider>();
        return services;
    }
}
