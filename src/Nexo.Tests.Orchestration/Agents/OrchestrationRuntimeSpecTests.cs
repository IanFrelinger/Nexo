using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nexo.Abstractions;
using Nexo.Orchestration.Agents;
using Nexo.Orchestration.Architect.Models;
using Nexo.Orchestration.Models;
using Xunit;

namespace Nexo.Tests.Orchestration.Agents;

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
                ["combat"] = new ModelRuntimeSpec { Prefer = "agentic", Provider = "offline" }
            }
        };

        using var _ = accessor.Begin(spec);

        var factory = new AgentFactory(sp.GetRequiredService<ILogger<AgentFactory>>(), sp);
        var agentSpec = new AgentSpawnSpec
        {
            AgentId = "combat-1",
            Domain = "Combat",
            Goal = "Design weapon system"
        };

        var agent = factory.CreateAgent(agentSpec);
        await agent.InitializeAsync();
        await agent.ExecuteAsync();

        capture.LastSystem.Should().NotBeNullOrWhiteSpace();
        capture.LastSystem.Should().Contain("nexo.agent.id=combat-1");
        capture.LastSystem.Should().Contain("nexo.agent.domain=Combat");
        capture.LastSystem.Should().Contain("nexo.model.prefer=agentic");
        capture.LastSystem.Should().Contain("nexo.model.provider=offline");
    }

    private sealed class CapturingModel : IModel
    {
        public string? LastSystem { get; private set; }

        public Task<ModelOutput> CompleteAsync(ModelInput input, CancellationToken ct)
        {
            LastSystem = input.Messages.FirstOrDefault(m => m.role == "system").content;
            return Task.FromResult(new ModelOutput("{\"ok\":true}"));
        }
    }
}

