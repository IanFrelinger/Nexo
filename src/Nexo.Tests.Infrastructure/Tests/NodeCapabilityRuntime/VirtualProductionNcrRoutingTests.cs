using FluentAssertions;
using Nexo.Core.Application.Execution.Routing;
using Nexo.Core.Application.Mesh.Models;
using Nexo.Core.Application.NodeCapabilityRuntime.Models;
using Nexo.Core.Domain;
using Nexo.Core.Domain.Execution;
using Nexo.Infrastructure.Execution.Routing;
using Nexo.Tests.Infrastructure.Helpers.Ncr;
using Xunit;
using Nexo.Tests.Infrastructure.Helpers;

namespace Nexo.Tests.Infrastructure.Tests.NodeCapabilityRuntime;

/// <summary>
/// Uses production DI registrations (<see cref="RunPodHttpClient"/>, <see cref="ProviderFactoryLocalExecutor"/>,
/// <see cref="EnvironmentHardwareProfiler"/>, <see cref="FileBasedInstanceDiscovery"/>) inside a generic host.
/// RunPod traffic goes to <see cref="RunPodLoopbackApiServer"/> (same REST paths as cloud API).
/// </summary>
[Trait("Category", "NCR")]
[Trait("Category", "VirtualProduction")]
[Trait("Category", "ProdStyle")]
public sealed class VirtualProductionNcrRoutingTests
{
    [Fact(Timeout = TestTimeouts.Integration)]
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

    [Fact(Timeout = TestTimeouts.Integration)]
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

        await Task.Delay(600);

        var afterPeer = router.ResolveExecutionTarget(reqs);
        afterPeer.Should().BeOfType<ExecutionTarget.Remote>();
        ((ExecutionTarget.Remote)afterPeer).Executor.Should().BeAssignableTo<IPeerExecutor>();
    }

    [Fact(Timeout = TestTimeouts.Integration)]
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
