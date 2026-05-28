using FluentAssertions;
using Nexo.Core.Domain.Agents;
using Nexo.Core.Domain.Behaviors;
using Nexo.Core.Domain.Bricks;
using Nexo.Core.Domain.Clusters;
using Nexo.Core.Domain.Execution;
using Nexo.Core.Domain.Execution.Events;
using Nexo.Core.Domain.Exceptions;
using Nexo.Core.Domain.Values;
using Nexo.Core.Domain.Workflows;
using Xunit;
using ExecutionContext = Nexo.Infrastructure.Execution.ExecutionContext;

namespace Nexo.Tests.Domain.Tests;

public sealed class DomainRemainingCoverageTests
{
    [Fact]
    public void BrickInput_set_get_and_dictionary_constructor()
    {
        var fromDict = new BrickInput(new Dictionary<string, object> { ["a"] = 1 });
        fromDict.Get<int>("a").Should().Be(1);

        var input = new BrickInput();
        input.Set("key", "value");
        input.Get<string>("key").Should().Be("value");
        input.Get("missing", "default").Should().Be("default");
        input.ToDictionary().Should().ContainKey("key");

        var actMissing = () => input.Get<int>("missing");
        actMissing.Should().Throw<KeyNotFoundException>();

        input.Set("wrong", "text");
        var actCast = () => input.Get<int>("wrong");
        actCast.Should().Throw<InvalidCastException>();

        input.Set("typed", "seven");
        input.Get("typed", 0).Should().Be(0);

        input.Set("n", 5);
        input.Get("n", 0).Should().Be(5);
    }

    [Fact]
    public void BrickOutput_indexer_set_get_and_summary()
    {
        var output = new BrickOutput { Summary = "done" };
        output.Summary.Should().Be("done");
        output["x"] = 42;
        output["x"].Should().Be(42);
        output.Set("y", "z");
        output.Get<string>("y").Should().Be("z");
        output.ToDictionary().Should().ContainKey("y");

        var actMissing = () => output.Get<int>("nope");
        actMissing.Should().Throw<KeyNotFoundException>();

        output.Set("bad", "text");
        var actCast = () => output.Get<int>("bad");
        actCast.Should().Throw<InvalidCastException>();
    }

    [Fact]
    public void BrickMetadata_and_implementations_round_trip()
    {
        var meta = new BrickMetadata
        {
            Author = "team",
            License = "MIT",
            Repository = "https://example.com/repo",
            UsageCount = 3,
            LastUpdated = DateTime.UtcNow,
        };
        meta.Author.Should().Be("team");
        meta.UsageCount.Should().Be(3);

        var implementations = new BrickImplementations
        {
            Deterministic = new DeterministicImplementation
            {
                Id = "det-1",
                Name = "Rules",
                Description = "rule-based",
                Executor = "RuleEngineExecutor",
                Config = new Dictionary<string, object> { ["k"] = 1 },
            },
            Agentic = new AgenticImplementation
            {
                Id = "ag-1",
                Name = "LLM",
                Description = "model",
                ProviderMappings = new Dictionary<string, ProviderConfig>
                {
                    ["openai"] = new ProviderConfig("gpt-4", "https://api.openai.com"),
                },
                LLMConfig = new LLMConfig
                {
                    Model = "gpt-4",
                    SystemPrompt = "sys",
                    Temperature = 0.2,
                    MaxTokens = 100,
                    Tools = new[] { "search" },
                },
            },
        };
        implementations.HasDeterministic.Should().BeTrue();
        implementations.HasAgentic.Should().BeTrue();
    }

    [Fact]
    public void BehaviorInput_constructors_initialize_parameters()
    {
        new BehaviorInput().Parameters.Should().BeEmpty();

        var withParams = new BehaviorInput(new Dictionary<string, object> { ["p"] = 1 });
        withParams.Parameters["p"].Should().Be(1);
    }

    [Fact]
    public void AgenticEscalatedEvent_exposes_escalation_fields()
    {
        var evt = new AgenticEscalatedEvent("step-1", "brick-1", "queue depth", "model-x", "ollama");
        evt.StepId.Should().Be("step-1");
        evt.BrickId.Should().Be("brick-1");
        evt.Reason.Should().Be("queue depth");
        evt.ModelId.Should().Be("model-x");
        evt.Provider.Should().Be("ollama");
        evt.Type.Should().Be("agentic_escalated");
    }

