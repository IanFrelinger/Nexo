using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Nexo.Abstractions.Agents;
using Nexo.Orchestration.Agents;
using Nexo.Orchestration.Architect.Models;
using Xunit;

namespace Nexo.Tests.Orchestration.Agents;

public sealed class AgentRuntimeFactoryTests
{
    [Fact]
    public void AgentFactory_ImplementsIAgentRuntimeFactory_SpawnReturnsInProcessHandle()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<ILogger<AgentContainer>>(sp => sp.GetRequiredService<ILoggerFactory>().CreateLogger<AgentContainer>());
        var sp = services.BuildServiceProvider();

        IAgentRuntimeFactory factory = new AgentFactory(
            sp.GetRequiredService<ILogger<AgentFactory>>(),
            sp);

        var spec = new AgentSpawnSpec
        {
            AgentId = "a1",
            Domain = "General",
            Goal = "g"
        };

        var handle = factory.Spawn(spec);

        handle.Should().BeOfType<InProcessAgentHandle>();
        handle.AgentId.Should().Be("a1");
        ((InProcessAgentHandle)handle).Container.Agent.Spec.AgentId.Should().Be("a1");
    }
}
