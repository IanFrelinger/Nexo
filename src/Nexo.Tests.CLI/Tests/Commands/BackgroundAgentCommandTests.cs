using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Nexo.CLI.Commands.BackgroundAgent;
using Nexo.BackgroundAgents.Configuration;
using Nexo.BackgroundAgents.DataSensitivity;
using Nexo.BackgroundAgents.Registry;
using Nexo.Core.Application.Testing.Abstractions;
using Nexo.Core.Application.Testing.Models;
using Nexo.Orchestration.Agents;

namespace Nexo.Tests.CLI.Tests.Commands;

public class BackgroundAgentCommandTests : UnitTestBase
{
    public override async Task<TestResult> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await TestListEmpty();
            await TestListFormatJson();
            return new TestResult
            {
                TestName = nameof(BackgroundAgentCommandTests),
                Category = "CLI",
                Passed = true,
                Message = "All BackgroundAgentCommand tests passed"
            };
        }
        catch (AssertionException ex)
        {
            return new TestResult
            {
                TestName = nameof(BackgroundAgentCommandTests),
                Category = "CLI",
                Passed = false,
                ErrorMessage = $"Assertion failed: {ex.Message}",
                StackTrace = ex.StackTrace
            };
        }
        catch (Exception ex)
        {
            return new TestResult
            {
                TestName = nameof(BackgroundAgentCommandTests),
                Category = "CLI",
                Passed = false,
                ErrorMessage = ex.Message,
                StackTrace = ex.StackTrace
            };
        }
    }

    private static IConfiguration CreateEmptyConfiguration()
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();
    }

    private async Task TestListEmpty()
    {
        var config = CreateEmptyConfiguration();
        var sensitivityRegistry = new DataSensitivityRegistry();
        var configLoader = new BackgroundAgentConfigLoader(config, sensitivityRegistry, null);
        var registry = new Mock<IBackgroundAgentRegistry>();
        registry.Setup(r => r.GetAll()).Returns(Array.Empty<BackgroundAgentInstance>().ToList());
        var specBuilder = new BackgroundAgentSpecBuilder(sensitivityRegistry, null);
        var loggerFactory = new Mock<ILogger<AgentFactory>>();
        var serviceProvider = new Mock<IServiceProvider>();
        var agentFactory = new AgentFactory(loggerFactory.Object, serviceProvider.Object);
        var logger = new Mock<ILogger<BackgroundAgentCommand>>();

        var command = new BackgroundAgentCommand(
            configLoader,
            registry.Object,
            specBuilder,
            agentFactory,
            logger.Object);

        var exitCode = await command.ListAsync(false, null, null, null);
        AssertEqual(0, exitCode);
    }

    private async Task TestListFormatJson()
    {
        var config = CreateEmptyConfiguration();
        var sensitivityRegistry = new DataSensitivityRegistry();
        var configLoader = new BackgroundAgentConfigLoader(config, sensitivityRegistry, null);
        var registry = new Mock<IBackgroundAgentRegistry>();
        registry.Setup(r => r.GetAll()).Returns(Array.Empty<BackgroundAgentInstance>().ToList());
        var specBuilder = new BackgroundAgentSpecBuilder(sensitivityRegistry, null);
        var loggerFactory = new Mock<ILogger<AgentFactory>>();
        var serviceProvider = new Mock<IServiceProvider>();
        var agentFactory = new AgentFactory(loggerFactory.Object, serviceProvider.Object);
        var logger = new Mock<ILogger<BackgroundAgentCommand>>();

        var command = new BackgroundAgentCommand(
            configLoader,
            registry.Object,
            specBuilder,
            agentFactory,
            logger.Object);

        var exitCode = await command.ListAsync(true, null, null, null);
        AssertEqual(0, exitCode);
    }
}