    [Fact]
    public void ImmutableCoreViolationException_supports_all_constructors()
    {
        var simple = new ImmutableCoreViolationException("blocked");
        simple.Message.Should().Be("blocked");
        simple.TargetComponent.Should().BeNull();

        var inner = new InvalidOperationException("root");
        var wrapped = new ImmutableCoreViolationException("wrapped", inner);
        wrapped.InnerException.Should().BeSameAs(inner);
    }

    [Fact]
    public void ImplementationSelector_prefers_agentic_when_matching_condition()
    {
        var selector = new ImplementationSelector
        {
            PreferDeterministic = [],
            PreferAgentic = ["context.auditMode"],
            Default = ImplementationType.Deterministic,
        };
        var ctx = new ExecutionContext
        {
            AgentId = "a",
            BehaviorId = "b",
            IsAirGapped = false,
            AuditMode = true,
            Provider = "mock",
            Variables = new Dictionary<string, object>(),
        };
        selector.Select(ctx).Should().Be(ImplementationType.Agentic);
    }

    [Fact]
    public void WorkflowDefinition_output_node_and_metadata_defaults()
    {
        var definition = new WorkflowDefinition
        {
            Description = "visual flow",
            Metadata = new WorkflowMetadata
            {
                CustomData = new Dictionary<string, object> { ["tag"] = "demo" },
            },
        };
        definition.Description.Should().Be("visual flow");
        definition.Metadata.CustomData.Should().ContainKey("tag");

        var output = new OutputNode
        {
            WebhookUrl = "https://hook",
            DatabaseConnectionString = "Host=db",
            TableName = "results",
        };
        output.WebhookUrl.Should().Be("https://hook");
        output.TableName.Should().Be("results");
    }

    [Fact]
    public void AgentCard_platform_configs_and_behavior_step_config()
    {
        var card = new AgentCard
        {
            Id = "agent-1",
            Name = "Planner",
            Domain = "game",
            Description = "plans",
            PlatformConfigs = new Dictionary<Platform, PlatformConfig>
            {
                [Platform.Unity] = new PlatformConfig { EntryPoint = "game.main" },
            },
        };
        card.PlatformConfigs.Should().ContainKey(Platform.Unity);

        var step = new BehaviorStep
        {
            BrickId = "echo",
            Config = new Dictionary<string, object> { ["depth"] = 2 },
        };
        step.Config.Should().ContainKey("depth");
    }

    [Fact]
    public void ScalingConfig_dynamic_and_event_driven_fields()
    {
        var scaling = new ScalingConfig
        {
            Mode = ScalingMode.Dynamic,
            DynamicExpression = "playerCount * 2",
            TriggerEvent = "wave_start",
        };
        scaling.DynamicExpression.Should().Be("playerCount * 2");
        scaling.TriggerEvent.Should().Be("wave_start");
    }

    [Fact]
    public void ExecutionOptions_exposes_override_dictionaries()
    {
        var options = new ExecutionOptions
        {
            BehaviorOverrides = new Dictionary<string, ImplementationMode> { ["b"] = ImplementationMode.Mixed },
            BrickOverrides = new Dictionary<string, ImplementationType> { ["echo"] = ImplementationType.Agentic },
        };
        options.BehaviorOverrides["b"].Should().Be(ImplementationMode.Mixed);
        options.BrickOverrides["echo"].Should().Be(ImplementationType.Agentic);
    }

    [Fact]
    public void TestBrick_default_properties_are_initialized()
    {
        var brick = new TestBrick();
        brick.FallbackChain.Should().HaveCount(2);
        brick.Metadata.Should().NotBeNull();
        brick.Selector.Should().BeNull();
    }

