using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Nexo.Core.Application.Pipelines.Models;
using Nexo.Core.Application.Pipelines.Ports;
using Nexo.Core.Domain;
using Nexo.Infrastructure.Pipelines;
using Nexo.Tests.Infrastructure.Helpers;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.Pipelines;

/// <summary>
/// E2E lifecycle tests for the pipeline system. Covers defaults alignment with
/// <see cref="NexoDefaults"/>, happy path, resume, concurrent execution, store
/// persistence, and fan-in topology.
/// </summary>
[Trait("Category", "E2E")]
public sealed class PipelineLifecycleE2ETests
{
    private static PipelineOrchestrator BuildOrchestrator(
        out IPipelineRunStore runStore,
        PipelineExecutionOptions? executionOptions = null)
    {
        runStore = new InMemoryPipelineRunStore();
        var validator = new PipelineTemplateValidator();
        var decomposer = new PipelineDecomposer(validator);
        var scheduler = new PipelineScheduler();
        var scaling = new ThresholdScalingPolicy();
        var options = executionOptions ?? new PipelineExecutionOptions();
        var adapters = new IPipelineStageExecutionAdapter[]
        {
            new TestDeterministicAdapter(),
            new TestAgenticAdapter()
        };
        var adapterOptions = Options.Create(new PipelineExecutionAdapterOptions
        {
            DeterministicAdapter = "default",
            AgenticAdapter = "default"
        });
        var executionOptionsValue = Options.Create(options);
        var executors = new IPipelineStageExecutor[]
        {
            new DeterministicPipelineStageExecutor(
                NullLogger<DeterministicPipelineStageExecutor>.Instance,
                adapters, adapterOptions, executionOptionsValue),
            new AgenticPipelineStageExecutor(
                NullLogger<AgenticPipelineStageExecutor>.Instance,
                adapters, adapterOptions, executionOptionsValue)
        };
        return new PipelineOrchestrator(
            validator, decomposer, scheduler, runStore, scaling, executors,
            Options.Create(options),
            NullLogger<PipelineOrchestrator>.Instance);
    }

    private static PipelineTemplate SingleStageTemplate(string id) =>
        new()
        {
            TemplateId = id,
            Stages = new[]
            {
                new PipelineStageDefinition { Id = "stage1", Name = "Stage1", Mode = PipelineExecutionMode.Deterministic }
            },
            Edges = Array.Empty<PipelineEdge>()
        };

    [Fact(Timeout = TestTimeouts.E2E)]
    public void DefaultOptions_UseNexoDefaults()
    {
        var opts = new PipelineExecutionOptions();
        opts.MaxRetryAttempts.Should().Be(NexoDefaults.PipelineMaxRetryAttempts);
        opts.RetryDelayMs.Should().Be(NexoDefaults.PipelineRetryDelayMs);
        opts.ResumeFailedStages.Should().BeTrue();
        opts.CompletionPolicy.Should().Be(PipelineCompletionPolicy.FailOnAnyStageFailure);
    }

    [Fact(Timeout = TestTimeouts.E2E)]
    public async Task HappyPath_SingleStage_Completes()
    {
        var orchestrator = BuildOrchestrator(out var store, new PipelineExecutionOptions { RetryDelayMs = 1 });
        var run = await orchestrator.RunAsync(new PipelineExecutionRequest
        {
            RunId = "run-happy-lifecycle",
            Template = SingleStageTemplate("happy-lifecycle")
        });

        run.State.Should().Be(PipelineRunState.Completed);
        var saved = await store.GetAsync("run-happy-lifecycle");
        saved.Should().NotBeNull();
        saved!.State.Should().Be(PipelineRunState.Completed);
    }

    [Fact(Timeout = TestTimeouts.E2E)]
    public async Task MultiStage_LinearPipeline_AllComplete()
    {
        var orchestrator = BuildOrchestrator(out _, new PipelineExecutionOptions { RetryDelayMs = 1 });
        var template = new PipelineTemplate
        {
            TemplateId = "linear-lifecycle",
            Stages = new[]
            {
                new PipelineStageDefinition { Id = "a", Name = "A", Mode = PipelineExecutionMode.Deterministic },
                new PipelineStageDefinition { Id = "b", Name = "B", Mode = PipelineExecutionMode.Agentic },
                new PipelineStageDefinition { Id = "c", Name = "C", Mode = PipelineExecutionMode.Deterministic }
            },
            Edges = new[] { new PipelineEdge("a", "b"), new PipelineEdge("b", "c") }
        };

        var run = await orchestrator.RunAsync(new PipelineExecutionRequest
        {
            RunId = "run-linear-lifecycle",
            Template = template
        });

        run.State.Should().Be(PipelineRunState.Completed);
        run.StageRuns.Should().HaveCount(3);
        run.StageRuns.Should().OnlyContain(s => s.State == PipelineStageRunState.Completed);
    }

