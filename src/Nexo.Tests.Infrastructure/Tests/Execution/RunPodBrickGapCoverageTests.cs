using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Nexo.Core.Application.Execution.Routing;
using Nexo.Core.Domain.Bricks;
using Nexo.Core.Domain.Execution;
using Nexo.Infrastructure.Execution.Routing;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.Execution;

/// <summary>Tests for run pod brick gap coverage.</summary>
public sealed class RunPodBrickGapCoverageTests
{
    [Fact]
    public async Task ExecuteAsync_via_brick_input_maps_success_output()
    {
        var outputDir = Path.Combine(Path.GetTempPath(), $"nexo-runpod-gap-{Guid.NewGuid():N}");
        var client = new StubRunPodClient
        {
            SpinUpResult = Result<RunPodInstance>.Success(new RunPodInstance
            {
                InstanceId = "inst-ok",
                ModelId = "m1",
                GpuType = "gpu",
            }),
            DispatchResult = Result<JobHandle>.Success(new JobHandle
            {
                InstanceId = "inst-ok",
                JobId = "job-ok",
            }),
            StatusSequence = new Queue<JobStatus>([new JobStatus { State = RunPodJobState.Completed }]),
            PullResult = Result<byte[]>.Success([1, 2, 3]),
        };
        var brick = new RunPodBrick(
            client,
            Options.Create(new RunPodBrickConfig { OutputStagingPath = outputDir }),
            NullLogger<RunPodBrick>.Instance);

        var input = new BrickInput();
        input.Set("modelId", "m1");
        input.Set("prompt", "draw");
        input.Set("computeClass", GpuComputeClass.Low.ToString());
        input.Set("minimumVramBytes", 0L);

        var output = await brick.ExecuteAsync(
            input,
            ImplementationType.Deterministic,
            new GapExecutionContext(),
            CancellationToken.None);

        output.Get<bool>("success").Should().BeTrue();
        output.Get<byte[]>("payload").Should().Equal([1, 2, 3]);
        output.Get<string>("outputPath").Should().NotBeNullOrWhiteSpace();
        client.TerminateCalls.Should().Be(1);

        if (Directory.Exists(outputDir))
            Directory.Delete(outputDir, recursive: true);
    }

    [Fact]
    public async Task ExecuteAsync_returns_failure_when_job_terminates_in_failed_state()
    {
        var client = new StubRunPodClient
        {
            SpinUpResult = Result<RunPodInstance>.Success(new RunPodInstance
            {
                InstanceId = "inst-fail",
                ModelId = "m1",
                GpuType = "gpu",
            }),
            DispatchResult = Result<JobHandle>.Success(new JobHandle
            {
                InstanceId = "inst-fail",
                JobId = "job-fail",
            }),
            StatusSequence = new Queue<JobStatus>([
                new JobStatus { State = RunPodJobState.Failed, Message = "gpu OOM" },
            ]),
        };
        var brick = new RunPodBrick(
            client,
            Options.Create(new RunPodBrickConfig
            {
                Timeout = TimeSpan.FromSeconds(2),
                PollingInterval = TimeSpan.FromMilliseconds(10),
            }),
            NullLogger<RunPodBrick>.Instance);

        var result = await brick.ExecuteAsync(
            new RunPodJobPayload { ModelId = "m1", Prompt = "fail" },
            new JobRequirements { ModelId = "m1" },
            new GapExecutionContext());

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("runpod.job_failed");
        client.TerminateCalls.Should().Be(1);
    }

    [Fact]
    public async Task ExecuteAsync_returns_cancelled_when_token_is_cancelled()
    {
        var client = new StubRunPodClient
        {
            SpinUpResult = Result<RunPodInstance>.Success(new RunPodInstance
            {
                InstanceId = "inst-cancel",
                ModelId = "m1",
                GpuType = "gpu",
            }),
            DispatchResult = Result<JobHandle>.Success(new JobHandle
            {
                InstanceId = "inst-cancel",
                JobId = "job-cancel",
            }),
            StatusSequence = new Queue<JobStatus>([new JobStatus { State = RunPodJobState.Running }]),
        };
        var brick = new RunPodBrick(
            client,
            Options.Create(new RunPodBrickConfig
            {
                Timeout = TimeSpan.FromSeconds(30),
                PollingInterval = TimeSpan.FromMilliseconds(50),
            }),
            NullLogger<RunPodBrick>.Instance);

        using var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromMilliseconds(20));

