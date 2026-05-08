using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Nexo.Core.Application.Execution.Routing;
using Nexo.Core.Application.Mesh.Models;
using Nexo.Core.Application.Mesh.Ports;
using Nexo.Core.Application.NodeCapabilityRuntime.Models;
using Nexo.Core.Application.NodeCapabilityRuntime.Ports;
using Nexo.Core.Domain;
using Nexo.Core.Domain.Bricks;
using Nexo.Core.Domain.Execution;
using Nexo.Infrastructure.Execution.Routing;
using Nexo.Infrastructure.NodeCapabilityRuntime;

namespace Nexo.Tests.Infrastructure.Helpers.Ncr;

/// <summary>
/// Spins up a minimal <see cref="IHost"/> that mirrors production wiring for capability routing:
/// real <see cref="NCRCapabilityPoller"/>, <see cref="PeerCapabilitySnapshotPoller"/>,
/// <see cref="NcrCapabilityRouter"/>, <see cref="CapabilityRoutingBrick"/>, and bound <c>Nexo:RunPod</c> /
/// <c>Nexo:NodeCapabilityRuntime</c> configuration — with deterministic substitutes for hardware probing,
/// mesh discovery, queue depth, RunPod HTTP, and local GPU execution.
/// </summary>
public sealed class VirtualProductionNcrRoutingHost : IAsyncDisposable
{
    private VirtualProductionNcrRoutingHost(IHost underlyingHost, SimulatedHardwareProfiler hardware, SimulatedMeshRegistry mesh, SimulatedQueueDepthProvider queue)
    {
        UnderlyingHost = underlyingHost;
        Hardware = hardware;
        Mesh = mesh;
        Queue = queue;
    }

    /// <summary>The generic host running NCR + peer pollers.</summary>
    public IHost UnderlyingHost { get; }

    public SimulatedHardwareProfiler Hardware { get; }

    public SimulatedMeshRegistry Mesh { get; }

    public SimulatedQueueDepthProvider Queue { get; }

    public IServiceProvider Services => UnderlyingHost.Services;

    public static async Task<VirtualProductionNcrRoutingHost> StartAsync(
        Action<VirtualProductionNcrRoutingHostOptions>? configure = null,
        CancellationToken cancellationToken = default)
    {
        var opts = new VirtualProductionNcrRoutingHostOptions();
        configure?.Invoke(opts);

        var staging = Path.Combine(Path.GetTempPath(), $"nexo-vprod-{Guid.NewGuid():N}");
        Directory.CreateDirectory(staging);
        opts.ConfigurationOverrides["Nexo:RunPod:OutputStagingPath"] = staging;

        var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings { DisableDefaults = true });

        builder.Logging.ClearProviders();
        builder.Logging.AddProvider(NullLoggerProvider.Instance);

        foreach (var kv in opts.ConfigurationOverrides)
            builder.Configuration[kv.Key] = kv.Value;

        builder.Services.AddSingleton(opts.RunPodStub);
        builder.Services.AddSingleton(opts.LocalExecutor);

        builder.Services.AddSingleton<IBrickRegistry, StubBrickRegistry>();

        // Production capability routing (pollers + router + bricks).
        builder.Services.AddSingleton<IHardwareProfiler>(opts.HardwareProfiler);
        builder.Services.AddSingleton<ILocalQueueDepthProvider>(opts.QueueDepthProvider);
        builder.Services.AddSingleton<IInstanceDiscovery>(opts.MeshRegistry);

        builder.Services.AddOptions<NodeCapabilityRuntimeOptions>()
            .Bind(builder.Configuration.GetSection(NodeCapabilityRuntimeOptions.SectionName));

        builder.Services.AddRunPodCapabilityRouting(builder.Configuration);

        builder.Services.RemoveAll<IRunPodClient>();
        builder.Services.AddSingleton<IRunPodClient>(sp => sp.GetRequiredService<StubRunPodClient>());

