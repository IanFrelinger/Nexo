using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Nexo.Core.Application.Execution.Routing;
using Nexo.Core.Application.NodeCapabilityRuntime.Ports;
using Nexo.Core.Domain.Execution;
using Nexo.Infrastructure.Adaptation;
using Nexo.Infrastructure.Execution;
using Nexo.Infrastructure.Execution.Routing;
using Nexo.Core.Application.Mesh.Models;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.Execution;

public sealed class CapabilityRoutingBrickTests
{
    [Fact]
    public async Task HappyPath_LocalExecution()
    {
        var snapshot = new SnapshotStub
        {
            AvailableVramBytes = 32L * 1024 * 1024 * 1024,
            ComputeClass = GpuComputeClass.High,
            CurrentQueueDepth = 0
        };
        var localExecutor = new StubLocalExecutor(_ => Result<GenerationExecutionResult>.Success(new GenerationExecutionResult
        {
            Payload = [1, 2, 3],
            Provider = "local",
            ModelId = "local-model",
            IsRemote = false,
            Summary = "local ok"
        }));
        var config = Options.Create(new RunPodBrickConfig
        {
            Timeout = TimeSpan.FromSeconds(2),
            PollingInterval = TimeSpan.FromMilliseconds(20),
            QueueDepthThreshold = 4,
            OutputStagingPath = Path.Combine(Path.GetTempPath(), "nexo-runpod-tests-local")
        });
        var runPodClient = new StubRunPodClient();
        var runPodBrick = new RunPodBrick(runPodClient, config, NullLogger<RunPodBrick>.Instance);
        var peerExecutor = new StubPeerExecutor();
        var peerSnapshot = new StubPeerSnapshot();
        var router = new NcrCapabilityRouter(
            snapshot,
            peerSnapshot,
            peerExecutor,
            localExecutor,
            runPodBrick,
            config,
            NullLogger<NcrCapabilityRouter>.Instance);
        var brick = new CapabilityRoutingBrick(router, NullLogger<CapabilityRoutingBrick>.Instance);

        var result = await brick.ExecuteAsync(
            new RunPodJobPayload
            {
                ModelId = "local-model",
                Prompt = "hello"
            },
            new JobRequirements
            {
                ModelId = "local-model",
                MinimumVramBytes = 2L * 1024 * 1024 * 1024,
                ComputeClass = GpuComputeClass.Medium,
                EstimatedDuration = TimeSpan.FromSeconds(10)
            },
            new TestExecutionContext());

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.IsRemote.Should().BeFalse();
        result.Value.Provider.Should().Be("local");
    }

    [Fact]
    public async Task HappyPath_RemoteExecution()
    {
        var outputDir = Path.Combine(Path.GetTempPath(), $"nexo-runpod-tests-remote-{Guid.NewGuid():N}");
        var snapshot = new SnapshotStub
        {
            AvailableVramBytes = 256L * 1024 * 1024,
            ComputeClass = GpuComputeClass.Low,
            CurrentQueueDepth = 0
        };
        var config = Options.Create(new RunPodBrickConfig
        {
            Timeout = TimeSpan.FromSeconds(2),
            PollingInterval = TimeSpan.FromMilliseconds(20),
            QueueDepthThreshold = 4,
            OutputStagingPath = outputDir
        });
        var runPodClient = new StubRunPodClient
        {
            SpinUpResult = Result<RunPodInstance>.Success(new RunPodInstance
            {
                InstanceId = "inst-1",
                ModelId = "remote-model",
                GpuType = "NVIDIA_A4000"
            }),
            DispatchResult = Result<JobHandle>.Success(new JobHandle
            {
                InstanceId = "inst-1",
                JobId = "job-1"
            }),
            StatusSequence = new Queue<JobStatus>(
            [
                new JobStatus { State = RunPodJobState.Running },
                new JobStatus { State = RunPodJobState.Completed }
            ]),
            PullResult = Result<byte[]>.Success([9, 8, 7]),
            TerminateResult = Result<Unit>.Success(Unit.Value)
        };

        var localExecutor = new StubLocalExecutor(_ =>
            Result<GenerationExecutionResult>.Failure("local.should_not_run", "Local executor should not run in this test."));
        var runPodBrick = new RunPodBrick(runPodClient, config, NullLogger<RunPodBrick>.Instance);
        var peerExecutor = new StubPeerExecutor();
        var peerSnapshot = new StubPeerSnapshot();
        var router = new NcrCapabilityRouter(
            snapshot,
            peerSnapshot,
            peerExecutor,
            localExecutor,
            runPodBrick,
            config,
            NullLogger<NcrCapabilityRouter>.Instance);
        var brick = new CapabilityRoutingBrick(router, NullLogger<CapabilityRoutingBrick>.Instance);

        var result = await brick.ExecuteAsync(
            new RunPodJobPayload
            {
                ModelId = "remote-model",
                Prompt = "hello remote"
            },
            new JobRequirements
            {
                ModelId = "remote-model",
                MinimumVramBytes = 4L * 1024 * 1024 * 1024,
                ComputeClass = GpuComputeClass.High,
                EstimatedDuration = TimeSpan.FromMinutes(1)
            },
            new TestExecutionContext());

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.IsRemote.Should().BeTrue();
        result.Value.Provider.Should().Be("runpod");
        File.Exists(result.Value.OutputPath).Should().BeTrue();
        runPodClient.TerminateCalls.Should().Be(1);
    }