        var result = await brick.ExecuteAsync(
            new RunPodJobPayload { ModelId = "m1", Prompt = "cancel" },
            new JobRequirements { ModelId = "m1" },
            new GapExecutionContext(),
            cts.Token);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("runpod.cancelled");
    }

    [Fact]
    public async Task ExecuteAsync_surfaces_unexpected_client_exceptions()
    {
        var client = new ThrowingRunPodClient();
        var brick = new RunPodBrick(
            client,
            Options.Create(new RunPodBrickConfig()),
            NullLogger<RunPodBrick>.Instance);

        var result = await brick.ExecuteAsync(
            new RunPodJobPayload { ModelId = "m1", Prompt = "boom" },
            new JobRequirements { ModelId = "m1" },
            new GapExecutionContext());

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("runpod.unexpected");
    }

    [Fact]
    public async Task ExecuteAsync_via_brick_input_maps_failure_output()
    {
        var client = new StubRunPodClient
        {
            SpinUpResult = Result<RunPodInstance>.Failure("runpod.spinup", "no gpu"),
        };
        var brick = new RunPodBrick(client, Options.Create(new RunPodBrickConfig()), NullLogger<RunPodBrick>.Instance);
        var input = new BrickInput();
        input.Set("modelId", "m1");
        input.Set("prompt", "fail");

        var output = await brick.ExecuteAsync(input, ImplementationType.Deterministic, new GapExecutionContext());

        output.Get<bool>("success").Should().BeFalse();
        output.Get<string>("errorCode").Should().Be("runpod.spinup");
    }

    [Fact]
    public async Task ExecuteAsync_pull_failure_after_completion_still_tears_down()
    {
        var client = new StubRunPodClient
        {
            SpinUpResult = Result<RunPodInstance>.Success(new RunPodInstance { InstanceId = "inst-pull", ModelId = "m1", GpuType = "gpu" }),
            DispatchResult = Result<JobHandle>.Success(new JobHandle { InstanceId = "inst-pull", JobId = "job-pull" }),
            StatusSequence = new Queue<JobStatus>([new JobStatus { State = RunPodJobState.Completed }]),
            PullResult = Result<byte[]>.Failure("runpod.pull", "missing artifact"),
        };

        var brick = new RunPodBrick(client, Options.Create(new RunPodBrickConfig()), NullLogger<RunPodBrick>.Instance);
        var result = await brick.ExecuteAsync(
            new RunPodJobPayload { ModelId = "m1", Prompt = "pull fail" },
            new JobRequirements { ModelId = "m1" },
            new GapExecutionContext());

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("runpod.pull");
        client.TerminateCalls.Should().Be(1);
    }

    /// <summary>Tests for gap execution context.</summary>
    private sealed class GapExecutionContext : IExecutionContext
    {
        /// <summary>Agent id.</summary>
        public string AgentId { get; init; } = "gap-agent";
        /// <summary>Behavior id.</summary>
        public string BehaviorId { get; init; } = "gap-behavior";
        /// <summary>Is air gapped.</summary>
        public bool IsAirGapped { get; init; }
        /// <summary>Audit mode.</summary>
        public bool AuditMode { get; init; } = true;
        /// <summary>Provider.</summary>
        public string Provider { get; init; } = "local";
        /// <summary>Variables.</summary>
        public IReadOnlyDictionary<string, object> Variables { get; init; } = new Dictionary<string, object>();
    }

    /// <summary>Tests for stub run pod client.</summary>
    private sealed class StubRunPodClient : IRunPodClient
    {
        /// <summary>Spin up result.</summary>
        public Result<RunPodInstance> SpinUpResult { get; set; } =
            Result<RunPodInstance>.Failure("stub.spinup", "Spin-up not configured.");

        /// <summary>Dispatch result.</summary>
        public Result<JobHandle> DispatchResult { get; set; } =
            Result<JobHandle>.Failure("stub.dispatch", "Dispatch not configured.");

        /// <summary>Status sequence.</summary>
        public Queue<JobStatus> StatusSequence { get; set; } = new();
        /// <summary>Pull result.</summary>
        public Result<byte[]> PullResult { get; set; } = Result<byte[]>.Failure("stub.pull", "Pull not configured.");
        /// <summary>Terminate result.</summary>
        public Result<Unit> TerminateResult { get; set; } = Result<Unit>.Success(Unit.Value);
        /// <summary>Terminate calls.</summary>
        public int TerminateCalls { get; private set; }

        /// <summary>Spin up instance.</summary>
        /// <param name="modelId">Model id.</param>
        /// <param name="gpuType">Gpu type.</param>
        /// <param name="default">Default.</param>
        public Task<Result<RunPodInstance>> SpinUpInstance(string modelId, string gpuType, CancellationToken cancellationToken = default) =>
            Task.FromResult(SpinUpResult);

        /// <summary>Dispatch job.</summary>
        /// <param name="instanceId">Instance id.</param>
        /// <param name="payload">Payload.</param>
        /// <param name="default">Default.</param>
        public Task<Result<JobHandle>> DispatchJob(string instanceId, RunPodJobPayload payload, CancellationToken cancellationToken = default) =>
            Task.FromResult(DispatchResult);

        public Task<Result<JobStatus>> PollJobStatus(JobHandle jobHandle, CancellationToken cancellationToken = default)
        {
            if (StatusSequence.Count > 0)
                return Task.FromResult(Result<JobStatus>.Success(StatusSequence.Dequeue()));
            return Task.FromResult(Result<JobStatus>.Success(new JobStatus { State = RunPodJobState.Running }));
        }

        /// <summary>Pull results.</summary>
        /// <param name="jobHandle">Job handle.</param>
        /// <param name="default">Default.</param>
        public Task<Result<byte[]>> PullResults(JobHandle jobHandle, CancellationToken cancellationToken = default) =>
            Task.FromResult(PullResult);

        public Task<Result<Unit>> TerminateInstance(string instanceId, CancellationToken cancellationToken = default)
        {
            TerminateCalls++;
            return Task.FromResult(TerminateResult);
        }
    }

    /// <summary>Tests for throwing run pod client.</summary>
    private sealed class ThrowingRunPodClient : IRunPodClient
    {
        /// <summary>Spin up instance.</summary>
        /// <param name="modelId">Model id.</param>
        /// <param name="gpuType">Gpu type.</param>
        /// <param name="default">Default.</param>
        public Task<Result<RunPodInstance>> SpinUpInstance(string modelId, string gpuType, CancellationToken cancellationToken = default) =>
            /// <summary>Invalid operation exception.</summary>
            /// <param name="exploded"">Exploded".</param>
            throw new InvalidOperationException("spinup exploded");

        /// <summary>Dispatch job.</summary>
        /// <param name="instanceId">Instance id.</param>
        /// <param name="payload">Payload.</param>
        /// <param name="default">Default.</param>
        public Task<Result<JobHandle>> DispatchJob(string instanceId, RunPodJobPayload payload, CancellationToken cancellationToken = default) =>
            /// <summary>Invalid operation exception.</summary>
            /// <param name="exploded"">Exploded".</param>
            throw new InvalidOperationException("dispatch exploded");

        /// <summary>Poll job status.</summary>
        /// <param name="jobHandle">Job handle.</param>
        /// <param name="default">Default.</param>
        public Task<Result<JobStatus>> PollJobStatus(JobHandle jobHandle, CancellationToken cancellationToken = default) =>
            /// <summary>Invalid operation exception.</summary>
            /// <param name="exploded"">Exploded".</param>
            throw new InvalidOperationException("poll exploded");

        /// <summary>Pull results.</summary>
        /// <param name="jobHandle">Job handle.</param>
        /// <param name="default">Default.</param>
        public Task<Result<byte[]>> PullResults(JobHandle jobHandle, CancellationToken cancellationToken = default) =>
            /// <summary>Invalid operation exception.</summary>
            /// <param name="exploded"">Exploded".</param>
            throw new InvalidOperationException("pull exploded");

        /// <summary>Terminate instance.</summary>
        /// <param name="instanceId">Instance id.</param>
        /// <param name="default">Default.</param>
        public Task<Result<Unit>> TerminateInstance(string instanceId, CancellationToken cancellationToken = default) =>
            /// <summary>Invalid operation exception.</summary>
            /// <param name="exploded"">Exploded".</param>
            throw new InvalidOperationException("terminate exploded");
    }
}
