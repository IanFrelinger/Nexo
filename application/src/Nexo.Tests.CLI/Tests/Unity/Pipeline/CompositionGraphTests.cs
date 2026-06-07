using System.Text.Json;
using FluentAssertions;
using Nexo.CLI.Unity.Pipeline;
using Xunit;

namespace Nexo.Tests.CLI.Tests.Unity.Pipeline;

public sealed class CompositionGraphTests : IDisposable
{
    private readonly string _tempDir = Path.Combine(Path.GetTempPath(), $"nexo-compose-{Guid.NewGuid():N}");

    public CompositionGraphTests()
    {
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, true);
    }

    [Fact]
    public void GetExecutionOrder_LinearDeps_ReturnsTopologicalOrder()
    {
        var graph = new CompositionGraph
        {
            Systems = new[]
            {
                new CompositionNode { Id = "C", Prompt = "System C", Depends = new[] { "B" } },
                new CompositionNode { Id = "B", Prompt = "System B", Depends = new[] { "A" } },
                new CompositionNode { Id = "A", Prompt = "System A", Depends = Array.Empty<string>() }
            }
        };

        var order = graph.GetExecutionOrder();

        order.Should().HaveCount(3);
        order[0].Id.Should().Be("A");
        order[1].Id.Should().Be("B");
        order[2].Id.Should().Be("C");
    }

    [Fact]
    public void GetExecutionOrder_ParallelDeps_ResolvesCorrectly()
    {
        var graph = new CompositionGraph
        {
            Systems = new[]
            {
                new CompositionNode { Id = "A", Prompt = "System A", Depends = Array.Empty<string>() },
                new CompositionNode { Id = "B", Prompt = "System B", Depends = Array.Empty<string>() },
                new CompositionNode { Id = "C", Prompt = "System C", Depends = new[] { "A", "B" } }
            }
        };

        var order = graph.GetExecutionOrder();

        order.Should().HaveCount(3);
        var cIndex = order.ToList().FindIndex(n => n.Id == "C");
        var aIndex = order.ToList().FindIndex(n => n.Id == "A");
        var bIndex = order.ToList().FindIndex(n => n.Id == "B");
        cIndex.Should().BeGreaterThan(aIndex);
        cIndex.Should().BeGreaterThan(bIndex);
    }

    [Fact]
    public void GetExecutionOrder_CircularDependency_Throws()
    {
        var graph = new CompositionGraph
        {
            Systems = new[]
            {
                new CompositionNode { Id = "A", Prompt = "A", Depends = new[] { "B" } },
                new CompositionNode { Id = "B", Prompt = "B", Depends = new[] { "A" } }
            }
        };

        var act = () => graph.GetExecutionOrder();
        act.Should().Throw<InvalidOperationException>()
           .WithMessage("*Circular dependency*");
    }

    [Fact]
    public void GetExecutionOrder_NoDeps_ReturnsOriginalOrder()
    {
        var graph = new CompositionGraph
        {
            Systems = new[]
            {
                new CompositionNode { Id = "X", Prompt = "X" },
                new CompositionNode { Id = "Y", Prompt = "Y" },
                new CompositionNode { Id = "Z", Prompt = "Z" }
            }
        };

        var order = graph.GetExecutionOrder();

        order.Should().HaveCount(3);
        order[0].Id.Should().Be("X");
        order[1].Id.Should().Be("Y");
        order[2].Id.Should().Be("Z");
    }

    [Fact]
    public void LoadFromFile_ValidJson_ParsesCorrectly()
    {
        var filePath = Path.Combine(_tempDir, "composition.json");
        var graphJson = JsonSerializer.Serialize(new
        {
            systems = new[]
            {
                new { id = "health", prompt = "Health system", depends = Array.Empty<string>() },
                new { id = "combat", prompt = "Combat system", depends = new[] { "health" } }
            }
        }, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, WriteIndented = true });
        File.WriteAllText(filePath, graphJson);

        var graph = CompositionGraph.LoadFromFile(filePath);

        graph.Should().NotBeNull();
        graph!.Systems.Should().HaveCount(2);
        graph.Systems[0].Id.Should().Be("health");
        graph.Systems[1].Id.Should().Be("combat");
        graph.Systems[1].Depends.Should().Contain("health");
    }

    [Fact]
    public void LoadFromFile_MissingFile_ReturnsNull()
    {
        CompositionGraph.LoadFromFile(Path.Combine(_tempDir, "nope.json")).Should().BeNull();
    }

    [Fact]
    public void LoadFromFile_NullPath_ReturnsNull()
    {
        CompositionGraph.LoadFromFile(null!).Should().BeNull();
    }

    [Fact]
    public void LoadFromFile_EmptyPath_ReturnsNull()
    {
        CompositionGraph.LoadFromFile("").Should().BeNull();
    }
}
