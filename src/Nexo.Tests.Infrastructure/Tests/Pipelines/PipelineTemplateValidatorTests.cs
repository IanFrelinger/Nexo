using FluentAssertions;
using Nexo.Core.Application.Pipelines.Models;
using Nexo.Infrastructure.Pipelines;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.Pipelines;

public sealed class PipelineTemplateValidatorTests
{
    private readonly PipelineTemplateValidator _sut = new();

    [Fact]
    public void Validate_WhenTemplateIsNull_ReturnsInvalidResult()
    {
        var result = _sut.Validate(null!);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle().Which.Should().Contain("Template is required");
    }

    [Fact]
    public void Validate_WhenTemplateHasStructuralViolations_ReturnsAllRelevantErrors()
    {
        var template = new PipelineTemplate
        {
            TemplateId = "bad-structure",
            Stages = new[]
            {
                new PipelineStageDefinition
                {
                    Id = "dup",
                    Name = "Duplicate 1"
                },
                new PipelineStageDefinition
                {
                    Id = "dup",
                    Name = "Duplicate 2"
                },
                new PipelineStageDefinition
                {
                    Id = "hybrid-no-fallback",
                    Name = "Hybrid No Fallback",
                    Mode = PipelineExecutionMode.Hybrid
                },
                new PipelineStageDefinition
                {
                    Id = "fanin-missing-join",
                    Name = "FanIn Missing Join",
                    StageType = PipelineStageType.FanIn
                },
                new PipelineStageDefinition
                {
                    Id = "quorum-invalid",
                    Name = "Quorum Invalid",
                    StageType = PipelineStageType.FanIn,
                    JoinStrategy = PipelineJoinStrategy.Quorum,
                    QuorumCount = 0
                },
                new PipelineStageDefinition
                {
                    Id = "critical-no-post-validation",
                    Name = "Critical No Post Validation",
                    Constraints = new PipelineStageConstraints
                    {
                        Critical = true,
                        RequiresPostValidation = false
                    }
                },
                new PipelineStageDefinition
                {
                    Id = "max-parallelism-invalid",
                    Name = "Max Parallelism Invalid",
                    Constraints = new PipelineStageConstraints
                    {
                        MaxParallelism = 0
                    }
                }
            },
            Edges = new[]
            {
                new PipelineEdge("dup", "unknown-target"),
                new PipelineEdge("unknown-source", "dup")
            }
        };

        var result = _sut.Validate(template);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.Contains("Duplicate stage id"));
        result.Errors.Should().Contain(error => error.Contains("must define fallback chain"));
        result.Errors.Should().Contain(error => error.Contains("must define join strategy"));
        result.Errors.Should().Contain(error => error.Contains("must define QuorumCount > 0"));
        result.Errors.Should().Contain(error => error.Contains("must require post validation"));
        result.Errors.Should().Contain(error => error.Contains("MaxParallelism must be greater than 0"));
        result.Errors.Should().Contain(error => error.Contains("unknown source stage"));
        result.Errors.Should().Contain(error => error.Contains("unknown target stage"));
    }

    [Fact]
    public void Validate_WhenGraphContainsCycle_ReturnsCycleError()
    {
        var template = new PipelineTemplate
        {
            TemplateId = "cycle",
            Stages = new[]
            {
                new PipelineStageDefinition { Id = "a", Name = "A" },
                new PipelineStageDefinition { Id = "b", Name = "B" },
                new PipelineStageDefinition { Id = "c", Name = "C" }
            },
            Edges = new[]
            {
                new PipelineEdge("a", "b"),
                new PipelineEdge("b", "c"),
                new PipelineEdge("c", "a")
            }
        };

        var result = _sut.Validate(template);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.Contains("contains at least one cycle"));
    }

    [Fact]
    public void Validate_WhenGraphHasUnreachableStage_ReturnsUnreachableError()
    {
        var template = new PipelineTemplate
        {
            TemplateId = "unreachable",
            Stages = new[]
            {
                new PipelineStageDefinition { Id = "root", Name = "Root" },
                new PipelineStageDefinition { Id = "child", Name = "Child" },
                new PipelineStageDefinition { Id = "isolated", Name = "Isolated" }
            },
            Edges = new[]
            {
                new PipelineEdge("root", "child"),
                new PipelineEdge("isolated", "isolated")
            }
        };

        var result = _sut.Validate(template);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.Contains("Orphan or unreachable stage 'isolated'"));
    }

    [Fact]
    public void Validate_WhenTemplateIsWellFormed_ReturnsValid()
    {
        var template = new PipelineTemplate
        {
            TemplateId = "valid-template",
            Stages = new[]
            {
                new PipelineStageDefinition
                {
                    Id = "ingest",
                    Name = "Ingest",
                    Mode = PipelineExecutionMode.Deterministic
                },
                new PipelineStageDefinition
                {
                    Id = "enrich",
                    Name = "Enrich",
                    Mode = PipelineExecutionMode.Hybrid,
                    FallbackChain = new[] { PipelineWorkerType.Deterministic, PipelineWorkerType.Agentic }
                },
                new PipelineStageDefinition
                {
                    Id = "merge",
                    Name = "Merge",
                    StageType = PipelineStageType.FanIn,
                    JoinStrategy = PipelineJoinStrategy.Quorum,
                    QuorumCount = 1,
                    Constraints = new PipelineStageConstraints
                    {
                        Critical = true,
                        RequiresPostValidation = true,
                        MaxParallelism = 2
                    }
                }
            },
            Edges = new[]
            {
                new PipelineEdge("ingest", "enrich"),
                new PipelineEdge("enrich", "merge")
            }
        };

        var result = _sut.Validate(template);

        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }
}