    [Theory]
    [InlineData(nameof(AIConfidenceLevel))]
    [InlineData(nameof(AIEngineType))]
    [InlineData(nameof(AIProviderType))]
    [InlineData(nameof(BetaProgramStatus))]
    [InlineData(nameof(HealthStatus))]
    [InlineData(nameof(MethodVisibility))]
    [InlineData(nameof(OnboardingStatus))]
    [InlineData(nameof(ProjectStatus))]
    [InlineData(nameof(SprintStatus))]
    [InlineData(nameof(TaskPriority))]
    [InlineData("DomainTaskStatus")]
    [InlineData(nameof(RiskLevel))]
    public void Value_objects_support_equality_and_factory_methods(string typeName)
    {
        var (fromName, fromValue, sample, other) = CreateValueObjectSamples(typeName);
        fromName.Should().NotBeNull();
        fromValue.Should().NotBeNull();
        sample.Equals(other).Should().BeFalse();
        sample.GetHashCode().Should().Be(sample.GetHashCode());
        sample.Equals(fromName).Should().BeTrue();
        (sample == fromName).Should().BeTrue();
        (sample != other).Should().BeTrue();
        sample.ToString().Should().NotBeNullOrWhiteSpace();
        GetDescription(sample).Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void TaskPriority_comparison_operators_order_by_value()
    {
        (TaskPriority.High > TaskPriority.Low).Should().BeTrue();
        (TaskPriority.Low < TaskPriority.High).Should().BeTrue();
        (TaskPriority.High >= TaskPriority.Medium).Should().BeTrue();
        (TaskPriority.Low <= TaskPriority.Medium).Should().BeTrue();
    }

    [Fact]
    public void Cluster_port_and_parameter_optional_fields()
    {
        var port = new ClusterPort
        {
            Name = "in",
            Type = "int",
            Description = "input",
            Default = 1,
            InternalMapping = "brick.in",
        };
        port.Default.Should().Be(1);

        var parameter = new ClusterParameter
        {
            Name = "level",
            Type = "int",
            Validations = new[] { new ParameterValidation("min", "0", "too low") },
        };
        parameter.Validations.Should().ContainSingle();
    }

    private static string GetDescription(object value) =>
        (string)value.GetType().GetProperty("Description")!.GetValue(value)!;

    private static (object FromName, object FromValue, object Sample, object Other) CreateValueObjectSamples(string typeName) =>
        typeName switch
        {
            nameof(AIConfidenceLevel) => (
                AIConfidenceLevel.FromName("High"),
                AIConfidenceLevel.FromValue(3),
                AIConfidenceLevel.High,
                AIConfidenceLevel.Low),
            nameof(AIEngineType) => (
                AIEngineType.FromName("GPT"),
                AIEngineType.FromValue(1),
                AIEngineType.GPT,
                AIEngineType.Claude),
            nameof(AIProviderType) => (
                AIProviderType.FromName("OpenAI"),
                AIProviderType.FromValue(5),
                AIProviderType.OpenAI,
                AIProviderType.Ollama),
            nameof(BetaProgramStatus) => (
                BetaProgramStatus.FromName("Active"),
                BetaProgramStatus.FromValue(1),
                BetaProgramStatus.Active,
                BetaProgramStatus.Pending),
            nameof(HealthStatus) => (
                HealthStatus.FromName("Good"),
                HealthStatus.FromValue(3),
                HealthStatus.Good,
                HealthStatus.Critical),
            nameof(MethodVisibility) => (
                MethodVisibility.FromName("Public"),
                MethodVisibility.FromValue(1),
                MethodVisibility.Public,
                MethodVisibility.Private),
            nameof(OnboardingStatus) => (
                OnboardingStatus.FromName("Completed"),
                OnboardingStatus.FromValue(2),
                OnboardingStatus.Completed,
                OnboardingStatus.Pending),
            nameof(ProjectStatus) => (
                ProjectStatus.FromName("Active"),
                ProjectStatus.FromValue(3),
                ProjectStatus.Active,
                ProjectStatus.Completed),
            nameof(SprintStatus) => (
                SprintStatus.FromName("Active"),
                SprintStatus.FromValue(1),
                SprintStatus.Active,
                SprintStatus.Cancelled),
            nameof(TaskPriority) => (
                TaskPriority.FromName("High"),
                TaskPriority.FromValue(2),
                TaskPriority.High,
                TaskPriority.Low),
            "DomainTaskStatus" => (
                Nexo.Core.Domain.Values.TaskStatus.FromName("Done"),
                Nexo.Core.Domain.Values.TaskStatus.FromValue(2),
                Nexo.Core.Domain.Values.TaskStatus.Done,
                Nexo.Core.Domain.Values.TaskStatus.Todo),
            nameof(RiskLevel) => (
                RiskLevel.FromName("High"),
                RiskLevel.FromValue(2),
                RiskLevel.High,
                RiskLevel.Low),
            _ => throw new ArgumentOutOfRangeException(nameof(typeName), typeName, null),
        };

    private sealed class TestBrick : Brick
    {
        public override Task<BrickOutput> ExecuteAsync(
            BrickInput input,
            ImplementationType implementation,
            IExecutionContext context,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new BrickOutput());
    }
}
