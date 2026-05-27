using FluentAssertions;
using Nexo.Core.Domain.Bricks;
using Nexo.Core.Domain.Clusters;
using Nexo.Core.Domain.Execution;
using Nexo.Core.Domain.Execution.Events;
using Nexo.Core.Domain.Export;
using Nexo.Core.Domain.Exceptions;
using Nexo.Core.Domain.Workflows;
using Xunit;
using ExecutionContext = Nexo.Infrastructure.Execution.ExecutionContext;
using BehaviorResult = Nexo.Core.Domain.Execution.BehaviorResult;

namespace Nexo.Tests.Domain.Tests;

public class DomainGapCoverageTests
{
    [Fact]
    public void ClusterInput_exposes_port_and_parameter_dictionaries()
    {
        var input = new ClusterInput
        {
            PortValues = new Dictionary<string, object> { ["in"] = 1 },
            Parameters = new Dictionary<string, object> { ["p"] = "v" },
        };
        input.PortValues.Should().ContainKey("in");
        input.Parameters["p"].Should().Be("v");
    }

    [Fact]
    public void ClusterStats_tracks_usage_metrics()
    {
        var stats = new ClusterStats { UsageCount = 9, FavoriteCount = 2, AverageRating = 4.5 };
        stats.UsageCount.Should().Be(9);
        stats.FavoriteCount.Should().Be(2);
        stats.AverageRating.Should().Be(4.5);
    }

    [Fact]
    public void ParameterValidation_stores_rule_fields()
    {
        var v = new ParameterValidation("min", "1", "too small");
        v.Type.Should().Be("min");
        v.Value.Should().Be("1");
        v.Message.Should().Be("too small");
    }

    [Fact]
    public void DistributionRule_supports_enum_and_range_fields()
    {
        var rule = new DistributionRule
        {
            Parameter = "count",
            Type = DistributionType.Range,
            Values = new object[] { 1, 2 },
            RangeMin = 0,
            RangeMax = 10,
        };
        rule.Parameter.Should().Be("count");
        rule.Type.Should().Be(DistributionType.Range);
        rule.Values.Should().HaveCount(2);
        rule.RangeMin.Should().Be(0);
        rule.RangeMax.Should().Be(10);
    }

    [Fact]
    public void ImmutableCoreViolationException_exposes_target_component()
    {
        var ex = new ImmutableCoreViolationException("blocked", "pipeline");
        ex.Message.Should().Be("blocked");
        ex.TargetComponent.Should().Be("pipeline");
    }

    [Fact]
    public void BehaviorResult_and_ExecutionOptions_round_trip()
    {
        var result = new BehaviorResult
        {
            Success = true,
            Outputs = new Dictionary<string, object> { ["k"] = 1 },
            Errors = new[] { "warn" },
            Duration = TimeSpan.FromSeconds(2),
        };
        result.Success.Should().BeTrue();
        result.Outputs.Should().ContainKey("k");
        result.Errors.Should().ContainSingle();
        result.Duration.Should().Be(TimeSpan.FromSeconds(2));

        var opts = new ExecutionOptions
        {
            IsAirGapped = true,
            AuditMode = true,
            Provider = "mock",
            BrickRuntime = new Dictionary<string, BrickRuntimeSpec>
            {
                ["b"] = BrickRuntimeSpec.AgenticOnly(),
            },
        };
        opts.IsAirGapped.Should().BeTrue();
        opts.BrickRuntime["b"].Prefer.Should().Be("agentic");
    }

    [Fact]
    public void BrickRuntimeSpec_factory_methods_set_preference_and_fallback()
    {
        BrickRuntimeSpec.DeterministicOnly().Prefer.Should().Be("deterministic");
        BrickRuntimeSpec.AgenticWithDeterministicFallback().Fallback.Should().HaveCount(2);
        BrickRuntimeSpec.AgenticOnly().Fallback.Should().ContainSingle().Which.Should().Be(ImplementationType.Agentic);
    }

    [Fact]
    public void ImplementationSelector_evaluates_context_conditions()
    {
        var selector = new ImplementationSelector
        {
            PreferDeterministic = new[] { "environment.airGapped", "context.auditMode" },
            PreferAgentic = new[] { "unknown.condition" },
            Default = ImplementationType.Agentic,
        };

        var airGapped = new ExecutionContext
        {
            AgentId = "a",
            BehaviorId = "b",
            IsAirGapped = true,
            AuditMode = false,
            Provider = "mock",
            Variables = new Dictionary<string, object>(),
        };
        selector.Select(airGapped).Should().Be(ImplementationType.Deterministic);

        var audit = new ExecutionContext
        {
            AgentId = "a",
            BehaviorId = "b",
            IsAirGapped = false,
            AuditMode = true,
            Provider = "mock",
            Variables = new Dictionary<string, object>(),
        };
        selector.Select(audit).Should().Be(ImplementationType.Deterministic);

        var defaultCtx = new ExecutionContext
        {
            AgentId = "a",
            BehaviorId = "b",
            IsAirGapped = false,
            AuditMode = false,
            Provider = "mock",
            Variables = new Dictionary<string, object>(),
        };
        selector.Select(defaultCtx).Should().Be(ImplementationType.Agentic);
    }

    [Fact]
    public void Workflow_and_connection_round_trip()
    {
        var workflow = new Workflow
        {
            Id = "wf-1",
            Name = "Main",
            Description = "desc",
            Instances = new[] { new ClusterInstance { InstanceId = "i1", ClusterId = "c1" } },
            Connections = new[]
            {
                new WorkflowConnection
                {
                    Id = "conn-1",
                    FromInstanceId = "i1",
                    FromOutput = "out",
                    ToInstanceId = "i2",
                    ToInput = "in",
                },
            },
        };
        workflow.Instances.Should().ContainSingle();
        workflow.Connections[0].ToInput.Should().Be("in");
    }