    [Fact]
    public void NCR_RoutesToRemote_DueToInsufficientVram()
    {
        var router = BuildRouter(
            availableVram: 128L * 1024 * 1024,
            computeClass: GpuComputeClass.High,
            queueDepth: 0,
            queueThreshold: 4);

        var target = router.ResolveExecutionTarget(new JobRequirements
        {
            ModelId = "m",
            MinimumVramBytes = 2L * 1024 * 1024 * 1024,
            ComputeClass = GpuComputeClass.Low
        });

        target.Should().BeOfType<ExecutionTarget.Remote>();
    }

    [Fact]
    public void NCR_RoutesToRemote_DueToQueueDepth()
    {
        var router = BuildRouter(
            availableVram: 32L * 1024 * 1024 * 1024,
            computeClass: GpuComputeClass.High,
            queueDepth: 12,
            queueThreshold: 4);

        var target = router.ResolveExecutionTarget(new JobRequirements
        {
            ModelId = "m",
            MinimumVramBytes = 256L * 1024 * 1024,
            ComputeClass = GpuComputeClass.Low
        });

        target.Should().BeOfType<ExecutionTarget.Remote>();
    }

    [Fact]
    public void NCR_RoutesToRemote_DueToInsufficientComputeClass()
    {
        var router = BuildRouter(
            availableVram: 32L * 1024 * 1024 * 1024,
            computeClass: GpuComputeClass.Low,
            queueDepth: 0,
            queueThreshold: 4);

        var target = router.ResolveExecutionTarget(new JobRequirements
        {
            ModelId = "m",
            MinimumVramBytes = 512L * 1024 * 1024,
            ComputeClass = GpuComputeClass.High
        });

        target.Should().BeOfType<ExecutionTarget.Remote>();
    }

    [Fact]
    public void OvernightFlag_ForcesRemote()
    {
        var router = BuildRouter(
            availableVram: 32L * 1024 * 1024 * 1024,
            computeClass: GpuComputeClass.Extreme,
            queueDepth: 0,
            queueThreshold: 99);

        var target = router.ResolveExecutionTarget(new JobRequirements
        {
            ModelId = "m",
            MinimumVramBytes = 1,
            ComputeClass = GpuComputeClass.Low,
            IsOvernightOrBackground = true
        });

        target.Should().BeOfType<ExecutionTarget.Remote>();
    }

    [Fact]
    public async Task RunPodTimeout_WithTeardown()
    {
        var outputDir = Path.Combine(Path.GetTempPath(), $"nexo-runpod-timeout-{Guid.NewGuid():N}");
        var config = Options.Create(new RunPodBrickConfig
        {
            Timeout = TimeSpan.FromMilliseconds(120),
            PollingInterval = TimeSpan.FromMilliseconds(15),
            OutputStagingPath = outputDir
        });
        var client = new StubRunPodClient
        {
            SpinUpResult = Result<RunPodInstance>.Success(new RunPodInstance
            {
                InstanceId = "inst-timeout",
                ModelId = "m",
                GpuType = "gpu"
            }),
            DispatchResult = Result<JobHandle>.Success(new JobHandle
            {
                InstanceId = "inst-timeout",
                JobId = "job-timeout"
            }),
            StatusSequence = new Queue<JobStatus>(
            [
                new JobStatus { State = RunPodJobState.Running },
                new JobStatus { State = RunPodJobState.Running },
                new JobStatus { State = RunPodJobState.Running },
                new JobStatus { State = RunPodJobState.Running },
                new JobStatus { State = RunPodJobState.Running }
            ]),
            TerminateResult = Result<Unit>.Success(Unit.Value)
        };
        var brick = new RunPodBrick(client, config, NullLogger<RunPodBrick>.Instance);

        var result = await brick.ExecuteAsync(
            new RunPodJobPayload { ModelId = "m", Prompt = "timeout please" },
            new JobRequirements { ModelId = "m" },
            new TestExecutionContext());

        result.IsFailure.Should().BeTrue();
        result.Error.Should().NotBeNull();
        result.Error!.Code.Should().Be("runpod.timeout");
        client.TerminateCalls.Should().Be(1);
    }

