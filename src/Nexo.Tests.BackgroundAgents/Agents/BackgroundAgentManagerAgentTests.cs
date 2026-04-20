using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Nexo.BackgroundAgents.Agents;
using Nexo.Orchestration.Agents;
using Nexo.Abstractions.Agents;
using Nexo.Orchestration.Architect.Models;
using Xunit;

namespace Nexo.Tests.BackgroundAgents.Agents;

public class BackgroundAgentManagerAgentTests
{
    [Fact]
    public void Name_EqualsSpecAgentId()
    {
        var spec = new AgentSpawnSpec
        {
            AgentId = "meta-agent",
            Domain = "BackgroundAgentManager",
            Goal = "Manage background agents"
        };
        var logger = new Mock<ILogger<BaseAgent>>();
        var agent = new BackgroundAgentManagerAgent(spec, logger.Object);

        agent.Name.Should().Be("meta-agent");
    }

    [Fact]
    public async Task ExecuteAsync_WithoutModel_ReturnsMockOutput()
    {
        var spec = new AgentSpawnSpec
        {
            AgentId = "meta-agent",
            Domain = "BackgroundAgentManager",
            Goal = "Manage background agents"
        };
        var logger = new Mock<ILogger<BaseAgent>>();
        var agent = new BackgroundAgentManagerAgent(spec, logger.Object, model: null);
        await agent.InitializeAsync(default);

        var output = await agent.ExecuteAsync(null, default);

        output.Should().NotBeNull();
        agent.State.Should().Be(AgentState.Completed);
    }
}
