using FluentAssertions;
using Ashlar.Core.Application.Execution.Routing;
using Ashlar.Core.Application.Mesh.Models;
using Ashlar.Core.Application.NodeCapabilityRuntime.Models;
using Ashlar.Core.Domain;
using Ashlar.Core.Domain.Execution;
using Ashlar.Infrastructure.Execution.Routing;
using Ashlar.Tests.Infrastructure.Helpers.Ncr;
using Xunit;
using Ashlar.Tests.Infrastructure.Helpers;

namespace Ashlar.Tests.Infrastructure.Tests.NodeCapabilityRuntime;

/// <summary>
/// Uses production DI registrations (<see cref="RunPodHttpClient"/>, <see cref="ProviderFactoryLocalExecutor"/>,
/// <see cref="EnvironmentHardwareProfiler"/>, <see cref="FileBasedInstanceDiscovery"/>) inside a generic host.
/// RunPod traffic goes to <see cref="RunPodLoopbackApiServer"/> (same REST paths as cloud API).
/// The host applies process-wide env overrides (ASHLAR_TOTAL_VRAM_BYTES / ASHLAR_AVAILABLE_VRAM_BYTES),
/// so the class runs in the serialized "EnvironmentVariables" collection.
/// </summary>
[Trait("Category", "NCR")]
[Trait("Category", "VirtualProduction")]
[Trait("Category", "ProdStyle")]
[Collection("EnvironmentVariables")]
public sealed class VirtualProductionNcrRoutingTests
{
    [Fact(Timeout = TestTimeouts.HostTouching)]
    public async Task NcrCapabilityPoller_exposes_environment_hardware_profiler_snapshot()
    {
        const long expectedVram = 24L * 1024 * 1024 * 1024;

        await using var env = await VirtualProductionNcrRoutingHost.StartAsync(o =>
        {
            o.SetVramBytes(expectedVram, expectedVram);
            o.PostStartDelay = TimeSpan.FromMilliseconds(300);
        });

        var snap = env.GetNcrSnapshot();
        Assert.Equal(expectedVram, snap.AvailableVramBytes);
        Assert.Equal(GpuComputeClass.Extreme, snap.ComputeClass);
    }

    [Fact(Timeout = TestTimeouts.HostTouching)]
    public async Task Peer_snapshot_refresh_changes_routing_preference_cloud_then_peer()
    {
        await using var env = await VirtualProductionNcrRoutingHost.StartAsync(o =>
        {
            o.SetVramBytes(512L * 1024 * 1024, 512L * 1024 * 1024);
            o.RunPodCloud.InstanceId = "cloud-inst";
            o.RunPodCloud.JobId = "j1";
            o.RunPodCloud.PullBytes = [1];
            o.RunPodCloud.PollStatuses.Enqueue(new RunPodLoopbackPollStatus { status = "completed" });
            o.PostStartDelay = TimeSpan.FromMilliseconds(350);
        });

        var reqs = new JobRequirements
        {
            ModelId = "large",
            MinimumVramBytes = 8L * 1024 * 1024 * 1024,
            ComputeClass = GpuComputeClass.Medium
        };

        var router = env.GetRouter();

        var beforePeer = router.ResolveExecutionTarget(reqs);
        beforePeer.Should().BeOfType<ExecutionTarget.Remote>();
        ((ExecutionTarget.Remote)beforePeer).Executor.Should().BeOfType<RunPodBrick>();

        env.WriteMeshPeers(
        [
            new PeerInfo
            {
                PeerId = "cluster-gpu-1",
                Endpoint = "http://cluster-gpu-1.mesh.local:8080",
                TrustTier = PeerTrustTier.Trusted,
                Capabilities =
                [
                    "generation.capability-routing",
                    "vram:68719476736",
                    "compute:High",
                    "queue:0",
                    "trust:Trusted"
                ]
            }
        ]);

        // The peer snapshot is refreshed by a background poller (150 ms cadence in this host);
        // wait for the routing decision to actually flip rather than for a guessed interval a
        // slow runner can overshoot. The assertions below still report a real failure.
        var afterPeer = await WaitForTargetAsync(
            () => router.ResolveExecutionTarget(reqs),
            t => t is ExecutionTarget.Remote { Executor: IPeerExecutor });
        afterPeer.Should().BeOfType<ExecutionTarget.Remote>();
        ((ExecutionTarget.Remote)afterPeer).Executor.Should().BeAssignableTo<IPeerExecutor>();
    }

    /// <summary>
    /// Polls <paramref name="resolve"/> until <paramref name="accept"/> holds or the timeout
    /// elapses, returning the last resolved target either way so the caller's assertions
    /// produce the diagnostic.
    /// </summary>
    private static async Task<ExecutionTarget> WaitForTargetAsync(
        Func<ExecutionTarget> resolve,
        Func<ExecutionTarget, bool> accept,
        int timeoutMs = 10_000)
    {
        var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
        var target = resolve();
        while (!accept(target) && DateTime.UtcNow < deadline)
        {
            await Task.Delay(50);
            target = resolve();
        }

        return target;
    }

    [Fact(Timeout = TestTimeouts.HostTouching)]
    public async Task CapabilityRoutingBrick_executes_locally_via_ProviderFactory_when_ncr_snapshot_satisfies_job()
    {
        await using var env = await VirtualProductionNcrRoutingHost.StartAsync(o =>
        {
            o.SetVramBytes(96L * 1024 * 1024 * 1024, 96L * 1024 * 1024 * 1024);
            o.PostStartDelay = TimeSpan.FromMilliseconds(300);
        });

        var reqs = new JobRequirements
        {
            ModelId = "small",
            MinimumVramBytes = 4L * 1024 * 1024 * 1024,
            ComputeClass = GpuComputeClass.Medium
        };

        var brick = env.GetCapabilityRoutingBrick();
        var result = await brick.ExecuteAsync(
            new RunPodJobPayload { ModelId = "small", Prompt = "virtual prod" },
            reqs,
            new VirtualProdExecutionContext { Provider = "mock-json" });

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Provider.Should().Be("mock-json");
        result.Value.IsRemote.Should().BeFalse();
    }
}
