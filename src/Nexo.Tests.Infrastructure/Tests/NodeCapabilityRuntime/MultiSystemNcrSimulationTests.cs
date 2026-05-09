using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Nexo.Core.Application.Execution.Routing;
using Nexo.Core.Application.NodeCapabilityRuntime.Ports;
using Nexo.Core.Domain;
using Nexo.Core.Domain.Execution;
using Nexo.Infrastructure.Execution.Routing;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.NodeCapabilityRuntime;

/// <summary>
/// Simulates a multi-node mesh: each “instance” has its own NCR snapshot plus peer view.
/// Covers both synchronous routing (<see cref="ICapabilityRouter.ResolveExecutionTarget"/>) and
/// asynchronous brick execution (<see cref="CapabilityRoutingBrick.ExecuteAsync"/>).
/// </summary>
[Trait("Category", "NCR")]
[Trait("Category", "ProdStyle")]
public sealed class MultiSystemNcrSimulationTests
{
    [Fact]
    public void SyncRouting_SequentialVirtualNodes_ReflectsDistinctLocalAndPeerCapabilityViews()
    {
        var reqs = new JobRequirements
        {
            ModelId = "gen",
            MinimumVramBytes = 6L * 1024 * 1024 * 1024,
            ComputeClass = GpuComputeClass.Medium
        };

        var gpuCore = CreateRouter(
            "gpu-core",
            localVramGiB: 80,
            GpuComputeClass.Extreme,
            queueDepth: 0,
            peers: [],
            enablePeers: false);

        var edge = CreateRouter(
            "edge",
            localVramGiB: 1,
            GpuComputeClass.Low,
            queueDepth: 0,
            peers:
            [
                new PeerExecutionCandidate
                {
                    PeerId = "gpu-core",
                    Endpoint = "http://gpu-core.internal:8080",
                    AvailableVramBytes = 64L * 1024 * 1024 * 1024,
                    ComputeClass = GpuComputeClass.Extreme,
                    QueueDepth = 0
                }
            ],
            enablePeers: true);

        var batchOnly = CreateRouter(
            "batch-satellite",
            localVramGiB: 1,
            GpuComputeClass.Low,
            queueDepth: 0,
            peers: [],
            enablePeers: false);

        var tCore = gpuCore.ResolveExecutionTarget(reqs);
        var tEdge = edge.ResolveExecutionTarget(reqs);
        var tSat = batchOnly.ResolveExecutionTarget(reqs);

        tCore.Should().BeOfType<ExecutionTarget.Local>();
        tEdge.Should().BeOfType<ExecutionTarget.Remote>("edge should offload to peer mesh when local NCR is insufficient");
        ((ExecutionTarget.Remote)tEdge).Executor.Should().BeAssignableTo<IPeerExecutor>();
        tSat.Should().BeOfType<ExecutionTarget.Remote>("no peers ⇒ cloud RunPod brick");
        ((ExecutionTarget.Remote)tSat).Executor.Should().BeAssignableTo<RunPodBrick>();
    }

    [Fact]
    public async Task AsyncRouting_ParallelVirtualNodes_ConcurrentResolutionWithoutCrossTalk()
    {
        const int iterations = 64;
        var reqs = new JobRequirements
        {
            ModelId = "gen",
            MinimumVramBytes = 4L * 1024 * 1024 * 1024,
            ComputeClass = GpuComputeClass.Low
        };

        var gpuCore = CreateRouter(
            "gpu-core-parallel",
            localVramGiB: 48,
            GpuComputeClass.High,
            queueDepth: 0,
            peers: [],
            enablePeers: false);

        var edge = CreateRouter(
            "edge-parallel",
            localVramGiB: 0.5,
            GpuComputeClass.Low,
            queueDepth: 2,
            peers:
            [
                new PeerExecutionCandidate
                {
                    PeerId = "helper",
                    Endpoint = "http://helper:8080",
                    AvailableVramBytes = 24L * 1024 * 1024 * 1024,
                    ComputeClass = GpuComputeClass.High,
                    QueueDepth = 0
                }
            ],
            enablePeers: true);

        var tasks = new List<Task<bool>>();
        foreach (var _ in Enumerable.Range(0, iterations))
        {
            tasks.Add(Task.Run(() => gpuCore.ResolveExecutionTarget(reqs) is ExecutionTarget.Local));
            tasks.Add(Task.Run(() => edge.ResolveExecutionTarget(reqs) is ExecutionTarget.Remote));
        }

        var flags = await Task.WhenAll(tasks);
        flags.Should().HaveCount(iterations * 2);
        flags.Should().OnlyContain(x => x);
    }

