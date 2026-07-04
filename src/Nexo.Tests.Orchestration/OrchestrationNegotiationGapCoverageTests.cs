using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Nexo.Abstractions;
using Nexo.Core.Application.Common.Ports;
using Nexo.Orchestration.Coordination.Conflicts;
using Nexo.Orchestration.Negotiation;
using Nexo.Orchestration.Negotiation.Models;
using System.Text.Json;
using Xunit;

namespace Nexo.Tests.Orchestration;

/// <summary>Tests for orchestration negotiation gap coverage.</summary>
public class OrchestrationNegotiationGapCoverageTests
{
    [Fact]
    public void SchemaAdapter_merges_compatible_and_union_schemas()
    {
        var adapter = new SchemaAdapter(NullLogger<SchemaAdapter>.Instance);

        var objectA = JsonDocument.Parse("""{"type":"object","properties":{"id":{"type":"string"}}}""").RootElement;
        var objectB = JsonDocument.Parse("""{"type":"object","properties":{"name":{"type":"string"}}}""").RootElement;
        adapter.MergeSchemas(objectA, objectB).Should().NotBeNull();

        var intSchema = JsonDocument.Parse("""{"type":"integer"}""").RootElement;
        var numSchema = JsonDocument.Parse("""{"type":"number"}""").RootElement;
        adapter.MergeSchemas(intSchema, numSchema).Should().NotBeNull();

        var stringSchema = JsonDocument.Parse("""{"type":"string"}""").RootElement;
        var boolSchema = JsonDocument.Parse("""{"type":"boolean"}""").RootElement;
        adapter.MergeSchemas(stringSchema, boolSchema).Should().NotBeNull();
    }

    [Fact]
    public void SchemaAdapter_returns_null_when_merge_fails()
    {
        var adapter = new SchemaAdapter(NullLogger<SchemaAdapter>.Instance);
        var invalid = default(JsonElement);
        adapter.MergeSchemas(invalid, invalid).Should().BeNull();
    }

    [Fact]
    public async Task SynthesisEngine_uses_fallback_when_model_missing()
    {
        var engine = new SynthesisEngine(NullLogger<SynthesisEngine>.Instance);
        var positions = new[]
        {
            new NegotiationPosition { AgentId = "a1", Domain = "combat", PrimaryGoal = "fast combat" },
            new NegotiationPosition { AgentId = "a2", Domain = "economy", PrimaryGoal = "deep economy" },
        };
        var conflict = SampleConflict("a1", "a2");

        var resolution = await engine.SynthesizeAsync(positions, conflict);
        resolution.Should().NotBeNull();
        resolution!.ProposerId.Should().Be("synthesis-fallback");
    }

    [Fact]
    public async Task SynthesisEngine_parses_model_output_and_uses_cache()
    {
        var model = new Mock<IModel>();
        model.Setup(m => m.CompleteAsync(It.IsAny<ModelInput>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ModelOutput("""
                ```json
                {
                  "description": "Hybrid pacing",
                  "requiredChanges": { "a1": "shorter loops", "a2": "background sim" },
                  "confidence": 0.8
                }
                ```
                """));

        var cache = new Mock<ICacheStrategy>();
        cache.Setup(c => c.GetAsync<ProposedResolution>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProposedResolution?)null);
        cache.Setup(c => c.SetAsync(It.IsAny<string>(), It.IsAny<ProposedResolution>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var engine = new SynthesisEngine(NullLogger<SynthesisEngine>.Instance, model.Object, cache.Object);
        var positions = new[]
        {
            new NegotiationPosition
            {
                AgentId = "a1",
                Domain = "combat",
                PrimaryGoal = "action",
                UnderlyingGoals = new[] { "fun" },
                HardConstraints = new[] { "60fps" },
                FlexibilityScore = 0.5,
            },
            new NegotiationPosition { AgentId = "a2", Domain = "economy", PrimaryGoal = "depth" },
        };
        var conflict = SampleConflict("a1", "a2");

        var resolution = await engine.SynthesizeAsync(positions, conflict);
        resolution!.Description.Should().Be("Hybrid pacing");
        resolution.RequiredChanges["a1"].Should().Be("shorter loops");
        cache.Verify(c => c.SetAsync(It.IsAny<string>(), It.IsAny<ProposedResolution>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SynthesisEngine_returns_cached_resolution_without_calling_model()
    {
        var cached = new ProposedResolution { ProposerId = "cache", Description = "cached", Confidence = 0.9 };
        var cache = new Mock<ICacheStrategy>();
        cache.Setup(c => c.GetAsync<ProposedResolution>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(cached);

        var model = new Mock<IModel>();
        var engine = new SynthesisEngine(NullLogger<SynthesisEngine>.Instance, model.Object, cache.Object);

        var result = await engine.SynthesizeAsync(
            new[] { new NegotiationPosition { AgentId = "a1", Domain = "x", PrimaryGoal = "g1" } },
            SampleConflict("a1"));

        result.Should().BeSameAs(cached);
        model.Verify(m => m.CompleteAsync(It.IsAny<ModelInput>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>Sample conflict.</summary>
    /// <param name="agentIds">Agent ids.</param>
    private static Conflict SampleConflict(params string[] agentIds) => new()
    {
        ConflictType = ConflictType.Philosophy,
        AgentIds = agentIds,
        Description = "test conflict",
    };
}
