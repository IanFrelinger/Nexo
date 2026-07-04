using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Nexo.Abstractions;
using Nexo.Orchestration.Models;
using Xunit;

namespace Nexo.Tests.Orchestration;

/// <summary>Tests for orchestration models gap coverage.</summary>
public class OrchestrationModelsGapCoverageTests
{
    [Fact]
    public void OrchestrationRuntimeSpec_resolves_agent_domain_and_default()
    {
        var agentSpec = new ModelRuntimeSpec { Prefer = "agentic", Provider = "ollama", Model = "llama" };
        var domainSpec = new ModelRuntimeSpec { Prefer = "deterministic", Provider = "mock-json" };
        var defaultSpec = new ModelRuntimeSpec { Prefer = "auto", Provider = "offline" };

        var spec = new OrchestrationRuntimeSpec
        {
            Model = defaultSpec,
            Domains = new Dictionary<string, ModelRuntimeSpec>(StringComparer.OrdinalIgnoreCase)
            {
                ["combat"] = domainSpec,
            },
            Agents = new Dictionary<string, ModelRuntimeSpec>(StringComparer.OrdinalIgnoreCase)
            {
                ["agent-1"] = agentSpec,
            },
        };

        spec.Resolve("agent-1", "combat").Provider.Should().Be("ollama");
        spec.Resolve("", "combat").Provider.Should().Be("mock-json");
        spec.Resolve("missing", "missing").Provider.Should().Be("offline");
        OrchestrationRuntimeSpec.Default().Model.Prefer.Should().Be("auto");
    }

    [Fact]
    public async Task OrchestrationRuntimeModelDecorator_passthrough_when_auto_without_provider()
    {
        var inner = new Mock<IModel>();
        inner.Setup(m => m.CompleteAsync(It.IsAny<ModelInput>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ModelOutput("ok"));

        var accessor = new OrchestrationRuntimeSpecAccessor();
        var decorator = new OrchestrationRuntimeModelDecorator(
            inner.Object,
            accessor,
            NullLogger<OrchestrationRuntimeModelDecorator>.Instance);

        var input = new ModelInput(new[] { ("user", "hello") });
        var output = await decorator.CompleteAsync(input, CancellationToken.None);

        output.Text.Should().Be("ok");
        inner.Verify(m => m.CompleteAsync(input, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task OrchestrationRuntimeModelDecorator_injects_directives_into_existing_system_message()
    {
        var captured = new List<ModelInput>();
        var inner = new Mock<IModel>();
        inner.Setup(m => m.CompleteAsync(It.IsAny<ModelInput>(), It.IsAny<CancellationToken>()))
            .Callback<ModelInput, CancellationToken>((input, _) => captured.Add(input))
            .ReturnsAsync(new ModelOutput("routed"));

        using var scope = new OrchestrationRuntimeSpecAccessor().Begin(new OrchestrationRuntimeSpec
        {
            Model = new ModelRuntimeSpec { Prefer = "deterministic", Provider = "mock-json", Model = "test-model" },
        });

        var decorator = new OrchestrationRuntimeModelDecorator(
            inner.Object,
            new OrchestrationRuntimeSpecAccessor(),
            NullLogger<OrchestrationRuntimeModelDecorator>.Instance);

        await decorator.CompleteAsync(
            new ModelInput(new[] { ("system", "base"), ("user", "go") }),
            CancellationToken.None);

        captured.Should().ContainSingle();
        captured[0].Messages.First(m => m.role == "system").content
            .Should().Contain("nexo.model.provider=mock-json");
    }

    [Fact]
    public void InjectDirectives_inserts_system_message_when_missing()
    {
        var routed = OrchestrationRuntimeModelDecorator.InjectDirectives(
            new ModelInput(new[] { ("user", "prompt") }),
            prefer: "agentic",
            provider: "ollama",
            model: "llama3");

        routed.Messages.Should().HaveCount(2);
        routed.Messages[0].role.Should().Be("system");
        routed.Messages[0].content.Should().Contain("nexo.model.name=llama3");
    }

    [Fact]
    public async Task AgentScopedModel_prepends_agent_and_runtime_directives()
    {
        var captured = new List<ModelInput>();
        var inner = new Mock<IModel>();
        inner.Setup(m => m.CompleteAsync(It.IsAny<ModelInput>(), It.IsAny<CancellationToken>()))
            .Callback<ModelInput, CancellationToken>((input, _) => captured.Add(input))
            .ReturnsAsync(new ModelOutput("scoped"));

        var scoped = new AgentScopedModel(
            inner.Object,
            "agent-42",
            "security",
            new ModelRuntimeSpec { Prefer = "deterministic", Provider = "azure", Model = "gpt" });

        await scoped.CompleteAsync(new ModelInput(new[] { ("user", "scan") }), CancellationToken.None);

        captured[0].Messages[0].content.Should().Contain("nexo.agent.id=agent-42");
        captured[0].Messages[0].content.Should().Contain("nexo.model.provider=azure");
    }

    [Fact]
    public void OrchestrationRuntimeSpecAccessor_begin_restores_prior_scope()
    {
        var accessor = new OrchestrationRuntimeSpecAccessor();
        accessor.Current.Model.Prefer.Should().Be("auto");

        using (accessor.Begin(new OrchestrationRuntimeSpec
        {
            Model = new ModelRuntimeSpec { Prefer = "deterministic" },
        }))
        {
            accessor.Current.Model.Prefer.Should().Be("deterministic");
        }

        accessor.Current.Model.Prefer.Should().Be("auto");
    }
}
