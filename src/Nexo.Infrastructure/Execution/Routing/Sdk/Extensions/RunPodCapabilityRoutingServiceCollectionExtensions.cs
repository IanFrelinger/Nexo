using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Execution.Routing;
using Nexo.Core.Application.Mesh;
using Nexo.Core.Application.Mesh.Models;
using Nexo.Core.Application.Mesh.Ports;
using Nexo.Core.Application.NodeCapabilityRuntime.Ports;
using Nexo.Infrastructure.Adaptation;
using Nexo.Infrastructure.Mesh;

namespace Nexo.Infrastructure.Execution.Routing.Sdk;

/// <summary>
/// Registers RunPod + NCR capability-routing execution components.
/// </summary>
public static class RunPodCapabilityRoutingServiceCollectionExtensions
{
    public static IServiceCollection AddRunPodCapabilityRouting(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        if (services is null) throw new ArgumentNullException(nameof(services));
        if (configuration is null) throw new ArgumentNullException(nameof(configuration));

        services.AddOptions<RunPodBrickConfig>()
            .Bind(configuration.GetSection(RunPodBrickConfig.SectionName));

        services.TryAddSingleton<IInstanceDiscovery>(sp =>
        {
            var instancesPath = Environment.GetEnvironmentVariable("NEXO_MESH_INSTANCES_PATH");
            var trustedPeerIdsCsv = Environment.GetEnvironmentVariable("NEXO_TRUSTED_PEER_IDS");
            var untrustedPeerIdsCsv = Environment.GetEnvironmentVariable("NEXO_UNTRUSTED_PEER_IDS");
            return new FileBasedInstanceDiscovery(
                string.IsNullOrWhiteSpace(instancesPath) ? null : instancesPath.Trim(),
                trustedPeerIdsCsv,
                untrustedPeerIdsCsv,
                MeshTrustPolicyConfiguration.ResolveDiscoveryPolicy());
        });

        services.AddHttpClient<IRunPodClient, RunPodHttpClient>((sp, client) =>
        {
            var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<RunPodBrickConfig>>().Value;
            var baseUrl = string.IsNullOrWhiteSpace(options.BaseUrl)
                ? "https://api.runpod.io"
                : options.BaseUrl.TrimEnd('/');
            client.BaseAddress = new Uri(baseUrl + "/", UriKind.Absolute);
        });

        services.TryAddSingleton<ILocalQueueDepthProvider, EnvironmentQueueDepthProvider>();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService, NCRCapabilityPoller>());
        services.TryAddSingleton<INCRCapabilitySnapshot>(sp =>
            sp.GetServices<IHostedService>().OfType<NCRCapabilityPoller>().First());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService, PeerCapabilitySnapshotPoller>());
        services.TryAddSingleton<IPeerCapabilitySnapshot>(sp =>
            sp.GetServices<IHostedService>().OfType<PeerCapabilitySnapshotPoller>().First());
        services.TryAddSingleton<NexoPeerBrickExecutor>();
        services.TryAddSingleton<IPeerExecutor>(sp => sp.GetRequiredService<NexoPeerBrickExecutor>());
        services.TryAddSingleton(sp =>
        {
            var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<RunPodBrickConfig>>().Value;
            return new PeerTrustPolicyResolver(options.PeerTrustPolicy, options.TrustedPeerIdsCsv, options.UntrustedPeerIdsCsv);
        });

        services.TryAddSingleton<ILocalExecutor, ProviderFactoryLocalExecutor>();
        services.TryAddSingleton<RunPodBrick>();
        services.TryAddSingleton<ICapabilityRouter, NcrCapabilityRouter>();
        services.TryAddSingleton<CapabilityRoutingBrick>();

        // Default executor entrypoint is capability-routing brick.
        services.TryAddSingleton<IBrickExecutor>(sp => sp.GetRequiredService<CapabilityRoutingBrick>());

        // Register both routing bricks into adaptation/composite brick registries.
        services.Configure<AdaptationBrickOptions>(options =>
        {
            if (!options.AdditionalBrickTypes.Contains(typeof(RunPodBrick)))
            {
                options.AdditionalBrickTypes.Add(typeof(RunPodBrick));
            }

            if (!options.AdditionalBrickTypes.Contains(typeof(CapabilityRoutingBrick)))
            {
                options.AdditionalBrickTypes.Add(typeof(CapabilityRoutingBrick));
            }
        });

        services.TryAddSingleton<CompositeBrickRegistry>(sp =>
        {
            var local = sp.GetRequiredService<Nexo.Core.Domain.Execution.IBrickRegistry>();
            var remoteCatalogs = sp.GetService<IReadOnlyList<IRemoteBrickCatalog>>() ?? [];
            var clientFactory = sp.GetRequiredService<IHttpClientFactory>();
            var logger = sp.GetService<ILogger<CompositeBrickRegistry>>();
            return new CompositeBrickRegistry(local, remoteCatalogs, clientFactory.CreateClient(), logger);
        });

        return services;
    }
}
