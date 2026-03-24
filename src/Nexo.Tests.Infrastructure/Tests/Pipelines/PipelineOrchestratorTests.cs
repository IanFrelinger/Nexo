using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Nexo.Core.Application.Pipelines.Models;
using Nexo.Core.Application.Pipelines.Ports;
using Nexo.Infrastructure.Pipelines;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.Pipelines;

public sealed class PipelineOrchestratorTests
{
    [Fact]
    public async Task RunAsync_HappyPath_CompletesRun()
    {
        var orchestrator = BuildOrchestrator(
            out var runStore,
            executionOptions: new PipelineExecutionOptions
            {
                MaxRetryAttempts = 2,
                RetryDelayMs = 1
            });

        var template = new PipelineTemplate
        {
            TemplateId = "happy-path",
            Stages = new[]
            {
                new PipelineStageDefinition { Id = "ingest", Name = "Ingest", Mode = PipelineExecutionMode.Deterministic },
                new PipelineStageDefinition { Id = "enrich", Name = "Enrich", Mode = PipelineExecutionMode.Agentic },
                new PipelineStageDefinition
                {
                    Id = "merge",
                    Name = "Merge",
                    StageType = PipelineStageType.FanIn,
                    JoinStrategy = PipelineJoinStrategy.All,
                    Mode = PipelineExecutionMode.Hybrid,
                    FallbackChain = new[] { PipelineWorkerType.Deterministic, PipelineWorkerType.Agentic }
                }
            },
            Edges = new[]
            {
                new PipelineEdge("ingest", "enrich"),
                new PipelineEdge("enrich", "merge")
            }
        };

        var run = await orchestrator.RunAsync(new PipelineExecutionRequest
        {
            RunId = "run-happy",
            Template = template
        });

        run.State.Should().Be(PipelineRunState.Completed);
        run.StageRuns.Should().OnlyContain(x => x.State == PipelineStageRunState.Completed);

        var saved = await runStore.GetAsync("run-happy");
        saved.Should().NotBeNull();
        saved!.State.Should().Be(PipelineRunState.Completed);
    }

    [Fact]
    public async Task RunAsync_WhenDeterministicFails_HybridFallsBackToAgentic()
    {
        var orchestrator = BuildOrchestrator(
            out _,
            executionOptions: new PipelineExecutionOptions
            {
                MaxRetryAttempts = 1,
                RetryDelayMs = 1
            });

        var template = new PipelineTemplate
        {
            TemplateId = "fallback",
            Stages = new[]
            {
                new PipelineStageDefinition { Id = "root", Name = "Root", Mode = PipelineExecutionMode.Deterministic },
                new PipelineStageDefinition
                {
                    Id = "hybrid-stage",
                    Name = "Hybrid",
                    Mode = PipelineExecutionMode.Hybrid,
                    FallbackChain = new[] { PipelineWorkerType.Deterministic, PipelineWorkerType.Agentic }
                }
            },
            Edges = new[]
            {
                new PipelineEdge("root", "hybrid-stage")
            }
        };

        var run = await orchestrator.RunAsync(new PipelineExecutionRequest
        {
            RunId = "run-fallback",
            Template = template,
            Inputs = new Dictionary<string, string>
            {
                ["fail:hybrid-stage:deterministic"] = "true"
            }
        });

        run.State.Should().Be(PipelineRunState.Completed);
        var hybrid = run.StageRuns.Single(x => x.StageId == "hybrid-stage");
        hybrid.State.Should().Be(PipelineStageRunState.Completed);
        hybrid.WorkerType.Should().Be(PipelineWorkerType.Agentic);
    }

    [Fact]
    public async Task RunAsync_WhenCriticalStageFails_FailsFast()
    {
        var orchestrator = BuildOrchestrator(
            out _,
            executionOptions: new PipelineExecutionOptions
            {
                MaxRetryAttempts = 1,
                RetryDelayMs = 1
            });

        var template = new PipelineTemplate
        {
            TemplateId = "critical-fail-fast",
            Stages = new[]
            {
                new PipelineStageDefinition
                {
                    Id = "critical",
                    Name = "Critical",
                    Mode = PipelineExecutionMode.Deterministic,
                    Constraints = new PipelineStageConstraints
                    {
                        Critical = true,
                        RequiresPostValidation = true
                    }
                },
                new PipelineStageDefinition
                {
                    Id = "downstream",
                    Name = "Downstream",
                    Mode = PipelineExecutionMode.Agentic
                }
            },
            Edges = new[]
            {
                new PipelineEdge("critical", "downstream")
            }
        };

        var run = await orchestrator.RunAsync(new PipelineExecutionRequest
        {
            RunId = "run-critical",
            Template = template,
            Inputs = new Dictionary<string, string>
            {
                ["fail:critical:deterministic"] = "true"
            }
        });

        run.State.Should().Be(PipelineRunState.Failed);
        var critical = run.StageRuns.Single(x => x.StageId == "critical");
        critical.State.Should().Be(PipelineStageRunState.Failed);
        var downstream = run.StageRuns.Single(x => x.StageId == "downstream");
        downstream.State.Should().Be(PipelineStageRunState.Pending);
    }

