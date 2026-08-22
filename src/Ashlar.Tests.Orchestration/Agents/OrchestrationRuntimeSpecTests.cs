using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Ashlar.Abstractions;
using Ashlar.Orchestration.Agents;
using Ashlar.Orchestration.Architect.Models;
using Ashlar.Orchestration.Models;
using Xunit;

namespace Ashlar.Tests.Orchestration.Agents;

/// <summary>Tests for orchestration runtime spec.</summary>
public sealed class OrchestrationRuntimeSpecTests
{
    [Fact]
    public async Task AgentFactory_InsertsRuntimeDirectives_PerDomain()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IOrchestrationRuntimeSpecAccessor, OrchestrationRuntimeSpecAccessor>();

        var capture = new CapturingModel();
        services.AddSingleton<IModel>(capture);

        var sp = services.BuildServiceProvider();
        var accessor = sp.GetRequiredService<IOrchestrationRuntimeSpecAccessor>();

        var spec = new OrchestrationRuntimeSpec
        {
            Domains =
            {
                ["security"] = new ModelRuntimeSpec { Prefer = "agentic", Provider = "offline" }
            }
        };

        using var _ = accessor.Begin(spec);

        var factory = new AgentFactory(sp.GetRequiredService<ILogger<AgentFactory>>(), sp);
        var agentSpec = new AgentSpawnSpec
        {
            AgentId = "security-1",
            Domain = "Security",
            Goal = "Design auth flow"
        };

        var agent = factory.CreateAgent(agentSpec);
        await agent.InitializeAsync();
        await agent.ExecuteAsync();

        capture.LastSystem.Should().NotBeNullOrWhiteSpace();
        capture.LastSystem.Should().Contain("ashlar.agent.id=security-1");
        capture.LastSystem.Should().Contain("ashlar.agent.domain=Security");
        capture.LastSystem.Should().Contain("ashlar.model.prefer=agentic");
        capture.LastSystem.Should().Contain("ashlar.model.provider=offline");
    }

    [Fact]
    public async Task AgentFactory_InsertsOllamaModelDirective_WhenSpecifiedOnAgent()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IOrchestrationRuntimeSpecAccessor, OrchestrationRuntimeSpecAccessor>();

        var capture = new CapturingModel();
        services.AddSingleton<IModel>(capture);

        var sp = services.BuildServiceProvider();
        var factory = new AgentFactory(sp.GetRequiredService<ILogger<AgentFactory>>(), sp);
        var agentSpec = new AgentSpawnSpec
        {
            AgentId = "infra-1",
            Domain = "Infrastructure",
            Goal = "Plan capacity",
            OllamaModel = "llama3.2:3b"
        };

        var agent = factory.CreateAgent(agentSpec);
        await agent.InitializeAsync();
        await agent.ExecuteAsync();

        capture.LastSystem.Should().NotBeNullOrWhiteSpace();
        capture.LastSystem.Should().Contain("ashlar.model.provider=ollama");
        capture.LastSystem.Should().Contain("ashlar.model.name=llama3.2:3b");
    }

    /// <summary>Capturing model.</summary>
    private sealed class CapturingModel : IModel
    {
        /// <summary>Last system.</summary>
        public string? LastSystem { get; private set; }

        public Task<ModelOutput> CompleteAsync(ModelInput input, CancellationToken ct)
        {
            LastSystem = input.Messages.FirstOrDefault(m => m.role == "system").content;
            return Task.FromResult(new ModelOutput("{\"ok\":true}"));
        }
    }
}

