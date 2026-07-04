using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Nexo.Abstractions.Agents;
using Nexo.Orchestration.Agents;
using Nexo.Orchestration.Architect;
using Nexo.Orchestration.Architect.Models;
using Nexo.Orchestration.Coordination;
using Nexo.Orchestration.Coordination.Conflicts;
using Nexo.Orchestration.Validation;
using Xunit;

namespace Nexo.Tests.Orchestration;

/// <summary>Tests for orchestration coordination gap coverage.</summary>
public class OrchestrationCoordinationGapCoverageTests
{
    private readonly AgentFactory _factory;

    public OrchestrationCoordinationGapCoverageTests()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        _factory = new AgentFactory(
            services.BuildServiceProvider().GetRequiredService<ILogger<AgentFactory>>(),
            services.BuildServiceProvider());
    }

    [Fact]
    public void ConflictDetector_detects_schema_philosophy_and_aggregate_resource_conflicts()
    {
        var detector = new ConflictDetector(NullLogger<ConflictDetector>.Instance);

        var schemaA = JsonDocument.Parse("""{"type":"object","properties":{"id":{"type":"string"}}}""").RootElement;
        var schemaB = JsonDocument.Parse("""{"type":"number"}""").RootElement;

        var c1 = _factory.CreateContainer(new AgentSpawnSpec
        {
            AgentId = "schema-a",
            Domain = "Data",
            Goal = "optimize fast performance",
            OutputSchema = schemaA,
        });
        var c2 = _factory.CreateContainer(new AgentSpawnSpec
        {
            AgentId = "schema-b",
            Domain = "Data",
            Goal = "deliver thorough comprehensive quality polish",
            OutputSchema = schemaB,
        });

        detector.DetectConflicts(new[] { c1, c2 })
            .Should().Contain(c => c.ConflictType == ConflictType.Schema)
            .And.Contain(c => c.ConflictType == ConflictType.Philosophy);

        var heavy = _factory.CreateContainer(new AgentSpawnSpec
        {
            AgentId = "heavy-1",
            Domain = "Sim",
            Goal = "simulate",
            ResourceRequirements = new ResourceRequirements { RequiredMemoryMB = 9000 },
        });
        var heavy2 = _factory.CreateContainer(new AgentSpawnSpec
        {
            AgentId = "heavy-2",
            Domain = "Sim",
            Goal = "simulate",
            ResourceRequirements = new ResourceRequirements { RequiredMemoryMB = 9000 },
        });

        detector.DetectResourceConflicts(
                new[] { heavy, heavy2 },
                new ResourceBudget { MaxMemoryMB = 10000 })
            .Should().ContainSingle(c => c.ConflictType == ConflictType.Resource);

        detector.DetectSchemaConflicts(new[] { c1, c2 })
            .Should().ContainSingle(c => c.ConflictType == ConflictType.Schema);
    }

    [Fact]
    public void EscalationManager_tracks_conflict_and_issue_escalations()
    {
        var manager = new EscalationManager(NullLogger<EscalationManager>.Instance);
        var conflict = new Conflict
        {
            ConflictType = ConflictType.Constraint,
            AgentIds = new[] { "a1", "a2" },
            Description = "contradiction",
            Severity = ConflictSeverity.High,
        };

        var escalation = manager.EscalateConflict(conflict, "needs review");
        escalation.Status.Should().Be(EscalationStatus.Pending);
        escalation.Context.Should().Be("needs review");

        manager.GetPendingEscalations().Should().ContainSingle();
        manager.GetEscalationsBySeverity(EscalationSeverity.High).Should().ContainSingle();
        manager.GetAllEscalations().Should().HaveCount(1);

        manager.ResolveEscalation(escalation.Id, "fixed");
        manager.GetPendingEscalations().Should().BeEmpty();

        var issue = manager.EscalateIssue("Timeout", "agent stalled", EscalationSeverity.Critical);
        issue.IssueType.Should().Be("Timeout");

        manager.ResolveEscalation("missing-id");
    }

    [Fact]
    public void EscalationManager_throws_for_null_conflict()
    {
        var manager = new EscalationManager(NullLogger<EscalationManager>.Instance);
        var act = () => manager.EscalateConflict(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void OutputIntegrator_merges_domain_outputs_and_validates_schemas()
    {
        var integrator = new OutputIntegrator(NullLogger<OutputIntegrator>.Instance);
        var schema = JsonDocument.Parse("""
            {
              "type": "object",
              "required": ["name"],
              "properties": { "name": { "type": "string" } }
            }
            """).RootElement;

        var specs = new[]
        {
            new AgentSpawnSpec
            {
                AgentId = "a1",
                Domain = "Combat",
                Goal = "design",
                OutputSchema = schema,
                Constraints = new[] { new AgentConstraint { Type = "required_field", Description = "Field 'name' is required" } },
            },
            new AgentSpawnSpec
            {
                AgentId = "a2",
                Domain = "Combat",
                Goal = "balance",
            },
            new AgentSpawnSpec
            {
                AgentId = "missing",
                Domain = "Economy",
                Goal = "economy",
            },
        };

        var outputs = new Dictionary<string, object>
        {
            ["a1"] = new { name = "hero" },
            ["a2"] = new { score = 10 },
        };

        var result = integrator.Integrate(specs, outputs);
        result.IntegratedResults.Should().ContainKey("Combat");
        result.IntegrationErrors.Should().BeEmpty();
        result.ValidationErrors.Should().Contain(e => e.Contains("missing", StringComparison.OrdinalIgnoreCase));

        var combined = result.IntegratedResults["Combat"];
        combined.Should().BeOfType<Dictionary<string, object>>();
    }

    [Fact]
    public void OutputIntegrator_throws_for_null_inputs()
    {
        var integrator = new OutputIntegrator(NullLogger<OutputIntegrator>.Instance);
        integrator.Invoking(i => i.Integrate(null!, new Dictionary<string, object>()))
            .Should().Throw<ArgumentNullException>();
        integrator.Invoking(i => i.Integrate(Array.Empty<AgentSpawnSpec>(), null!))
            .Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void DependencyResolver_tracks_dependencies_execution_order_and_outputs()
    {
        var resolver = new DependencyResolver(NullLogger<DependencyResolver>.Instance);
        var services = new ServiceCollection();
        services.AddLogging();
        var sp = services.BuildServiceProvider();
        var factory = new AgentFactory(sp.GetRequiredService<ILogger<AgentFactory>>(), sp);

        var lead = factory.CreateContainer(new AgentSpawnSpec
        {
            AgentId = "lead",
            Domain = "Planning",
            Goal = "lead work",
        });
        var worker = factory.CreateContainer(new AgentSpawnSpec
        {
            AgentId = "worker",
            Domain = "Planning",
            Goal = "worker task",
            Dependencies = new[] { "lead" },
            ReportsToAgentId = "lead",
        });

        resolver.RegisterAgent(lead);
        resolver.RegisterAgent(worker);
        /// <summary>Sets agent state.</summary>
        SetAgentState(lead.Agent, AgentState.Ready);
        /// <summary>Sets agent state.</summary>
        SetAgentState(worker.Agent, AgentState.Ready);

        resolver.GetReadyAgents().Should().ContainSingle(c => c.AgentId == "lead");
        resolver.GetBlockedAgents().Should().ContainSingle(c => c.AgentId == "worker");
        resolver.GetBlockingDependencies("worker").Should().Contain("lead");
        resolver.GetDependenciesForAgent("worker").Should().Contain("lead");

        resolver.RecordOutput("lead", new { ok = true });
        resolver.AreDependenciesResolved("worker").Should().BeTrue();
        resolver.GetReadyAgents().Should().ContainSingle(c => c.AgentId == "worker");

        resolver.GetExecutionOrder().Should().ContainInOrder("lead", "worker");
        resolver.GetAllOutputs()["lead"].Should().NotBeNull();
        resolver.Clear();
        resolver.GetAllOutputs().Should().BeEmpty();
    }

    [Fact]
    public void DependencyResolver_rejects_null_container()
    {
        var resolver = new DependencyResolver(NullLogger<DependencyResolver>.Instance);
        var act = () => resolver.RegisterAgent(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void DependencyResolver_detects_circular_dependencies_in_execution_order()
    {
        var resolver = new DependencyResolver(NullLogger<DependencyResolver>.Instance);
        var services = new ServiceCollection();
        services.AddLogging();
        var sp = services.BuildServiceProvider();
        var factory = new AgentFactory(sp.GetRequiredService<ILogger<AgentFactory>>(), sp);

        var a = factory.CreateContainer(new AgentSpawnSpec { AgentId = "a", Domain = "Planning", Goal = "a", Dependencies = new[] { "b" } });
        var b = factory.CreateContainer(new AgentSpawnSpec { AgentId = "b", Domain = "Planning", Goal = "b", Dependencies = new[] { "a" } });
        resolver.RegisterAgent(a);
        resolver.RegisterAgent(b);

        resolver.GetExecutionOrder().Should().Contain(new[] { "a", "b" });
    }

    [Fact]
    public async Task HealthMonitor_reports_unhealthy_agents()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        var sp = services.BuildServiceProvider();
        var factory = new AgentFactory(sp.GetRequiredService<ILogger<AgentFactory>>(), sp);
        var container = factory.CreateContainer(new AgentSpawnSpec
        {
            AgentId = "sick",
            Domain = "Planning",
            Goal = "fail",
            ResourceRequirements = new ResourceRequirements { EstimatedComputeSeconds = 1 },
        });
        await container.InitializeAsync();

        var healthField = typeof(BaseAgent).GetField("_health", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        healthField.SetValue(container.Agent, AgentHealth.Unhealthy);

        var monitor = new HealthMonitor(NullLogger<HealthMonitor>.Instance, TimeSpan.FromHours(1));
        monitor.RegisterAgent(container);
        monitor.GetUnhealthyAgents().Should().ContainSingle(s => s.AgentId == "sick");
        monitor.Dispose();
    }

    [Fact]
    public void DependencyResolver_rejects_invalid_output_and_unknown_agent()
    {
        var resolver = new DependencyResolver(NullLogger<DependencyResolver>.Instance);
        resolver.GetDependenciesForAgent("").Should().BeEmpty();
        resolver.GetBlockingDependencies("missing").Should().BeEmpty();
        resolver.AreDependenciesResolved("missing").Should().BeFalse();

        var act = () => resolver.RecordOutput("", new object());
        act.Should().Throw<ArgumentException>();
        var act2 = () => resolver.RecordOutput("a1", null!);
        act2.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void TimeoutManager_creates_and_cancels_agent_timeouts()
    {
        var manager = new TimeoutManager(NullLogger<TimeoutManager>.Instance, TimeSpan.FromMinutes(5));
        var services = new ServiceCollection();
        services.AddLogging();
        var sp = services.BuildServiceProvider();
        var factory = new AgentFactory(sp.GetRequiredService<ILogger<AgentFactory>>(), sp);
        var container = factory.CreateContainer(new AgentSpawnSpec
        {
            AgentId = "timed",
            Domain = "Planning",
            Goal = "work",
            ResourceRequirements = new ResourceRequirements { EstimatedComputeSeconds = 30 },
        });

        var token = manager.CreateTimeoutToken(container);
        token.CanBeCanceled.Should().BeTrue();

        manager.CreateTimeoutToken(container, TimeSpan.FromSeconds(1));
        manager.CancelTimeout("timed");
        manager.Dispose();
    }

    [Fact]
    public void TimeoutManager_throws_for_null_container()
    {
        var manager = new TimeoutManager(NullLogger<TimeoutManager>.Instance);
        var act = () => manager.CreateTimeoutToken(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public async Task HealthMonitor_tracks_registered_agents_and_health_checks()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        var sp = services.BuildServiceProvider();
        var factory = new AgentFactory(sp.GetRequiredService<ILogger<AgentFactory>>(), sp);
        var container = factory.CreateContainer(new AgentSpawnSpec
        {
            AgentId = "health-1",
            Domain = "Planning",
            Goal = "check health",
            ResourceRequirements = new ResourceRequirements { EstimatedComputeSeconds = 1 },
        });
        await container.InitializeAsync();
        /// <summary>Sets agent state.</summary>
        SetAgentState(container.Agent, AgentState.Failed);

        var monitor = new HealthMonitor(NullLogger<HealthMonitor>.Instance, TimeSpan.FromHours(1));
        monitor.RegisterAgent(container);
        monitor.GetHealthStatus("health-1")!.AgentId.Should().Be("health-1");
        monitor.GetAllHealthStatuses().Should().ContainSingle();

        var startField = typeof(AgentContainer).GetField("_startTime", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        startField.SetValue(container, DateTimeOffset.UtcNow.AddMinutes(-10));
        /// <summary>Sets agent state.</summary>
        SetAgentState(container.Agent, AgentState.Executing);

        typeof(HealthMonitor).GetMethod("PerformHealthChecks", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .Invoke(monitor, new object?[] { null });

        monitor.GetHealthStatus("health-1")!.Health.Should().Be(AgentHealth.Degraded);
        monitor.UnregisterAgent("health-1");
        monitor.GetHealthStatus("health-1").Should().BeNull();
        monitor.Dispose();
    }

    private static void SetAgentState(BaseAgent agent, AgentState state)
    {
        var field = typeof(BaseAgent).GetField("_state", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        field.SetValue(agent, state);
    }

    [Fact]
    public async Task SchemaValidator_flags_invalid_decomposition()
    {
        var validator = new SchemaValidator(NullLogger<SchemaValidator>.Instance);
        var result = new DecompositionResult
        {
            Agents = new[]
            {
                new AgentSpawnSpec { AgentId = "bad id!", Domain = "", Goal = "", Dependencies = new[] { "missing", "bad id!" } },
            },
            OriginalRequest = "x",
        };

        var errors = await validator.ValidateAsync(result);
        errors.Should().NotBeEmpty();
        errors.Should().Contain(e => e.ErrorType == "Schema");
    }

    [Fact]
    public async Task SchemaValidator_flags_chain_of_command_and_resource_errors()
    {
        var validator = new SchemaValidator(NullLogger<SchemaValidator>.Instance);
        var result = new DecompositionResult
        {
            Agents = new[]
            {
                new AgentSpawnSpec
                {
                    AgentId = "lead",
                    Domain = "Ops",
                    Goal = "Lead",
                    ClusterId = "c1",
                },
                new AgentSpawnSpec
                {
                    AgentId = "worker",
                    Domain = "Ops",
                    Goal = "Work",
                    ClusterId = "c2",
                    ReportsToAgentId = "lead",
                    CommandChain = new[] { "missing-lead", "worker", "worker" },
                    ResourceRequirements = new ResourceRequirements
                    {
                        EstimatedComputeSeconds = -1,
                        RequiredContextTokens = -5,
                        RequiredMemoryMB = -100,
                    },
                    Constraints = new[]
                    {
                        new AgentConstraint { Type = "", Description = "", IsMandatory = false },
                    },
                },
                new AgentSpawnSpec { AgentId = "lead", Domain = "Ops", Goal = "Dup" },
            },
            OriginalRequest = "x",
        };

        var errors = await validator.ValidateAsync(result);
        errors.Should().Contain(e => e.Message.Contains("reports across clusters", StringComparison.OrdinalIgnoreCase));
        errors.Should().Contain(e => e.Message.Contains("Duplicate agent ID", StringComparison.OrdinalIgnoreCase));
        errors.Should().Contain(e => e.Message.Contains("negative", StringComparison.OrdinalIgnoreCase));
        errors.Should().Contain(e => e.Message.Contains("command chain", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task SchemaValidator_accepts_valid_decomposition()
    {
        var validator = new SchemaValidator(NullLogger<SchemaValidator>.Instance);
        var result = new DecompositionResult
        {
            Agents = new[]
            {
                new AgentSpawnSpec
                {
                    AgentId = "planner",
                    Domain = "Design",
                    Goal = "Plan",
                    ReportsToAgentId = "director",
                    CommandChain = new[] { "director" },
                },
                new AgentSpawnSpec
                {
                    AgentId = "director",
                    Domain = "Design",
                    Goal = "Direct",
                },
            },
            OriginalRequest = "build",
        };

        (await validator.ValidateAsync(result)).Should().BeEmpty();
    }
}
