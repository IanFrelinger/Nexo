using FluentAssertions;
using Nexo.Core.Application.Execution.Routing;
using Nexo.Core.Application.Mesh.Models;
using Nexo.Core.Application.NodeCapabilityRuntime.Models;
using Nexo.Core.Domain;
using Nexo.Core.Domain.Execution;
using Nexo.Infrastructure.Execution.Routing;
using Nexo.Tests.Infrastructure.Helpers.Ncr;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.NodeCapabilityRuntime;

/// <summary>
/// Integration-style tests using the real capability-routing DI graph (pollers + router + bricks)
/// inside a generic host, with deterministic substitutes for hardware and mesh — closest virtual
/// analogue to production wiring without GPUs or real peers.
/// </summary>
[Trait("Category", "NCR")]
[Trait("Category", "VirtualProduction")]
public sealed class VirtualProductionNcrRoutingTests
{
    [Fact]
    public async Task NcrCapabilityPoller_exposes_simulated_hardware_snapshot()
    {
        const long expectedVram = 24L * 1024 * 1024 * 1024;

        await using var env = await VirtualProductionNcrRoutingHost.StartAsync(o =>
        {
            o.HardwareProfiler.Profile = new NodeProfile
            {
                AvailableVRAMBytes = expectedVram,
                TotalVRAMBytes = expectedVram,
                Tier = NodeTier.Standard,
                CapturedAt = DateTimeOffset.UtcNow
            };
            o.PostStartDelay = TimeSpan.FromMilliseconds(300);
        });

        var snap = env.GetNcrSnapshot();
        snap.AvailableVramBytes.Should().Be(expectedVram);
        snap.ComputeClass.Should().Be(GpuComputeClass.High);
    }

    [Fact]
    public async Task Peer_snapshot_refresh_changes_routing_preference_cloud_then_peer()
    {
        await using var env = await VirtualProductionNcrRoutingHost.StartAsync(o =>
        {
            o.HardwareProfiler.Profile = new NodeProfile
            {
                AvailableVRAMBytes = 512L * 1024 * 1024,
                TotalVRAMBytes = 512L * 1024 * 1024,
                Tier = NodeTier.Nano,
                CapturedAt = DateTimeOffset.UtcNow
            };
            o.RunPodStub.SpinUpResult = Result<RunPodInstance>.Success(new RunPodInstance
            {
                InstanceId = "cloud-inst",
                ModelId = "m",
                GpuType = "SIM"
            });
            o.RunPodStub.DispatchResult = Result<JobHandle>.Success(new JobHandle { InstanceId = "cloud-inst", JobId = "j1" });
            o.RunPodStub.StatusSequence = new Queue<JobStatus>(
            [
                new JobStatus { State = RunPodJobState.Completed }
            ]);
            o.RunPodStub.PullResult = Result<byte[]>.Success([1]);
            o.RunPodStub.TerminateResult = Result<Unit>.Success(Unit.Value);
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

        env.Mesh.SetPeers(
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

    [Fact]
    public async Task CapabilityRoutingBrick_executes_locally_when_ncr_snapshot_satisfies_job()
    {
        await using var env = await VirtualProductionNcrRoutingHost.StartAsync(o =>
        {
            o.HardwareProfiler.Profile = new NodeProfile
            {
                AvailableVRAMBytes = 96L * 1024 * 1024 * 1024,
                TotalVRAMBytes = 96L * 1024 * 1024 * 1024,
                Tier = NodeTier.Core,
                CapturedAt = DateTimeOffset.UtcNow
            };
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
            new VirtualProdExecutionContext());

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Provider.Should().Be("virtual-local");
        result.Value.IsRemote.Should().BeFalse();
    }
}
