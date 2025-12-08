using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Nexo.Abstractions;
using Nexo.Orchestration.Agents;
using Nexo.Orchestration.Architect.Models;
using Nexo.Orchestration.Coordination.Conflicts;
using Nexo.Orchestration.Negotiation;
using System.Text.Json;
using Xunit;

namespace Nexo.Tests.Orchestration.Negotiation;

public class NegotiationProtocolTests
{
    private readonly IServiceProvider _serviceProvider;

    public NegotiationProtocolTests()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        _serviceProvider = services.BuildServiceProvider();
    }

    [Fact]
    public async Task NegotiateSchemaConflict_WithCompatibleSchemas_MergesSuccessfully()
    {
        // Arrange
        var protocol = CreateProtocol();
        var agents = CreateAgentsWithSchemas(
            ("agent-1", """{"type": "object", "properties": {"name": {"type": "string"}}}"""),
            ("agent-2", """{"type": "object", "properties": {"value": {"type": "number"}}}"""));

        var conflict = new Conflict
        {
            ConflictType = ConflictType.Schema,
            AgentIds = new[] { "agent-1", "agent-2" },
            Severity = ConflictSeverity.Medium,
            Description = "Incompatible output schemas"
        };

        // Act
        var result = await protocol.NegotiateAsync(conflict, agents);

        // Assert
        result.Success.Should().BeTrue();
        result.ResolvedSchema.Should().NotBeNull();
        result.ResolutionType.Should().Be(ResolutionType.Automatic);
    }

    [Fact]
    public async Task NegotiateResourceConflict_WithinBudget_AllocatesSuccessfully()
    {
        // Arrange
        var protocol = CreateProtocol();
        var agents = CreateAgentsWithResources(
            ("agent-1", 500, 1000),  // 500s compute, 1GB memory
            ("agent-2", 300, 500));   // 300s compute, 500MB memory

        var conflict = new Conflict
        {
            ConflictType = ConflictType.Resource,
            AgentIds = new[] { "agent-1", "agent-2" },
            Severity = ConflictSeverity.Medium,
            Description = "Resource contention"
        };

        // Act
        var result = await protocol.NegotiateAsync(conflict, agents);

        // Assert
        result.Success.Should().BeTrue();
        result.ResolvedAllocation.Should().NotBeNull();
    }

    [Fact]
    public async Task NegotiatePhilosophyConflict_WithSynthesis_FindsCreativeResolution()
    {
        // Arrange - mock model for synthesis
        var modelMock = new Mock<IModel>();
        modelMock
            .Setup(m => m.CompleteAsync(It.IsAny<ModelInput>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ModelOutput("""
                {
                    "description": "Hybrid approach using both philosophies",
                    "requiredChanges": {
                        "agent-1": "Support configurable mode",
                        "agent-2": "Accept hybrid interface"
                    },
                    "confidence": 0.8
                }
                """));

        var protocol = CreateProtocol(modelMock.Object);
        var agents = CreateAgentsWithPhilosophies(
            ("agent-1", "performance-focused", "Minimize latency"),
            ("agent-2", "quality-focused", "Maximize accuracy"));

        var conflict = new Conflict
        {
            ConflictType = ConflictType.Philosophy,
            AgentIds = new[] { "agent-1", "agent-2" },
            Severity = ConflictSeverity.Low,
            Description = "Conflicting design philosophies"
        };

        // Act
        var result = await protocol.NegotiateAsync(conflict, agents);

        // Assert
        result.Success.Should().BeTrue();
        result.ResolutionType.Should().BeOneOf(ResolutionType.Synthesized, ResolutionType.Negotiated);
        result.Resolution.Should().NotBeNull();
    }

    [Fact]
    public async Task NegotiateConstraintConflict_WithHardConflict_Escalates()
    {
        // Arrange
        var protocol = CreateProtocol();
        var agents = CreateAgentsWithConstraints(
            ("agent-1", new[] { "must be synchronous" }, new Dictionary<string, double>()),
            ("agent-2", new[] { "must be asynchronous" }, new Dictionary<string, double>()));

        var conflict = new Conflict
        {
            ConflictType = ConflictType.Constraint,
            AgentIds = new[] { "agent-1", "agent-2" },
            Severity = ConflictSeverity.High,
            Description = "Constraint violation"
        };

        // Act
        var result = await protocol.NegotiateAsync(conflict, agents);

        // Assert
        result.Success.Should().BeFalse();
        result.ResolutionType.Should().Be(ResolutionType.Escalated);
    }

    // Helper methods
    private NegotiationProtocol CreateProtocol(IModel? model = null)
    {
        var logger = _serviceProvider.GetRequiredService<ILogger<NegotiationProtocol>>();
        var schemaAdapter = new SchemaAdapter(_serviceProvider.GetRequiredService<ILogger<SchemaAdapter>>());
        var paretoOptimizer = new ParetoOptimizer(_serviceProvider.GetRequiredService<ILogger<ParetoOptimizer>>());
        var constraintRelaxer = new ConstraintRelaxer(_serviceProvider.GetRequiredService<ILogger<ConstraintRelaxer>>());
        var synthesisEngine = new SynthesisEngine(
            _serviceProvider.GetRequiredService<ILogger<SynthesisEngine>>(),
            model);
        return new NegotiationProtocol(logger, schemaAdapter, paretoOptimizer, constraintRelaxer, synthesisEngine, model);
    }

    private IReadOnlyList<AgentContainer> CreateAgentsWithSchemas(params (string, string)[] agents)
    {
        var factory = new AgentFactory(
            _serviceProvider.GetRequiredService<ILogger<AgentFactory>>(),
            _serviceProvider);

        return agents.Select(a =>
        {
            var spec = new AgentSpawnSpec
            {
                AgentId = a.Item1,
                Domain = "Combat",
                Goal = "Test goal",
                OutputSchema = JsonDocument.Parse(a.Item2).RootElement
            };
            return factory.CreateContainer(spec);
        }).ToList();
    }

    private IReadOnlyList<AgentContainer> CreateAgentsWithResources(params (string, int, int)[] agents)
    {
        var factory = new AgentFactory(
            _serviceProvider.GetRequiredService<ILogger<AgentFactory>>(),
            _serviceProvider);

        return agents.Select(a =>
        {
            var spec = new AgentSpawnSpec
            {
                AgentId = a.Item1,
                Domain = "Combat",
                Goal = "Test goal",
                ResourceRequirements = new ResourceRequirements
                {
                    EstimatedComputeSeconds = a.Item2,
                    RequiredMemoryMB = a.Item3
                }
            };
            return factory.CreateContainer(spec);
        }).ToList();
    }

    private IReadOnlyList<AgentContainer> CreateAgentsWithPhilosophies(params (string, string, string)[] agents)
    {
        var factory = new AgentFactory(
            _serviceProvider.GetRequiredService<ILogger<AgentFactory>>(),
            _serviceProvider);

        return agents.Select(a =>
        {
            var spec = new AgentSpawnSpec
            {
                AgentId = a.Item1,
                Domain = "Combat",
                Goal = a.Item3,
                Description = a.Item2
            };
            return factory.CreateContainer(spec);
        }).ToList();
    }

    private IReadOnlyList<AgentContainer> CreateAgentsWithConstraints(
        params (string, string[], Dictionary<string, double>)[] agents)
    {
        var factory = new AgentFactory(
            _serviceProvider.GetRequiredService<ILogger<AgentFactory>>(),
            _serviceProvider);

        return agents.Select(a =>
        {
            var constraints = a.Item2.Select(c => new AgentConstraint
            {
                Type = "Performance",
                Description = c,
                IsMandatory = true
            }).ToList();

            var spec = new AgentSpawnSpec
            {
                AgentId = a.Item1,
                Domain = "Combat",
                Goal = "Test goal",
                Constraints = constraints
            };
            return factory.CreateContainer(spec);
        }).ToList();
    }
}

