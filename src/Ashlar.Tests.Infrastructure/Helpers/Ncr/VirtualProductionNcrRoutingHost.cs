using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Ashlar.Core.Application.Execution.Routing;
using Ashlar.Core.Application.Mesh.Models;
using Ashlar.Core.Application.Mesh.Ports;
using Ashlar.Core.Application.NodeCapabilityRuntime.Ports;
using Ashlar.Core.Domain.Bricks;
using Ashlar.Core.Domain.Execution;
using Ashlar.Infrastructure.Execution;
using Ashlar.Infrastructure.Execution.Routing;
using Ashlar.Infrastructure.Mesh;
using Ashlar.Infrastructure.NodeCapabilityRuntime;
using Ashlar.Infrastructure.NodeCapabilityRuntime.Profiles;

namespace Ashlar.Tests.Infrastructure.Helpers.Ncr;

/// <summary>
/// Spins up a generic host with the **production** RunPod + NCR capability-routing registration:
/// real <see cref="RunPodHttpClient"/> (against <see cref="RunPodLoopbackApiServer"/>),
/// real <see cref="ProviderFactoryLocalExecutor"/> + <see cref="ProviderFactory"/>,
/// real <see cref="EnvironmentHardwareProfiler"/>, <see cref="EnvironmentQueueDepthProvider"/>,
/// <see cref="FileBasedInstanceDiscovery"/> over a temp mesh JSON file, and <see cref="BrickRegistry"/>.
/// </summary>
public sealed class VirtualProductionNcrRoutingHost : IAsyncDisposable
{
    private readonly RunPodLoopbackApiServer _runPodApi;
    private readonly Dictionary<string, string?> _savedEnv = new(StringComparer.Ordinal);

    private VirtualProductionNcrRoutingHost(
        IHost underlyingHost,
        RunPodLoopbackApiServer runPodApi,
        string meshInstancesPath,
        Dictionary<string, string?> savedEnv)
    {
        UnderlyingHost = underlyingHost;
        _runPodApi = runPodApi;
        MeshInstancesPath = meshInstancesPath;
        _savedEnv = savedEnv;
    }

    public IHost UnderlyingHost { get; }

    /// <summary>Temp JSON file passed to <see cref="FileBasedInstanceDiscovery"/>.</summary>
    public string MeshInstancesPath { get; }

    public IServiceProvider Services => UnderlyingHost.Services;

    public static async Task<VirtualProductionNcrRoutingHost> StartAsync(
        Action<VirtualProductionNcrRoutingHostOptions>? configure = null,
        CancellationToken cancellationToken = default)
    {
        var opts = new VirtualProductionNcrRoutingHostOptions();
        configure?.Invoke(opts);

        var savedEnv = ApplyEnvironmentOverrides(opts);

        var staging = Path.Combine(Path.GetTempPath(), $"ashlar-vprod-{Guid.NewGuid():N}");
        Directory.CreateDirectory(staging);
        opts.ConfigurationOverrides["Ashlar:RunPod:OutputStagingPath"] = staging;

        var meshPath = Path.Combine(Path.GetTempPath(), $"ashlar-mesh-{Guid.NewGuid():N}.json");
        File.WriteAllText(meshPath, "[]");

        var runPodApi = new RunPodLoopbackApiServer(opts.RunPodCloud);
        runPodApi.Start();
        opts.ConfigurationOverrides["Ashlar:RunPod:BaseUrl"] = runPodApi.BaseUrl.TrimEnd('/');

        var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings { DisableDefaults = true });

        builder.Logging.ClearProviders();
        builder.Logging.AddProvider(NullLoggerProvider.Instance);

        foreach (var kv in opts.ConfigurationOverrides)
            builder.Configuration[kv.Key] = kv.Value;

        builder.Services.AddSingleton<IBrickRegistry>(_ => new BrickRegistry(Array.Empty<DomainBrick>()));
        builder.Services.AddSingleton<IProviderFactory>(sp =>
            new ProviderFactory(sp.GetRequiredService<ILogger<ProviderFactory>>()));

        builder.Services.AddSingleton<IHardwareProfiler, EnvironmentHardwareProfiler>();
        builder.Services.AddSingleton<ILocalQueueDepthProvider, EnvironmentQueueDepthProvider>();
        builder.Services.AddSingleton<IInstanceDiscovery>(_ =>
            new FileBasedInstanceDiscovery(meshPath));

        builder.Services.AddOptions<NodeCapabilityRuntimeOptions>()
            .Bind(builder.Configuration.GetSection(NodeCapabilityRuntimeOptions.SectionName));

        builder.Services.AddRunPodCapabilityRouting(builder.Configuration);

        var host = builder.Build();
        await host.StartAsync(cancellationToken).ConfigureAwait(false);

        await Task.Delay(opts.PostStartDelay, cancellationToken).ConfigureAwait(false);

        return new VirtualProductionNcrRoutingHost(host, runPodApi, meshPath, savedEnv);
    }

    private static Dictionary<string, string?> ApplyEnvironmentOverrides(VirtualProductionNcrRoutingHostOptions opts)
    {
        var saved = new Dictionary<string, string?>(StringComparer.Ordinal);
        foreach (var kv in opts.EnvironmentOverrides)
        {
            saved[kv.Key] = Environment.GetEnvironmentVariable(kv.Key);
            Environment.SetEnvironmentVariable(kv.Key, kv.Value);
        }

        return saved;
    }

    /// <summary>Writes mesh peers in the format expected by <see cref="FileBasedInstanceDiscovery"/>.</summary>
    public void WriteMeshPeers(IEnumerable<PeerInfo> peers)
    {
        var list = peers.Select(p => new
        {
            peerId = p.PeerId,
            endpoint = p.Endpoint,
            capabilities = p.Capabilities.ToArray(),
            trustTier = p.TrustTier.ToString(),
            admitted = p.Admitted,
            drained = p.Drained
        }).ToArray();

        File.WriteAllText(MeshInstancesPath, JsonSerializer.Serialize(list));
    }

    public ICapabilityRouter GetRouter() => UnderlyingHost.Services.GetRequiredService<ICapabilityRouter>();

    public INCRCapabilitySnapshot GetNcrSnapshot() => UnderlyingHost.Services.GetRequiredService<INCRCapabilitySnapshot>();

    public IPeerCapabilitySnapshot GetPeerSnapshot() => UnderlyingHost.Services.GetRequiredService<IPeerCapabilitySnapshot>();

    public CapabilityRoutingBrick GetCapabilityRoutingBrick() => UnderlyingHost.Services.GetRequiredService<CapabilityRoutingBrick>();

    public async ValueTask DisposeAsync()
    {
        try
        {
            await UnderlyingHost.StopAsync().ConfigureAwait(false);
            UnderlyingHost.Dispose();
            _runPodApi.Dispose();
        }
        finally
        {
            // The overrides are process-wide: restore them even when host teardown throws,
            // or the next test in the collection inherits this test's VRAM figures.
            foreach (var kv in _savedEnv)
            {
                if (kv.Value is null)
                    Environment.SetEnvironmentVariable(kv.Key, null);
                else
                    Environment.SetEnvironmentVariable(kv.Key, kv.Value);
            }
        }
    }
}