        builder.Services.RemoveAll<ILocalExecutor>();
        builder.Services.AddSingleton<ILocalExecutor>(sp => sp.GetRequiredService<StubLocalExecutor>());

        var host = builder.Build();
        await host.StartAsync(cancellationToken).ConfigureAwait(false);

        // Give hosted background loops their first tick (NCR + peer mesh refresh).
        await Task.Delay(opts.PostStartDelay, cancellationToken).ConfigureAwait(false);

        return new VirtualProductionNcrRoutingHost(host, opts.HardwareProfiler, opts.MeshRegistry, opts.QueueDepthProvider);
    }

    public ICapabilityRouter GetRouter() => UnderlyingHost.Services.GetRequiredService<ICapabilityRouter>();

    public INCRCapabilitySnapshot GetNcrSnapshot() => UnderlyingHost.Services.GetRequiredService<INCRCapabilitySnapshot>();

    public IPeerCapabilitySnapshot GetPeerSnapshot() => UnderlyingHost.Services.GetRequiredService<IPeerCapabilitySnapshot>();

    public CapabilityRoutingBrick GetCapabilityRoutingBrick() => UnderlyingHost.Services.GetRequiredService<CapabilityRoutingBrick>();

    public async ValueTask DisposeAsync()
    {
        await UnderlyingHost.StopAsync().ConfigureAwait(false);
        UnderlyingHost.Dispose();
    }
}

/// <summary>Mutable knobs for the virtual production routing host.</summary>
public sealed class VirtualProductionNcrRoutingHostOptions
{
    /// <summary>Defaults tuned for fast, deterministic CI (still uses real poller classes).</summary>
    public Dictionary<string, string?> ConfigurationOverrides { get; } = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Nexo:RunPod:QueueDepthThreshold"] = "8",
        ["Nexo:RunPod:Timeout"] = "00:01:00",
        ["Nexo:RunPod:PollingInterval"] = "00:00:00.050",
        ["Nexo:RunPod:EnablePeerNetworkRouting"] = "true",
        ["Nexo:RunPod:PreferPeerNetworkOverCloud"] = "true",
        ["Nexo:RunPod:PeerTrustPolicy"] = "any",
        ["Nexo:RunPod:PeerDiscoveryInterval"] = "00:00:00.150",
        ["Nexo:RunPod:PeerCapabilityId"] = "generation.capability-routing",
        ["Nexo:NodeCapabilityRuntime:ProfileRefreshInterval"] = "00:00:00.150",
        ["Nexo:NodeCapabilityRuntime:NodeId"] = "virtual-prod-node"
    };

    public TimeSpan PostStartDelay { get; set; } = TimeSpan.FromMilliseconds(250);

    public SimulatedHardwareProfiler HardwareProfiler { get; } = new();

    public SimulatedMeshRegistry MeshRegistry { get; } = new();

    public SimulatedQueueDepthProvider QueueDepthProvider { get; } = new();

    public StubRunPodClient RunPodStub { get; } = new();

    public StubLocalExecutor LocalExecutor { get; } = new()
    {
        Handler = _ => Result<GenerationExecutionResult>.Success(new GenerationExecutionResult
        {
            Payload = [1],
            Provider = "virtual-local",
            ModelId = "m",
            IsRemote = false,
            Summary = "virtual prod local"
        })
    };
}

/// <summary>Deterministic stand-in for OS / driver hardware probing.</summary>
public sealed class SimulatedHardwareProfiler : IHardwareProfiler
{
    public NodeProfile Profile { get; set; } = new()
    {
        AvailableVRAMBytes = 64L * 1024 * 1024 * 1024,
        TotalVRAMBytes = 64L * 1024 * 1024 * 1024,
        Tier = NodeTier.Core,
        CapturedAt = DateTimeOffset.UtcNow
    };

    public Task<NodeProfile> CaptureAsync(CancellationToken ct = default)
        => Task.FromResult(Profile with { CapturedAt = DateTimeOffset.UtcNow });
}