    [Fact]
    public async Task RunPodFailure_WithTeardown()
    {
        var outputDir = Path.Combine(Path.GetTempPath(), $"nexo-runpod-failure-{Guid.NewGuid():N}");
        var config = Options.Create(new RunPodBrickConfig
        {
            Timeout = TimeSpan.FromSeconds(2),
            PollingInterval = TimeSpan.FromMilliseconds(20),
            OutputStagingPath = outputDir
        });
        var client = new StubRunPodClient
        {
            SpinUpResult = Result<RunPodInstance>.Success(new RunPodInstance
            {
                InstanceId = "inst-failure",
                ModelId = "m",
                GpuType = "gpu"
            }),
            DispatchResult = Result<JobHandle>.Failure("dispatch.failed", "Dispatch failed"),
            TerminateResult = Result<Unit>.Success(Unit.Value)
        };
        var brick = new RunPodBrick(client, config, NullLogger<RunPodBrick>.Instance);

        var result = await brick.ExecuteAsync(
            new RunPodJobPayload { ModelId = "m", Prompt = "fail dispatch" },
            new JobRequirements { ModelId = "m" },
            new TestExecutionContext());

        result.IsFailure.Should().BeTrue();
        result.Error.Should().NotBeNull();
        result.Error!.Code.Should().Be("dispatch.failed");
        client.TerminateCalls.Should().Be(1);
    }

    [Fact]
    public void Config_IsLoaded_FromExistingOptionsPattern()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Nexo:RunPod:ApiKey"] = "test-key",
                ["Nexo:RunPod:PreferredGpuTier"] = "NVIDIA_H100",
                ["Nexo:RunPod:Timeout"] = "00:00:07",
                ["Nexo:RunPod:PollingInterval"] = "00:00:01",
                ["Nexo:RunPod:OutputStagingPath"] = "/tmp/nexo-test-staging",
                ["Nexo:RunPod:QueueDepthThreshold"] = "9"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IProviderFactory, StubProviderFactory>();
        services.AddRunPodCapabilityRouting(configuration);
        using var provider = services.BuildServiceProvider();

        var config = provider.GetRequiredService<IOptions<RunPodBrickConfig>>().Value;
        var adaptation = provider.GetRequiredService<IOptions<AdaptationBrickOptions>>().Value;