    [Fact]
    public void Export_result_and_generation_summary_round_trip()
    {
        var item = new GeneratedItem
        {
            BrickId = "b1",
            ItemType = "dialogue",
            VariationCount = 3,
            Reviewed = true,
        };
        var summary = new GenerationSummary
        {
            ItemsGenerated = 1,
            VariationsCreated = 3,
            Items = new[] { item },
        };
        var result = new ExportResult
        {
            Success = true,
            Mode = ExportMode.AIGeneratedThenDeterministic,
            Target = ExportTarget.CSharp,
            GenerationSummary = summary,
            RuntimeRequirements = new[] { "Nexo.Runtime" },
            Messages = new[] { "ok" },
        };
        result.GenerationSummary!.Items[0].BrickId.Should().Be("b1");
        result.Messages.Should().ContainSingle();
    }

    [Fact]
    public void Execution_events_expose_expected_metadata()
    {
        new BehaviorStartedEvent("b1", "Behavior", DateTime.UtcNow).Type.Should().Be("behavior_started");
        new BehaviorCompletedEvent("b1", true, new Dictionary<string, object>()).Success.Should().BeTrue();
        new BehaviorCancelledEvent("b1").BehaviorId.Should().Be("b1");
        new StepStartedEvent("s1", "brick", "Brick", ImplementationType.Deterministic, false, 0, 1)
            .StepIndex.Should().Be(0);
        new StepCompletedEvent("s1", "brick", ImplementationType.Agentic, 12, "done").LatencyMs.Should().Be(12);
        new StepSkippedEvent("s1", "condition false").Reason.Should().Contain("condition");
        new StepErrorEvent("s1", "boom", 5).LatencyMs.Should().Be(5);
        new CacheHitEvent("s1", "key").CacheKey.Should().Be("key");
        new ProviderSwitchedEvent("openai", "ollama").ToProvider.Should().Be("ollama");
        new OfflineModeActivatedEvent().Type.Should().Be("offline_mode_activated");
    }

    [Fact]
    public void WorkflowDefinition_nodes_and_connections_round_trip()
    {
        var cluster = new ClusterNode
        {
            Name = "Combat",
            ClusterId = "combat-cluster",
            Mode = ImplementationMode.AgenticPreferred,
            Parameters = new Dictionary<string, object> { ["difficulty"] = "hard" },
        };
        cluster.ClusterId.Should().Be("combat-cluster");

        var input = new InputNode
        {
            Name = "Level Data",
            Type = InputType.Content,
            Content = "{}",
            WebhookUrl = "https://example.com/hook",
            FilePath = "/in/data.json",
            DatabaseConnectionString = "Host=localhost",
            Query = "select 1",
        };
        input.Type.Should().Be(InputType.Content);
        input.FilePath.Should().Be("/in/data.json");

        var output = new OutputNode
        {
            Name = "Results",
            Type = OutputType.File,
            Format = Nexo.Core.Domain.Workflows.OutputFormat.Json,
            FilePath = "/out/result.json",
        };
        output.Format.Should().Be(Nexo.Core.Domain.Workflows.OutputFormat.Json);

        var transform = new TransformNode { Name = "Map", Operation = TransformOperation.Filter, Expression = "x > 0" };
        transform.Operation.Should().Be(TransformOperation.Filter);

        var conditional = new ConditionalNode { Name = "Branch", Condition = "score > 10" };
        conditional.Condition.Should().Be("score > 10");

        var definition = new WorkflowDefinition
        {
            Name = "Visual Flow",
            Nodes = new WorkflowNode[] { cluster, input, output, transform, conditional },
            Connections = new[]
            {
                new VisualWorkflowConnection
                {
                    FromNodeId = input.Id,
                    FromPortId = "out",
                    ToNodeId = cluster.Id,
                    ToPortId = "in",
                    Type = ConnectionType.Data,
                },
            },
            Metadata = new WorkflowMetadata { Zoom = 1.5, ViewportCenter = new Nexo.Core.Domain.Workflows.Position(10, 20) },
        };
        definition.Nodes.Should().HaveCount(5);
        definition.Connections.Should().ContainSingle();
        definition.Metadata.Zoom.Should().Be(1.5);
    }

    [Fact]
    public void WorkflowDefinition_agent_and_brick_nodes_round_trip()
    {
        var agent = new AgentNode
        {
            Name = "Planner",
            AgentId = "planner-1",
            Mode = ImplementationMode.Mixed,
            BehaviorOverrides = new Dictionary<string, ImplementationMode> { ["analyze"] = ImplementationMode.DeterministicOnly },
            BrickOverrides = new Dictionary<string, ImplementationType> { ["Echo"] = ImplementationType.Agentic },
            Parameters = new Dictionary<string, object> { ["depth"] = 2 },
        };
        agent.AgentId.Should().Be("planner-1");

        var brick = new BrickNode
        {
            Name = "Echo",
            BrickId = "echo",
            Implementation = ImplementationType.Deterministic,
            ProviderOverride = "local",
            Parameters = new Dictionary<string, object> { ["msg"] = "hi" },
        };
        brick.ProviderOverride.Should().Be("local");

        var port = new NodePort
        {
            Name = "In",
            Direction = PortDirection.Input,
            DataType = "string",
            Required = false,
            AllowMultiple = true,
        };
        port.AllowMultiple.Should().BeTrue();
    }
}
