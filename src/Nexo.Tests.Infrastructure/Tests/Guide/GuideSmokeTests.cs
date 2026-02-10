using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Nexo.Core.Application.Common.Ports;
using Nexo.Core.Application.Common.Services;
using Nexo.Core.Domain.Agents;
using Nexo.Core.Domain.Bricks;
using Nexo.Core.Domain.Execution;
using Nexo.Guide;
using Nexo.Guide.Bricks;
using Nexo.Guide.Knowledge;
using Nexo.Guide.Ollama;
using Nexo.Guide.Unity;
using Nexo.Infrastructure.Execution;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.Guide;

/// <summary>
/// Smoke tests for the Nexo Guide application: agent setup, Unity scene sink, and chat brick.
/// Validates the refactored Guide that dog-foods Nexo infrastructure (IBehaviorExecutor, domain Brick).
/// </summary>
public class GuideSmokeTests
{
    [Fact]
    public void GuideNexoSetup_ShouldCreateAgentCard()
    {
        var card = GuideNexoSetup.CreateAgentCard();

        card.Should().NotBeNull();
        card.Id.Should().Be(GuideNexoSetup.GuideAgentId);
        card.Name.Should().Be("Nexo Guide");
        card.Behaviors.Should().Contain(GuideNexoSetup.GuideChatBehaviorId);
        card.Memory.Should().NotBeNull();
        card.Constraints.Should().NotBeNull();
    }

    [Fact]
    public void GuideNexoSetup_ShouldCreateGuideChatBehavior()
    {
        var behavior = GuideNexoSetup.CreateGuideChatBehavior();

        behavior.Should().NotBeNull();
        behavior.Id.Should().Be(GuideNexoSetup.GuideChatBehaviorId);
        behavior.Steps.Should().NotBeEmpty();
        behavior.Steps[0].BrickId.Should().Be("nexo-guide.chat");
    }

    [Fact]
    public void InMemoryUnitySceneSink_ShouldPushAndSnapshot()
    {
        var sink = new InMemoryUnitySceneSink();
        var payload = JsonSerializer.SerializeToElement(new { test = true });

        sink.Push("add-mesh", payload);
        var snapshot = sink.Snapshot();

        snapshot.Should().HaveCount(1);
        snapshot[0].Type.Should().Be("add-mesh");

        sink.Clear();
        sink.Snapshot().Should().BeEmpty();
    }

    [Fact]
    public void NexoKnowledgeBase_ShouldReturnRelevantContext()
    {
        var kb = new NexoKnowledgeBase();
        var context = kb.GetRelevantContext("What is Nexo?");

        context.Should().NotBeNullOrWhiteSpace();
        context.Should().Contain("Nexo");
    }

    [Fact]
    public void GuideChatBrick_ShouldHaveCorrectInterface()
    {
        var mockOllama = new Mock<IOllamaClient>();
        var knowledge = new NexoKnowledgeBase();
        var brick = new GuideChatBrick(mockOllama.Object, knowledge);

        brick.Id.Should().Be("nexo-guide.chat");
        brick.Interface.Should().NotBeNull();
        brick.Interface.Inputs.Should().Contain(i => i.Name == "userMessage");
        brick.Interface.Outputs.Should().Contain(o => o.Name == "text");
    }

    [Fact]
    public async Task GuideChatBrick_ExecuteAsync_ShouldReturnOutputs_WhenOllamaReturnsText()
    {
        var mockOllama = new Mock<IOllamaClient>();
        mockOllama
            .Setup(x => x.GenerateAsync(It.IsAny<IReadOnlyList<OllamaMessage>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("Hello from Guide.");
        var knowledge = new NexoKnowledgeBase();
        var brick = new GuideChatBrick(mockOllama.Object, knowledge);

        var input = new BrickInput(new Dictionary<string, object>
        {
            ["userMessage"] = "Hi",
            ["audience"] = "general",
            ["historyJson"] = "[]",
            ["topicsCoveredJson"] = "[]"
        });
        var context = new Mock<IExecutionContext>().Object;

        var output = await brick.ExecuteAsync(
            input,
            ImplementationType.Agentic,
            context,
            CancellationToken.None);

        output.Should().NotBeNull();
        output["text"].Should().NotBeNull();
    }

    /// <summary>
    /// Headless DI pass: build the same execution pipeline as the Guide app (App.axaml.cs)
    /// and resolve all services that would be resolved at startup. Validates that DI is
    /// correctly configured without running the UI.
    /// </summary>
    [Fact]
    public void Guide_HeadlessDI_ShouldResolveExecutionPipeline()
    {
        var services = new ServiceCollection();
        services.AddLogging(b => b.AddConsole().SetMinimumLevel(LogLevel.Warning));

        // Same registrations as App.axaml.cs for execution pipeline
        services.AddSingleton<IOllamaClient>(_ => new OllamaClient("http://localhost:11434", "llama3.1:8b"));
        services.AddSingleton<NexoKnowledgeBase>();
        services.AddSingleton<IProviderFactory, ProviderFactory>();
        services.AddSingleton<ILoopKernel, SequentialLoopKernel>();
        services.AddSingleton<ISemanticCache, SemanticCache>();
        services.AddSingleton<Nexo.Core.Domain.Execution.IBrickRegistry>(sp =>
        {
            var ollama = sp.GetRequiredService<IOllamaClient>();
            var knowledge = sp.GetRequiredService<NexoKnowledgeBase>();
            var bricks = new Nexo.Core.Domain.Bricks.Brick[]
            {
                new GuideChatBrick(ollama, knowledge)
            };
            return new BrickRegistry(bricks);
        });
        services.AddSingleton<Nexo.Core.Domain.Execution.IBehaviorRegistry>(sp =>
        {
            var behaviors = new[] { GuideNexoSetup.CreateGuideChatBehavior() };
            return new BehaviorRegistry(behaviors);
        });
        services.AddSingleton<Nexo.Core.Domain.Execution.IBehaviorExecutor, BehaviorExecutor>();
        services.AddSingleton<AgentCard>(_ => GuideNexoSetup.CreateAgentCard());

        using var provider = services.BuildServiceProvider();

        var executor = provider.GetRequiredService<Nexo.Core.Domain.Execution.IBehaviorExecutor>();
        var behaviorRegistry = provider.GetRequiredService<Nexo.Core.Domain.Execution.IBehaviorRegistry>();
        var brickRegistry = provider.GetRequiredService<Nexo.Core.Domain.Execution.IBrickRegistry>();
        var agentCard = provider.GetRequiredService<AgentCard>();

        executor.Should().NotBeNull();
        behaviorRegistry.Should().NotBeNull();
        brickRegistry.Should().NotBeNull();
        agentCard.Should().NotBeNull();
        agentCard.Id.Should().Be(GuideNexoSetup.GuideAgentId);

        var behavior = behaviorRegistry.GetBehavior(GuideNexoSetup.GuideChatBehaviorId);
        behavior.Should().NotBeNull();
        var brick = brickRegistry.GetBrick("nexo-guide.chat");
        brick.Should().NotBeNull();
    }
}