    [Fact]
    public async Task RunAsync_WithResumeRunId_CarriesForwardCompletedStages()
    {
        var orchestrator = BuildOrchestrator(
            out var runStore,
            executionOptions: new PipelineExecutionOptions
            {
                MaxRetryAttempts = 1,
                RetryDelayMs = 1
            });

        var template = new PipelineTemplate
        {
            TemplateId = "resume-template",
            Stages = new[]
            {
                new PipelineStageDefinition { Id = "a", Name = "A", Mode = PipelineExecutionMode.Deterministic },
                new PipelineStageDefinition { Id = "b", Name = "B", Mode = PipelineExecutionMode.Agentic },
                new PipelineStageDefinition { Id = "c", Name = "C", Mode = PipelineExecutionMode.Deterministic }
            },
            Edges = new[]
            {
                new PipelineEdge("a", "b"),
                new PipelineEdge("b", "c")
            }
        };

        await runStore.SaveAsync(new PipelineRun
        {
            RunId = "prior-run",
            TemplateId = "resume-template",
            State = PipelineRunState.Failed,
            StageRuns = new[]
            {
                new PipelineStageRun
                {
                    StageId = "a",
                    State = PipelineStageRunState.Completed,
                    Attempt = 1,
                    WorkerType = PipelineWorkerType.Deterministic
                },
                new PipelineStageRun
                {
                    StageId = "b",
                    State = PipelineStageRunState.Failed,
                    Attempt = 1,
                    WorkerType = PipelineWorkerType.Agentic,
                    Error = "prior failure"
                },
                new PipelineStageRun
                {
                    StageId = "c",
                    State = PipelineStageRunState.Pending,
                    Attempt = 0
                }
            }
        });

        var run = await orchestrator.RunAsync(new PipelineExecutionRequest
        {
            RunId = "resumed-run",
            ResumeRunId = "prior-run",
            ResumeFailedStages = true,
            Template = template
        });

        run.State.Should().Be(PipelineRunState.Completed);
        run.StageRuns.Should().OnlyContain(x => x.State == PipelineStageRunState.Completed);
        run.StageRuns.Single(x => x.StageId == "a").Attempt.Should().Be(1);
    }

    [Fact]
    public async Task RunAsync_WhenFanInFirstSuccess_ProceedsAfterSingleCompletedPredecessor()
    {
        var orchestrator = BuildOrchestrator(
            out _,
            executionOptions: new PipelineExecutionOptions
            {
                MaxRetryAttempts = 1,
                RetryDelayMs = 1
            });

        var template = new PipelineTemplate
        {
            TemplateId = "fanin-first-success",
            Stages = new[]
            {
                new PipelineStageDefinition { Id = "a", Name = "A", Mode = PipelineExecutionMode.Deterministic },
                new PipelineStageDefinition { Id = "b", Name = "B", Mode = PipelineExecutionMode.Deterministic },
                new PipelineStageDefinition
                {
                    Id = "join",
                    Name = "Join",
                    StageType = PipelineStageType.FanIn,
                    JoinStrategy = PipelineJoinStrategy.FirstSuccess,
                    Mode = PipelineExecutionMode.Deterministic
                }
            },
            Edges = new[]
            {
                new PipelineEdge("a", "join"),
                new PipelineEdge("b", "join")
            }
        };

        var run = await orchestrator.RunAsync(new PipelineExecutionRequest
        {
            RunId = "run-fanin-first",
            Template = template,
            Inputs = new Dictionary<string, string>
            {
                ["fail:b:deterministic"] = "true"
            }
        });

        run.State.Should().Be(PipelineRunState.Completed);
        run.StageRuns.Single(x => x.StageId == "join").State.Should().Be(PipelineStageRunState.Completed);
    }

    private static PipelineOrchestrator BuildOrchestrator(
        out IPipelineRunStore runStore,
        PipelineExecutionOptions? executionOptions = null)
    {
        runStore = new InMemoryPipelineRunStore();
        var validator = new PipelineTemplateValidator();
        var decomposer = new PipelineDecomposer(validator);
        var scheduler = new PipelineScheduler();
        var scaling = new ThresholdScalingPolicy();
        var executors = new IPipelineStageExecutor[]
        {
            new DeterministicPipelineStageExecutor(NullLogger<DeterministicPipelineStageExecutor>.Instance),
            new AgenticPipelineStageExecutor(NullLogger<AgenticPipelineStageExecutor>.Instance)
        };
        var options = Options.Create(executionOptions ?? new PipelineExecutionOptions());
        return new PipelineOrchestrator(
            validator,
            decomposer,
            scheduler,
            runStore,
            scaling,
            executors,
            options,
            NullLogger<PipelineOrchestrator>.Instance);
    }
}
