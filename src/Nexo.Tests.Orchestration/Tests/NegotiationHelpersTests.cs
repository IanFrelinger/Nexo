using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nexo.Orchestration.Agents;
using Nexo.Orchestration.Architect;
using Nexo.Orchestration.Architect.Models;
using Nexo.Orchestration.Coordination.Conflicts;
using Nexo.Orchestration.Negotiation;
using Nexo.Orchestration.Negotiation.Models;
using Xunit;

namespace Nexo.Tests.Orchestration.Tests;

public sealed class NegotiationHelpersTests
{
    private readonly IServiceProvider _serviceProvider;

    public NegotiationHelpersTests()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        _serviceProvider = services.BuildServiceProvider();
    }

    // ── ParetoOptimizer ────────────────────────────────────────────

    [Fact(Timeout = 15000)]
    public async Task ParetoOptimizer_WithinBudget_ReturnsNoConflict()
    {
        await Task.CompletedTask;
        var optimizer = CreateParetoOptimizer();
        var agents = CreateAgentsWithResources(
            ("a1", compute: 100, memory: 200),
            ("a2", compute: 100, memory: 200));

        var budget = new ResourceBudget
        {
            MaxComputeSeconds = 1000,
            MaxMemoryMB = 1000
        };

        var result = optimizer.Optimize(agents, budget);

        result.HasConflict.Should().BeFalse();
    }

    [Fact(Timeout = 15000)]
    public async Task ParetoOptimizer_ExceedsBudget_ReturnsParetoFrontier()
    {
        await Task.CompletedTask;
        var optimizer = CreateParetoOptimizer();
        var agents = CreateAgentsWithResources(
            ("a1", compute: 600, memory: 400),
            ("a2", compute: 500, memory: 300));

        var budget = new ResourceBudget
        {
            MaxComputeSeconds = 500,
            MaxMemoryMB = 500
        };

        var result = optimizer.Optimize(agents, budget);

        result.HasConflict.Should().BeTrue();
        result.ParetoFrontier.Should().NotBeEmpty();
        result.RecommendedAllocation.Should().NotBeNull();
        result.Tradeoffs.Should().NotBeEmpty();
    }

    [Fact(Timeout = 15000)]
    public async Task ParetoOptimizer_RecommendedAllocation_RespectsBudget()
    {
        await Task.CompletedTask;
        var optimizer = CreateParetoOptimizer();
        var agents = CreateAgentsWithResources(
            ("a1", compute: 800, memory: 600),
            ("a2", compute: 400, memory: 200));

        var budget = new ResourceBudget
        {
            MaxComputeSeconds = 600,
            MaxMemoryMB = 500
        };

        var result = optimizer.Optimize(agents, budget);

        result.HasConflict.Should().BeTrue();
        var allocation = result.RecommendedAllocation!;
        var totalCompute = allocation.Allocations.Values.Sum(a => a.ComputeSeconds);
        var totalMemory = allocation.Allocations.Values.Sum(a => a.MemoryMb);

        totalCompute.Should().BeLessThanOrEqualTo(600);
        totalMemory.Should().BeLessThanOrEqualTo(500);
    }

    // ── ConstraintRelaxer ──────────────────────────────────────────

    [Fact(Timeout = 15000)]
    public async Task ConstraintRelaxer_HardConflict_ReturnsUnresolvable()
    {
        await Task.CompletedTask;
        var relaxer = CreateConstraintRelaxer();

        var positions = new List<NegotiationPosition>
        {
            new()
            {
                AgentId = "a1",
                Domain = "perf",
                PrimaryGoal = "Speed",
                HardConstraints = new[] { "must be synchronous" }
            },
            new()
            {
                AgentId = "a2",
                Domain = "async",
                PrimaryGoal = "Scalability",
                HardConstraints = new[] { "must be asynchronous" }
            }
        };

        var conflict = new Conflict
        {
            ConflictType = ConflictType.Constraint,
            AgentIds = new[] { "a1", "a2" },
            Description = "Sync vs async hard conflict"
        };

        var result = relaxer.Relax(positions, conflict);

        result.Success.Should().BeFalse();
        result.FailureReason.Should().Contain("Hard constraint");
    }

    [Fact(Timeout = 15000)]
    public async Task ConstraintRelaxer_FlexibleSoftConstraints_RelaxesSuccessfully()
    {
        await Task.CompletedTask;
        var relaxer = CreateConstraintRelaxer();

        var positions = new List<NegotiationPosition>
        {
            new()
            {
                AgentId = "a1",
                Domain = "d1",
                PrimaryGoal = "Goal A",
                HardConstraints = Array.Empty<string>(),
                SoftConstraints = new Dictionary<string, double>
                {
                    ["prefer low latency"] = 0.8
                }
            },
            new()
            {
                AgentId = "a2",
                Domain = "d2",
                PrimaryGoal = "Goal B",
                HardConstraints = Array.Empty<string>(),
                SoftConstraints = new Dictionary<string, double>
                {
                    ["prefer high throughput"] = 0.7
                }
            }
        };

        var conflict = new Conflict
        {
            ConflictType = ConflictType.Constraint,
            AgentIds = new[] { "a1", "a2" },
            Description = "Soft constraint tension"
        };

        var result = relaxer.Relax(positions, conflict);

        result.Success.Should().BeTrue();
        result.RelaxedConstraints.Should().NotBeEmpty();
        result.Explanation.Should().NotBeNullOrEmpty();
    }

    [Fact(Timeout = 15000)]
    public async Task ConstraintRelaxer_LowFlexibility_SkipsConstraint()
    {
        await Task.CompletedTask;
        var relaxer = CreateConstraintRelaxer();

        var positions = new List<NegotiationPosition>
        {
            new()
            {
                AgentId = "a1",
                Domain = "d1",
                PrimaryGoal = "Goal",
                HardConstraints = Array.Empty<string>(),
                SoftConstraints = new Dictionary<string, double>
                {
                    ["rigid rule"] = 0.1
                }
            }
        };

        var conflict = new Conflict
        {
            ConflictType = ConflictType.Constraint,
            AgentIds = new[] { "a1" },
            Description = "Single agent low flexibility"
        };

        var result = relaxer.Relax(positions, conflict);

        result.Success.Should().BeFalse();
    }

    // ── Helpers ──────────────────────────────────────────────────────

    private ParetoOptimizer CreateParetoOptimizer()
    {
        return new ParetoOptimizer(
            _serviceProvider.GetRequiredService<ILogger<ParetoOptimizer>>());
    }

    private ConstraintRelaxer CreateConstraintRelaxer()
    {
        return new ConstraintRelaxer(
            _serviceProvider.GetRequiredService<ILogger<ConstraintRelaxer>>());
    }

    private IReadOnlyList<AgentContainer> CreateAgentsWithResources(
        params (string id, int compute, int memory)[] agents)
    {
        var factory = new AgentFactory(
            _serviceProvider.GetRequiredService<ILogger<AgentFactory>>(),
            _serviceProvider);

        return agents.Select(a =>
        {
            var spec = new AgentSpawnSpec
            {
                AgentId = a.id,
                Domain = "test",
                Goal = "Test goal",
                ResourceRequirements = new ResourceRequirements
                {
                    EstimatedComputeSeconds = a.compute,
                    RequiredMemoryMB = a.memory
                }
            };
            return factory.CreateContainer(spec);
        }).ToList();
    }
}
