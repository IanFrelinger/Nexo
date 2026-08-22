using FluentAssertions;
using Ashlar.Core.Application.Rollback.Ports;
using Ashlar.Infrastructure.Rollback;
using Xunit;

namespace Ashlar.Tests.Infrastructure.Tests.Rollback;

/// <summary>Tests for dependency graph.</summary>
public sealed class DependencyGraphTests
{
    [Fact]
    public void GetTransitiveDependents_FindsAllDownstream()
    {
        var graph = new DependencyGraph();
        graph.Register("A", new[] { "B" });
        graph.Register("B", new[] { "C" });
        graph.Register("C", Array.Empty<string>());

        var dependents = graph.GetTransitiveDependents("C").ToList();

        dependents.Should().Contain("B");
        dependents.Should().Contain("A");
        dependents.Should().HaveCount(2);
    }

    [Fact]
    public void HasCycles_DetectsCycle()
    {
        var graph = new DependencyGraph();
        graph.Register("A", new[] { "B" });
        graph.Register("B", new[] { "C" });
        graph.Register("C", new[] { "A" });

        graph.HasCycles().Should().BeTrue();
    }

    [Fact]
    public void HasCycles_ClearOnAcyclicGraph()
    {
        var graph = new DependencyGraph();
        graph.Register("A", new[] { "B" });
        graph.Register("B", new[] { "C" });
        graph.Register("C", Array.Empty<string>());

        graph.HasCycles().Should().BeFalse();
    }

    [Fact]
    public void GetDependents_ReturnsDirectDependents()
    {
        var graph = new DependencyGraph();
        graph.Register("A", new[] { "B", "C" });

        var dependents = graph.GetDependents("B").ToList();
        dependents.Should().Contain("A");
        dependents.Should().HaveCount(1);
    }

    [Fact]
    public void Remove_RemovesComponent()
    {
        var graph = new DependencyGraph();
        graph.Register("A", new[] { "B" });
        graph.Remove("A");

        graph.GetDependencies("A").Should().BeEmpty();
        graph.GetDependents("B").Should().NotContain("A");
    }
}