    [Fact(Timeout = TestTimeouts.E2E)]
    public async Task MissingResumeSource_WhenNotAllowed_Throws()
    {
        var orchestrator = BuildOrchestrator(out _, new PipelineExecutionOptions
        {
            AllowMissingResumeSource = false, RetryDelayMs = 1
        });

        var act = async () => await orchestrator.RunAsync(new PipelineExecutionRequest
        {
            RunId = "run-missing-lifecycle",
            Template = SingleStageTemplate("missing-lifecycle"),
            ResumeRunId = "nonexistent-run"
        });

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact(Timeout = TestTimeouts.E2E)]
    public async Task MissingResumeSource_WhenAllowed_StartsFreshRun()
    {
        var orchestrator = BuildOrchestrator(out _, new PipelineExecutionOptions
        {
            AllowMissingResumeSource = true, RetryDelayMs = 1
        });

        var run = await orchestrator.RunAsync(new PipelineExecutionRequest
        {
            RunId = "run-fresh-lifecycle",
            Template = SingleStageTemplate("fresh-lifecycle"),
            ResumeRunId = "nonexistent-run"
        });

        run.State.Should().Be(PipelineRunState.Completed);
    }

    [Fact(Timeout = TestTimeouts.E2E)]
    public async Task ConcurrentPipelineRuns_DoNotInterfere()
    {
        var orchestrator = BuildOrchestrator(out var store, new PipelineExecutionOptions { RetryDelayMs = 1 });
        var tasks = Enumerable.Range(0, 10).Select(i =>
            orchestrator.RunAsync(new PipelineExecutionRequest
            {
                RunId = $"run-concurrent-lifecycle-{i}",
                Template = SingleStageTemplate($"concurrent-lifecycle-{i}")
            }));

        var runs = await Task.WhenAll(tasks);
        runs.Should().AllSatisfy(r => r.State.Should().Be(PipelineRunState.Completed));

        for (var i = 0; i < 10; i++)
        {
            var saved = await store.GetAsync($"run-concurrent-lifecycle-{i}");
            saved.Should().NotBeNull();
        }
    }

    [Fact(Timeout = TestTimeouts.E2E)]
    public async Task FanInPipeline_JoinAll_CompletesAllBranches()
    {
        var orchestrator = BuildOrchestrator(out _, new PipelineExecutionOptions { RetryDelayMs = 1 });
        var template = new PipelineTemplate
        {
            TemplateId = "fan-in-lifecycle",
            Stages = new[]
            {
                new PipelineStageDefinition { Id = "src1", Name = "Src1", Mode = PipelineExecutionMode.Deterministic },
                new PipelineStageDefinition { Id = "src2", Name = "Src2", Mode = PipelineExecutionMode.Deterministic },
                new PipelineStageDefinition
                {
                    Id = "merge", Name = "Merge",
                    StageType = PipelineStageType.FanIn,
                    JoinStrategy = PipelineJoinStrategy.All,
                    Mode = PipelineExecutionMode.Hybrid,
                    FallbackChain = new[] { PipelineWorkerType.Deterministic, PipelineWorkerType.Agentic }
                }
            },
            Edges = new[] { new PipelineEdge("src1", "merge"), new PipelineEdge("src2", "merge") }
        };

        var run = await orchestrator.RunAsync(new PipelineExecutionRequest
        {
            RunId = "run-fan-in-lifecycle",
            Template = template
        });

        run.State.Should().Be(PipelineRunState.Completed);
        run.StageRuns.Should().HaveCount(3);
    }

    [Fact(Timeout = TestTimeouts.E2E)]
    public async Task StoreRetrieval_AfterCompletion_ReturnsCorrectData()
    {
        var orchestrator = BuildOrchestrator(out var store, new PipelineExecutionOptions { RetryDelayMs = 1 });
        await orchestrator.RunAsync(new PipelineExecutionRequest
        {
            RunId = "run-persist-lifecycle",
            Template = SingleStageTemplate("persist-lifecycle")
        });

        var saved = await store.GetAsync("run-persist-lifecycle");
        saved.Should().NotBeNull();
        saved!.RunId.Should().Be("run-persist-lifecycle");
        saved.TemplateId.Should().Be("persist-lifecycle");
        saved.State.Should().Be(PipelineRunState.Completed);
        saved.StageRuns.Should().ContainSingle(s => s.StageId == "stage1");
    }

    [Fact(Timeout = TestTimeouts.E2E)]
    public void CompletionPolicy_EnumValues()
    {
        Enum.GetValues<PipelineCompletionPolicy>().Should().HaveCount(2);
    }

    // ── Test adapters ────────────────────────────────────────────────

    private sealed class TestDeterministicAdapter : IPipelineStageExecutionAdapter
    {
        public string AdapterKey => "default";
        public PipelineWorkerType WorkerType => PipelineWorkerType.Deterministic;
        public Task<PipelineStageExecutionResult> ExecuteAsync(PipelineStageExecutionRequest request, CancellationToken ct = default) =>
            Task.FromResult(new PipelineStageExecutionResult { Succeeded = true, WorkerId = "det", Output = $"det:{request.StageId}" });
    }

    private sealed class TestAgenticAdapter : IPipelineStageExecutionAdapter
    {
        public string AdapterKey => "default";
        public PipelineWorkerType WorkerType => PipelineWorkerType.Agentic;
        public Task<PipelineStageExecutionResult> ExecuteAsync(PipelineStageExecutionRequest request, CancellationToken ct = default) =>
            Task.FromResult(new PipelineStageExecutionResult { Succeeded = true, WorkerId = "agt", Output = $"agt:{request.StageId}" });
    }
}
