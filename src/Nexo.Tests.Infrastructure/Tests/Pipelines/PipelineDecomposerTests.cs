using FluentAssertions;
using Nexo.Core.Application.Pipelines.Models;
using Nexo.Infrastructure.Pipelines;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.Pipelines;

public sealed class PipelineDecomposerTests
{
    [Fact]
    public void Decompose_WhenTemplateIsInvalid_ThrowsWithValidationErrors()
    {
        var validator = new PipelineTemplateValidator();
        var sut = new PipelineDecomposer(validator);
        var template = new PipelineTemplate
        {
            TemplateId = "invalid",
            Stages = Array.Empty<PipelineStageDefinition>(),
            Edges = Array.Empty<PipelineEdge>()
        };

        Action act = () => sut.Decompose(template);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Template validation failed*At least one stage is required*");
    }

    [Fact]
    public void Decompose_WhenTemplateIsValid_ProducesExecutionGraphWithConnectivity()
    {
        var validator = new PipelineTemplateValidator();
        var sut = new PipelineDecomposer(validator);
        var template = new PipelineTemplate
        {
            TemplateId = "compose-ok",
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
                    Id = "fanout-a",
                    Name = "Fanout A",
                    StageType = PipelineStageType.FanOut,
                    Mode = PipelineExecutionMode.Agentic
                },
                new PipelineStageDefinition
                {
                    Id = "fanin",
                    Name = "FanIn",
                    StageType = PipelineStageType.FanIn,
                    Mode = PipelineExecutionMode.Hybrid,
                    JoinStrategy = PipelineJoinStrategy.Quorum,
                    QuorumCount = 1,
                    FallbackChain = new[] { PipelineWorkerType.Deterministic, PipelineWorkerType.Agentic }
                }
            },
            Edges = new[]
            {
                new PipelineEdge("ingest", "fanout-a"),
                new PipelineEdge("fanout-a", "fanin")
            }
        };

        var graph = sut.Decompose(template);

        graph.TemplateId.Should().Be("compose-ok");
        graph.Nodes.Should().HaveCount(3);

        var ingest = graph.Nodes.Single(n => n.StageId == "ingest");
        ingest.Predecessors.Should().BeEmpty();
        ingest.Successors.Should().ContainSingle("fanout-a");
        ingest.FallbackChain.Should().BeEmpty();

        var fanin = graph.Nodes.Single(n => n.StageId == "fanin");
        fanin.Predecessors.Should().ContainSingle("fanout-a");
        fanin.Successors.Should().BeEmpty();
        fanin.JoinStrategy.Should().Be(PipelineJoinStrategy.Quorum);
        fanin.QuorumCount.Should().Be(1);
        fanin.FallbackChain.Should().ContainInOrder(PipelineWorkerType.Deterministic, PipelineWorkerType.Agentic);
    }
}
