using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Moq;
using Nexo.BackgroundAgents.Configuration;
using Nexo.BackgroundAgents.DataSensitivity;
using Nexo.BackgroundAgents.Registry;
using Nexo.BackgroundAgents.Tools;
using Nexo.Orchestration.Agents;
using Xunit;

namespace Nexo.Tests.BackgroundAgents.Tools;

public class AgentManagementToolboxTests
{
    [Fact]
    public void Schemas_ReturnsFourTools()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>()).Build();
        var sensitivityRegistry = new DataSensitivityRegistry();
        var configLoader = new BackgroundAgentConfigLoader(config, sensitivityRegistry, null);
        var registry = new Mock<IBackgroundAgentRegistry>().Object;
        var specBuilder = new BackgroundAgentSpecBuilder(sensitivityRegistry, null);
        var agentFactory = new AgentFactory(Mock.Of<Microsoft.Extensions.Logging.ILogger<AgentFactory>>(), Mock.Of<IServiceProvider>());
        var toolbox = new AgentManagementToolbox(registry, configLoader, specBuilder, agentFactory);

        var schemas = toolbox.Schemas().ToList();

        schemas.Should().HaveCount(4);
        schemas.Select(s => s.Id).Should().Contain("enable_agent");
        schemas.Select(s => s.Id).Should().Contain("disable_agent");
        schemas.Select(s => s.Id).Should().Contain("restart_agent");
        schemas.Select(s => s.Id).Should().Contain("update_agent_config");
    }
}