    [Fact]
    public async Task MixedSyncRouterAndAsyncBrick_EndToEnd_LocalPeerAndCloudPaths()
    {
        var tmp = Path.Combine(Path.GetTempPath(), $"nexo-ncr-multi-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tmp);

        var reqs = new JobRequirements
        {
            ModelId = "gen",
            MinimumVramBytes = 8L * 1024 * 1024 * 1024,
            ComputeClass = GpuComputeClass.Medium,
            EstimatedDuration = TimeSpan.FromSeconds(30)
        };

        var payload = new RunPodJobPayload { ModelId = "gen", Prompt = "simulate" };

        var localBrick = CreateBrickBundle(
            "local-strong",
            localVramGiB: 48,
            GpuComputeClass.High,
            queueDepth: 0,
            peers: [],
            enablePeers: false,
            peerDelayMs: 0,
            outputDir: tmp,
            configureRunPodForRemote: false);

        var peerBrick = CreateBrickBundle(
            "edge-offload",
            localVramGiB: 1,
            GpuComputeClass.Low,
            queueDepth: 0,
            peers:
            [
                new PeerExecutionCandidate
                {
                    PeerId = "core-peer",
                    Endpoint = "http://core-peer:8080",
                    AvailableVramBytes = 40L * 1024 * 1024 * 1024,
                    ComputeClass = GpuComputeClass.High,
                    QueueDepth = 0
                }
            ],
            enablePeers: true,
            peerDelayMs: 3,
            outputDir: tmp,
            configureRunPodForRemote: false);

        var cloudBrick = CreateBrickBundle(
            "cloud-only",
            localVramGiB: 1,
            GpuComputeClass.Low,
            queueDepth: 0,
            peers: [],
            enablePeers: false,
            peerDelayMs: 0,
            outputDir: tmp,
            configureRunPodForRemote: true);

        // Sync routing decisions (no I/O).
        localBrick.Router.ResolveExecutionTarget(reqs).Should().BeOfType<ExecutionTarget.Local>();
        peerBrick.Router.ResolveExecutionTarget(reqs).Should().BeOfType<ExecutionTarget.Remote>();
        cloudBrick.Router.ResolveExecutionTarget(reqs).Should().BeOfType<ExecutionTarget.Remote>();

        var ctx = new TestExecutionContext();

        // Async brick execution — includes deliberately delayed peer executor to mimic async capacity.
        var tLocal = localBrick.Brick.ExecuteAsync(payload, reqs, ctx);
        var tPeer = peerBrick.Brick.ExecuteAsync(payload, reqs, ctx);
        var tCloud = cloudBrick.Brick.ExecuteAsync(payload, reqs, ctx);

        var outcomes = await Task.WhenAll(tLocal, tPeer, tCloud);
        var lr = outcomes[0];
        var pr = outcomes[1];
        var cr = outcomes[2];

        lr.IsSuccess.Should().BeTrue();
        lr.Value!.IsRemote.Should().BeFalse();
        lr.Value.Provider.Should().Be("local-sync-async");

        pr.IsSuccess.Should().BeTrue();
        pr.Value!.IsRemote.Should().BeTrue();
        pr.Value.Provider.Should().Be("peer-async");

        cr.IsSuccess.Should().BeTrue();
        cr.Value!.IsRemote.Should().BeTrue();
        cr.Value.Provider.Should().Be("runpod");
    }

    private static NcrCapabilityRouter CreateRouter(
        string nodeId,
        double localVramGiB,
        GpuComputeClass computeClass,
        int queueDepth,
        IReadOnlyList<PeerExecutionCandidate> peers,
        bool enablePeers)
    {
        var vramBytes = (long)(localVramGiB * 1024 * 1024 * 1024);
        var snapshot = new SnapshotStub
        {
            AvailableVramBytes = vramBytes,
            ComputeClass = computeClass,
            CurrentQueueDepth = queueDepth,
            CapturedAt = DateTimeOffset.UtcNow
        };

        var config = Options.Create(new RunPodBrickConfig
        {
            QueueDepthThreshold = 8,
            Timeout = TimeSpan.FromSeconds(4),
            PollingInterval = TimeSpan.FromMilliseconds(30),
            OutputStagingPath = Path.Combine(Path.GetTempPath(), $"nexo-ncr-sim-{nodeId}"),
            EnablePeerNetworkRouting = enablePeers,
            PreferPeerNetworkOverCloud = true
        });

        var localExecutor = new StubLocalExecutor(_ => Result<GenerationExecutionResult>.Success(new GenerationExecutionResult
        {
            Payload = [1],
            Provider = "local-" + nodeId,
            ModelId = "gen",
            IsRemote = false,
            Summary = "local"
        }));

        var runPodBrick = new RunPodBrick(new StubRunPodClient(), config, NullLogger<RunPodBrick>.Instance);
        var peerExecutor = new DelayedPeerExecutor(TimeSpan.FromMilliseconds(2));
        peerExecutor.NextResult = Result<GenerationExecutionResult>.Success(new GenerationExecutionResult
        {
            Payload = [2],
            Provider = "peer",
            ModelId = "gen",
            IsRemote = true,
            Summary = "peer"
        });

        var peerSnapshot = new StubPeerSnapshot { Candidates = peers };

        return new NcrCapabilityRouter(
            snapshot,
            peerSnapshot,
            peerExecutor,
            localExecutor,
            runPodBrick,
            config,
            NullLogger<NcrCapabilityRouter>.Instance);
    }

    private static BrickBundle CreateBrickBundle(
        string nodeId,
        double localVramGiB,
        GpuComputeClass computeClass,
        int queueDepth,
        IReadOnlyList<PeerExecutionCandidate> peers,
        bool enablePeers,
        int peerDelayMs,
        string outputDir,
        bool configureRunPodForRemote)
    {
        var vramBytes = (long)(localVramGiB * 1024 * 1024 * 1024);
        var snapshot = new SnapshotStub
        {
            AvailableVramBytes = vramBytes,
            ComputeClass = computeClass,
            CurrentQueueDepth = queueDepth
        };

        var runPodClient = new StubRunPodClient();
        if (configureRunPodForRemote)
        {
            runPodClient.SpinUpResult = Result<RunPodInstance>.Success(new RunPodInstance
            {
                InstanceId = "sim-inst",
                ModelId = "gen",
                GpuType = "SIM_GPU"
            });
            runPodClient.DispatchResult = Result<JobHandle>.Success(new JobHandle { InstanceId = "sim-inst", JobId = "sim-job" });
            runPodClient.StatusSequence = new Queue<JobStatus>(
            [
                new JobStatus { State = RunPodJobState.Running },
                new JobStatus { State = RunPodJobState.Completed }
            ]);
            runPodClient.PullResult = Result<byte[]>.Success([7, 7, 7]);
            runPodClient.TerminateResult = Result<Unit>.Success(Unit.Value);
        }

        var config = Options.Create(new RunPodBrickConfig
        {
            QueueDepthThreshold = 8,
            Timeout = TimeSpan.FromSeconds(6),
            PollingInterval = TimeSpan.FromMilliseconds(25),
            OutputStagingPath = Path.Combine(outputDir, nodeId),
            EnablePeerNetworkRouting = enablePeers,
            PreferPeerNetworkOverCloud = true
        });

        var localExecutor = new StubLocalExecutor(_ => Result<GenerationExecutionResult>.Success(new GenerationExecutionResult
        {
            Payload = [9],
            Provider = "local-sync-async",
            ModelId = "gen",
            IsRemote = false,
            Summary = "local ok"
        }));

        var runPodBrick = new RunPodBrick(runPodClient, config, NullLogger<RunPodBrick>.Instance);
        var peerExecutor = new DelayedPeerExecutor(TimeSpan.FromMilliseconds(peerDelayMs));
        peerExecutor.NextResult = Result<GenerationExecutionResult>.Success(new GenerationExecutionResult
        {
            Payload = [3, 3],
            Provider = "peer-async",
            ModelId = "gen",
            IsRemote = true,
            Summary = "peer ok"
        });

        var peerSnapshot = new StubPeerSnapshot { Candidates = peers };

        var router = new NcrCapabilityRouter(
            snapshot,
            peerSnapshot,
            peerExecutor,
            localExecutor,
            runPodBrick,
            config,
            NullLogger<NcrCapabilityRouter>.Instance);

        var brick = new CapabilityRoutingBrick(router, NullLogger<CapabilityRoutingBrick>.Instance);
        return new BrickBundle(router, brick);
    }

    private sealed record BrickBundle(NcrCapabilityRouter Router, CapabilityRoutingBrick Brick);

    private sealed class SnapshotStub : INCRCapabilitySnapshot
    {
        public long AvailableVramBytes { get; set; }
        public GpuComputeClass ComputeClass { get; set; } = GpuComputeClass.None;
        public int CurrentQueueDepth { get; set; }
        public DateTimeOffset CapturedAt { get; set; } = DateTimeOffset.UtcNow;
    }

    private sealed class StubPeerSnapshot : IPeerCapabilitySnapshot
    {
        public IReadOnlyList<PeerExecutionCandidate> Candidates { get; init; } = [];
    }

    private sealed class StubLocalExecutor : ILocalExecutor
    {
        private readonly Func<RunPodJobPayload, Result<GenerationExecutionResult>> _handler;

        public StubLocalExecutor(Func<RunPodJobPayload, Result<GenerationExecutionResult>> handler) => _handler = handler;

        public Task<Result<GenerationExecutionResult>> ExecuteAsync(
            RunPodJobPayload payload,
            JobRequirements requirements,
            IExecutionContext context,
            CancellationToken cancellationToken = default)
            => Task.FromResult(_handler(payload));
    }

    /// <summary>Peer executor that yields — exercises async scheduling distinct from sync routing.</summary>
    private sealed class DelayedPeerExecutor : IPeerExecutor
    {
        private readonly TimeSpan _delay;
        public Result<GenerationExecutionResult> NextResult { get; set; } =
            Result<GenerationExecutionResult>.Failure("peer.not_configured", "not configured");

        public DelayedPeerExecutor(TimeSpan delay) => _delay = delay;

        public async Task<Result<GenerationExecutionResult>> ExecuteAsync(
            RunPodJobPayload payload,
            JobRequirements requirements,
            IExecutionContext context,
            CancellationToken cancellationToken = default)
        {
            if (_delay > TimeSpan.Zero)
                await Task.Delay(_delay, cancellationToken).ConfigureAwait(false);
            return NextResult;
        }
    }

    private sealed class StubRunPodClient : IRunPodClient
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
            return Task.FromResult(Result<JobStatus>.Success(new JobStatus { State = RunPodJobState.Running }));
        }

        public Task<Result<byte[]>> PullResults(JobHandle jobHandle, CancellationToken cancellationToken = default)
            => Task.FromResult(PullResult);

        public Task<Result<Unit>> TerminateInstance(string instanceId, CancellationToken cancellationToken = default)
            => Task.FromResult(TerminateResult);
    }

    private sealed class TestExecutionContext : IExecutionContext
    {
        public string AgentId { get; init; } = "ncr-sim-agent";
        public string BehaviorId { get; init; } = "ncr-sim-behavior";
        public bool IsAirGapped { get; init; }
        public bool AuditMode { get; init; } = true;
        public string Provider { get; init; } = "simulation";
        public IReadOnlyDictionary<string, object> Variables { get; init; } = new Dictionary<string, object>();
    }
}
