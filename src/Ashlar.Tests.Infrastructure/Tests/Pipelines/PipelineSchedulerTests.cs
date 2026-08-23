using FluentAssertions;
using Ashlar.Core.Application.Pipelines.Models;
using Ashlar.Infrastructure.Pipelines;
using Xunit;

namespace Ashlar.Tests.Infrastructure.Tests.Pipelines;

/// <summary>Tests for pipeline scheduler.</summary>
public sealed class PipelineSchedulerTests
{
    private static PipelineExecutionGraph BuildGraph()
    {
        return new PipelineExecutionGraph
        {
            TemplateId = "scheduler-graph",
            Nodes = new[]
            {
                new PipelineExecutionNode
                {
                    StageId = "root",
                    StageType = PipelineStageType.Standard,
                    Predecessors = Array.Empty<string>(),
                    Successors = new[] { "fanin-first", "fanin-quorum", "fanin-all" }
                },
                new PipelineExecutionNode
                {
                    StageId = "peer",
                    StageType = PipelineStageType.Standard,
                    Predecessors = Array.Empty<string>(),
                    Successors = new[] { "fanin-first", "fanin-quorum", "fanin-all" }
                },
                new PipelineExecutionNode
                {
                    StageId = "fanin-first",
                    StageType = PipelineStageType.FanIn,
                    JoinStrategy = PipelineJoinStrategy.FirstSuccess,
                    Predecessors = new[] { "root", "peer" },
                    Successors = Array.Empty<string>()
                },
                new PipelineExecutionNode
                {
                    StageId = "fanin-quorum",
                    StageType = PipelineStageType.FanIn,
                    JoinStrategy = PipelineJoinStrategy.Quorum,
                    QuorumCount = 2,
                    Predecessors = new[] { "root", "peer" },
                    Successors = Array.Empty<string>()
                },
                new PipelineExecutionNode
                {
                    StageId = "fanin-all",
                    StageType = PipelineStageType.FanIn,
                    JoinStrategy = PipelineJoinStrategy.All,
                    Predecessors = new[] { "root", "peer" },
                    Successors = Array.Empty<string>()
                },
                new PipelineExecutionNode
                {
                    StageId = "standard-downstream",
                    StageType = PipelineStageType.Standard,
                    Predecessors = new[] { "fanin-all" },
                    Successors = Array.Empty<string>()
                }
            }
        };
    }

    [Fact]
    public void GetReadyStages_WhenRootStagesPending_ReturnsRoots()
    {
        var sut = new PipelineScheduler();
        var graph = BuildGraph();
        var states = graph.Nodes.ToDictionary(n => n.StageId, _ => PipelineStageRunState.Pending);

        var ready = sut.GetReadyStages(graph, states);

        Assert.Contains("root", ready);
        Assert.Contains("peer", ready);
        Assert.DoesNotContain("fanin-first", ready);
    }

    [Fact]
    public void GetReadyStages_WhenFirstSuccessSatisfied_ReleasesFirstSuccessNode()
    {
        var sut = new PipelineScheduler();
        var graph = BuildGraph();
        var states = graph.Nodes.ToDictionary(n => n.StageId, _ => PipelineStageRunState.Pending);
        states["root"] = PipelineStageRunState.Completed;

        var ready = sut.GetReadyStages(graph, states);

        Assert.Contains("fanin-first", ready);
        Assert.DoesNotContain("fanin-quorum", ready);
        Assert.DoesNotContain("fanin-all", ready);
    }

    [Fact]
    public void GetReadyStages_WhenQuorumSatisfied_ReleasesQuorumNode()
    {
        var sut = new PipelineScheduler();
        var graph = BuildGraph();
        var states = graph.Nodes.ToDictionary(n => n.StageId, _ => PipelineStageRunState.Pending);
        states["root"] = PipelineStageRunState.Completed;
        states["peer"] = PipelineStageRunState.Completed;

        var ready = sut.GetReadyStages(graph, states);

        ready.Should().Contain("fanin-quorum");
        ready.Should().Contain("fanin-all");
    }

    [Fact]
    public void GetReadyStages_WhenFanInAllCompleted_ReleasesStandardDownstream()
    {
        var sut = new PipelineScheduler();
        var graph = BuildGraph();
        var states = graph.Nodes.ToDictionary(n => n.StageId, _ => PipelineStageRunState.Pending);
        states["fanin-all"] = PipelineStageRunState.Completed;

        var ready = sut.GetReadyStages(graph, states);

        ready.Should().Contain("standard-downstream");
    }
}