        config.ApiKey.Should().Be("test-key");
        config.PreferredGpuTier.Should().Be("NVIDIA_H100");
        config.Timeout.Should().Be(TimeSpan.FromSeconds(7));
        config.PollingInterval.Should().Be(TimeSpan.FromSeconds(1));
        config.OutputStagingPath.Should().Be("/tmp/nexo-test-staging");
        config.QueueDepthThreshold.Should().Be(9);
        adaptation.AdditionalBrickTypes.Should().Contain(typeof(RunPodBrick));
        adaptation.AdditionalBrickTypes.Should().Contain(typeof(CapabilityRoutingBrick));
    }

    [Fact]
    public void PeerPreferred_RoutesToPeer_WhenEligiblePeerExists()
    {
        var localExecutor = new StubLocalExecutor(_ => Result<GenerationExecutionResult>.Success(new GenerationExecutionResult
        {
            Payload = [1],
            Provider = "local",
            ModelId = "m",
            IsRemote = false
        }));
        var runPodConfig = Options.Create(new RunPodBrickConfig
        {
            Timeout = TimeSpan.FromSeconds(2),
            PollingInterval = TimeSpan.FromMilliseconds(20),
            QueueDepthThreshold = 4,
            EnablePeerNetworkRouting = true,
            PreferPeerNetworkOverCloud = true,
            OutputStagingPath = Path.Combine(Path.GetTempPath(), $"nexo-peer-pref-{Guid.NewGuid():N}")
        });
        var runPod = new RunPodBrick(new StubRunPodClient(), runPodConfig, NullLogger<RunPodBrick>.Instance);
        var peerExecutor = new StubPeerExecutor();
        var peerSnapshot = new StubPeerSnapshot
        {
            Candidates =
            [
                new PeerExecutionCandidate
                {
                    PeerId = "peer-1",
                    Endpoint = "http://peer-1",
                    AvailableVramBytes = 24L * 1024 * 1024 * 1024,
                    ComputeClass = GpuComputeClass.High,
                    QueueDepth = 0,
                    CapturedAt = DateTimeOffset.UtcNow
                }
            ]
        };
        var router = new NcrCapabilityRouter(
            new SnapshotStub
            {
                AvailableVramBytes = 128L * 1024 * 1024,
                ComputeClass = GpuComputeClass.Low,
                CurrentQueueDepth = 0
            },
            peerSnapshot,
            peerExecutor,
            localExecutor,
            runPod,
            runPodConfig,
            NullLogger<NcrCapabilityRouter>.Instance);

        var target = router.ResolveExecutionTarget(new JobRequirements
        {
            ModelId = "m",
            MinimumVramBytes = 4L * 1024 * 1024 * 1024,
            ComputeClass = GpuComputeClass.Medium,
            RemoteExecutionPreference = RemoteExecutionPreference.PreferPeerNetwork
        });

        target.Should().BeOfType<ExecutionTarget.Remote>();
        var remote = (ExecutionTarget.Remote)target;
        remote.Executor.Should().BeSameAs(peerExecutor);
        remote.Reason.ToLowerInvariant().Should().Contain("peer");
    }

    [Fact]
    public void PeerOnly_RoutesToPeer_WhenEligiblePeerExists()
    {
        var runPodConfig = Options.Create(new RunPodBrickConfig
        {
            Timeout = TimeSpan.FromSeconds(2),
            PollingInterval = TimeSpan.FromMilliseconds(20),
            QueueDepthThreshold = 4,
            EnablePeerNetworkRouting = true,
            OutputStagingPath = Path.Combine(Path.GetTempPath(), $"nexo-peer-only-{Guid.NewGuid():N}")
        });

        var localExecutor = new StubLocalExecutor(_ => Result<GenerationExecutionResult>.Success(new GenerationExecutionResult
        {
            Payload = [1],
            Provider = "local",
            ModelId = "m",
            IsRemote = false
        }));
        var runPod = new RunPodBrick(new StubRunPodClient(), runPodConfig, NullLogger<RunPodBrick>.Instance);
        var peerExecutor = new StubPeerExecutor();
        var peerSnapshot = new StubPeerSnapshot
        {
            Candidates =
            [
                new PeerExecutionCandidate
                {
                    PeerId = "peer-1",
                    Endpoint = "http://peer-1",
                    AvailableVramBytes = 16L * 1024 * 1024 * 1024,
                    ComputeClass = GpuComputeClass.High,
                    QueueDepth = 0,
                    CapturedAt = DateTimeOffset.UtcNow
                }
            ]
        };

        var router = new NcrCapabilityRouter(
            new SnapshotStub
            {
                AvailableVramBytes = 128L * 1024 * 1024,
                ComputeClass = GpuComputeClass.Low,
                CurrentQueueDepth = 0
            },
            peerSnapshot,
            peerExecutor,
            localExecutor,
            runPod,
            runPodConfig,
            NullLogger<NcrCapabilityRouter>.Instance);

        var target = router.ResolveExecutionTarget(new JobRequirements
        {
            ModelId = "m",
            MinimumVramBytes = 2L * 1024 * 1024 * 1024,
            ComputeClass = GpuComputeClass.Medium,
            RemoteExecutionPreference = RemoteExecutionPreference.PeerNetworkOnly
        });

        target.Should().BeOfType<ExecutionTarget.Remote>();
        var remote = (ExecutionTarget.Remote)target;
        remote.Executor.Should().BeSameAs(peerExecutor);
        remote.Reason.ToLowerInvariant().Should().Contain("peer");
    }