/// <summary>In-memory mesh peer registry — mutate between polls to simulate topology churn.</summary>
public sealed class SimulatedMeshRegistry : IInstanceDiscovery
{
    private readonly object _gate = new();
    private PeerInfo[] _peers = [];

    public void SetPeers(IEnumerable<PeerInfo> peers)
    {
        var next = peers.ToArray();
        lock (_gate)
            _peers = next;
    }

    public Task<IReadOnlyList<PeerInfo>> DiscoverAsync(CancellationToken cancellationToken = default)
    {
        lock (_gate)
            return Task.FromResult<IReadOnlyList<PeerInfo>>(_peers.ToArray());
    }
}

/// <summary>Mutable local inference queue depth (mirrors <see cref="EnvironmentQueueDepthProvider"/> role).</summary>
public sealed class SimulatedQueueDepthProvider : ILocalQueueDepthProvider
{
    public int CurrentDepth { get; set; }
}

public sealed class StubRunPodClient : IRunPodClient
{
    public Result<RunPodInstance> SpinUpResult { get; set; } =
        Result<RunPodInstance>.Failure("stub.spinup", "not configured");

    public Result<JobHandle> DispatchResult { get; set; } =
        Result<JobHandle>.Failure("stub.dispatch", "not configured");

    public Queue<JobStatus> StatusSequence { get; set; } = new();

    public Result<byte[]> PullResult { get; set; } =
        Result<byte[]>.Failure("stub.pull", "not configured");

    public Result<Unit> TerminateResult { get; set; } = Result<Unit>.Success(Unit.Value);

    public Task<Result<RunPodInstance>> SpinUpInstance(string modelId, string gpuType, CancellationToken cancellationToken = default)
        => Task.FromResult(SpinUpResult);

    public Task<Result<JobHandle>> DispatchJob(string instanceId, RunPodJobPayload payload, CancellationToken cancellationToken = default)
        => Task.FromResult(DispatchResult);

    public Task<Result<JobStatus>> PollJobStatus(JobHandle jobHandle, CancellationToken cancellationToken = default)
    {
        if (StatusSequence.Count > 0)
            return Task.FromResult(Result<JobStatus>.Success(StatusSequence.Dequeue()));
        return Task.FromResult(Result<JobStatus>.Success(new JobStatus { State = RunPodJobState.Completed }));
    }

    public Task<Result<byte[]>> PullResults(JobHandle jobHandle, CancellationToken cancellationToken = default)
        => Task.FromResult(PullResult);

    public Task<Result<Unit>> TerminateInstance(string instanceId, CancellationToken cancellationToken = default)
        => Task.FromResult(TerminateResult);
}

public sealed class StubLocalExecutor : ILocalExecutor
{
    public Func<RunPodJobPayload, Result<GenerationExecutionResult>> Handler { get; set; } =
        _ => Result<GenerationExecutionResult>.Failure("stub.local", "not configured");

    public Task<Result<GenerationExecutionResult>> ExecuteAsync(
        RunPodJobPayload payload,
        JobRequirements requirements,
        IExecutionContext context,
        CancellationToken cancellationToken = default)
        => Task.FromResult(Handler(payload));
}

public sealed class VirtualProdExecutionContext : IExecutionContext
{
    public string AgentId { get; init; } = "virtual-prod-agent";
    public string BehaviorId { get; init; } = "virtual-prod-behavior";
    public bool IsAirGapped { get; init; }
    public bool AuditMode { get; init; } = true;
    public string Provider { get; init; } = "virtual-prod";
    public IReadOnlyDictionary<string, object> Variables { get; init; } = new Dictionary<string, object>();
}

/// <summary>Composite brick resolution is registered by capability routing; minimal no-op for isolated host.</summary>
internal sealed class StubBrickRegistry : IBrickRegistry
{
    public Brick? GetBrick(string id) => null;
    public IReadOnlyList<Brick> GetAllBricks() => Array.Empty<Brick>();
}