[Fact]
    public async Task PeerOnly_ReturnsErrorExecutor_WhenNoEligiblePeers()
    {
        var runPodConfig = Options.Create(new RunPodBrickConfig
        {
            Timeout = TimeSpan.FromSeconds(2),
            PollingInterval = TimeSpan.FromMilliseconds(20),
            QueueDepthThreshold = 4,
            EnablePeerNetworkRouting = true,
            OutputStagingPath = Path.Combine(Path.GetTempPath(), $"nexo-peer-none-{Guid.NewGuid():N}")
        });

        var localExecutor = new StubLocalExecutor(_ => Result<GenerationExecutionResult>.Success(new GenerationExecutionResult
        {
            Payload = [1],
            Provider = "local",
            ModelId = "m",
            IsRemote = false
        }));
        var runPod = new RunPodBrick(new StubRunPodClient(), runPodConfig, NullLogger<RunPodBrick>.Instance);
        var peerExecutor = new StubPeerExecutor
        {
            NextResult = Result<GenerationExecutionResult>.Failure("peer-routing.no_eligible_peers", "No eligible peer endpoint is currently available.")
        };
        var peerSnapshot = new StubPeerSnapshot
        {
            Candidates =
            [
                new PeerExecutionCandidate
                {
                    PeerId = "peer-low",
                    Endpoint = "http://peer-low",
                    AvailableVramBytes = 128L * 1024 * 1024,
                    ComputeClass = GpuComputeClass.Low,
                    QueueDepth = 0,
                    CapturedAt = DateTimeOffset.UtcNow
                }
            ]
        };

        var router = new NcrCapabilityRouter(
            new SnapshotStub
            {
                AvailableVramBytes = 128L * 1024 * 1024,
                ComputeClass = GpuComputeClass.Low,
                CurrentQueueDepth = 0
            },
            peerSnapshot,
            peerExecutor,
            localExecutor,
            runPod,
            runPodConfig,
            NullLogger<NcrCapabilityRouter>.Instance);

        var target = router.ResolveExecutionTarget(new JobRequirements
        {
            ModelId = "m",
            MinimumVramBytes = 8L * 1024 * 1024 * 1024,
            ComputeClass = GpuComputeClass.High,
            RemoteExecutionPreference = RemoteExecutionPreference.PeerNetworkOnly
        });

        target.Should().BeOfType<ExecutionTarget.Remote>();
        var remote = (ExecutionTarget.Remote)target;
        var result = await remote.Executor.ExecuteAsync(
            new RunPodJobPayload { ModelId = "m", Prompt = "p" },
            new JobRequirements { ModelId = "m" },
            new TestExecutionContext());
        result.IsFailure.Should().BeTrue();
        result.Error.Should().NotBeNull();
        result.Error!.Code.Should().Be("peer-routing.no_eligible_peers");
    }

    private static NcrCapabilityRouter BuildRouter(
        long availableVram,
        GpuComputeClass computeClass,
        int queueDepth,
        int queueThreshold)
    {
        var snapshot = new SnapshotStub
        {
            AvailableVramBytes = availableVram,
            ComputeClass = computeClass,
            CurrentQueueDepth = queueDepth
        };
        var config = Options.Create(new RunPodBrickConfig
        {
            QueueDepthThreshold = queueThreshold,
            Timeout = TimeSpan.FromSeconds(2),
            PollingInterval = TimeSpan.FromMilliseconds(20),
            OutputStagingPath = Path.Combine(Path.GetTempPath(), $"nexo-runpod-router-{Guid.NewGuid():N}")
        });
        var localExecutor = new StubLocalExecutor(_ => Result<GenerationExecutionResult>.Success(new GenerationExecutionResult
        {
            Payload = [1],
            Provider = "local",
            ModelId = "m",
            IsRemote = false
        }));
        var runPodClient = new StubRunPodClient();
        var runPodBrick = new RunPodBrick(runPodClient, config, NullLogger<RunPodBrick>.Instance);
        var peerExecutor = new StubPeerExecutor();
        var peerSnapshot = new StubPeerSnapshot();
        return new NcrCapabilityRouter(
            snapshot,
            peerSnapshot,
            peerExecutor,
            localExecutor,
            runPodBrick,
            config,
            NullLogger<NcrCapabilityRouter>.Instance);
    }

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

        public StubLocalExecutor(Func<RunPodJobPayload, Result<GenerationExecutionResult>> handler)
        {
            _handler = handler;
        }

        public Task<Result<GenerationExecutionResult>> ExecuteAsync(
            RunPodJobPayload payload,
            JobRequirements requirements,
            IExecutionContext context,
            CancellationToken cancellationToken = default)
            => Task.FromResult(_handler(payload));
    }

    private sealed class StubRunPodClient : IRunPodClient
    {
        public Result<RunPodInstance> SpinUpResult { get; set; } = Result<RunPodInstance>.Failure("stub.spinup.not_configured", "Spin-up not configured.");
        public Result<JobHandle> DispatchResult { get; set; } = Result<JobHandle>.Failure("stub.dispatch.not_configured", "Dispatch not configured.");
        public Queue<JobStatus> StatusSequence { get; set; } = new();
        public Result<byte[]> PullResult { get; set; } = Result<byte[]>.Failure("stub.pull.not_configured", "Pull not configured.");
        public Result<Unit> TerminateResult { get; set; } = Result<Unit>.Success(Unit.Value);
        public int TerminateCalls { get; private set; }

        public Task<Result<RunPodInstance>> SpinUpInstance(
            string modelId,
            string gpuType,
            CancellationToken cancellationToken = default)
            => Task.FromResult(SpinUpResult);

        public Task<Result<JobHandle>> DispatchJob(
            string instanceId,
            RunPodJobPayload payload,
            CancellationToken cancellationToken = default)
            => Task.FromResult(DispatchResult);

        public Task<Result<JobStatus>> PollJobStatus(
            JobHandle jobHandle,
            CancellationToken cancellationToken = default)
        {
            if (StatusSequence.Count > 0)
            {
                return Task.FromResult(Result<JobStatus>.Success(StatusSequence.Dequeue()));
            }

            return Task.FromResult(Result<JobStatus>.Success(new JobStatus
            {
                State = RunPodJobState.Running
            }));
        }

        public Task<Result<byte[]>> PullResults(
            JobHandle jobHandle,
            CancellationToken cancellationToken = default)
            => Task.FromResult(PullResult);

        public Task<Result<Unit>> TerminateInstance(
            string instanceId,
            CancellationToken cancellationToken = default)
        {
            TerminateCalls++;
            return Task.FromResult(TerminateResult);
        }
    }

    private sealed class TestExecutionContext : IExecutionContext
    {
        public string AgentId { get; init; } = "test-agent";
        public string BehaviorId { get; init; } = "test-behavior";
        public bool IsAirGapped { get; init; }
        public bool AuditMode { get; init; } = true;
        public string Provider { get; init; } = "ollama";
        public IReadOnlyDictionary<string, object> Variables { get; init; } = new Dictionary<string, object>();
    }

    private sealed class StubProviderFactory : IProviderFactory
    {
        public bool IsProviderAvailable(string provider) => true;
        public Task<string> ExecuteLLMAsync(string provider, string systemPrompt, string userPrompt, object config, CancellationToken cancellationToken = default)
            => Task.FromResult("ok");
        public Task<string> ExecuteVisionAsync(string provider, string systemPrompt, string userPrompt, byte[] imageBytes, object config, CancellationToken cancellationToken = default)
            => Task.FromResult("ok");
        public Task<string> ExecuteVisionMultiFrameAsync(string provider, string systemPrompt, string userPrompt, IReadOnlyList<byte[]> frameBytes, object config, CancellationToken cancellationToken = default)
            => Task.FromResult("ok");
        public Task<string> ExecuteVideoAsync(string systemPrompt, string userPrompt, IReadOnlyList<byte[]> frameBytes, object config, CancellationToken cancellationToken = default)
            => Task.FromResult("ok");
        public Task EnsureOllamaReachableAsync(bool requireVisionModel, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    private sealed class StubPeerExecutor : IPeerExecutor
    {
        public Result<GenerationExecutionResult> NextResult { get; set; } =
            Result<GenerationExecutionResult>.Failure("peer-routing.stub", "Stub peer executor should not be invoked in router-only tests.");

        public Task<Result<GenerationExecutionResult>> ExecuteAsync(
            RunPodJobPayload payload,
            JobRequirements requirements,
            IExecutionContext context,
            CancellationToken cancellationToken = default)
            => Task.FromResult(NextResult);
    }
}
