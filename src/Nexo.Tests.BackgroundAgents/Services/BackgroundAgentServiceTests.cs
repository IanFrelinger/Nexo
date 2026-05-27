using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Nexo.Abstractions;
using Nexo.BackgroundAgents.Configuration;
using Nexo.BackgroundAgents.DataSensitivity;
using Nexo.BackgroundAgents.Registry;
using Nexo.BackgroundAgents.Services;
using Nexo.Orchestration.Agents;
using Xunit;

namespace Nexo.Tests.BackgroundAgents.Services;

public sealed class BackgroundAgentServiceTests
{
    [Fact]
    public void Constructor_rejects_null_dependencies()
    {
        var sensitivity = new DataSensitivityRegistry();
        var loader = CreateLoaderForAgent("probe-agent");
        var builder = new BackgroundAgentSpecBuilder(sensitivity, null);
        var factory = CreateAgentFactory();
        var registry = Mock.Of<IBackgroundAgentRegistry>();
        var logger = Mock.Of<ILogger<BackgroundAgentService>>();

        var act = () => new BackgroundAgentService(null!, builder, factory, registry, sensitivity, logger);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public async Task ExecuteAsync_registers_enabled_agents_and_starts_registry()
    {
        var registry = new Mock<IBackgroundAgentRegistry>();
        registry.Setup(r => r.RegisterAsync(It.IsAny<IAgent>(), It.IsAny<BackgroundAgentConfig>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        registry.Setup(r => r.StartAllAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var service = CreateService(registry.Object, CreateLoaderForAgent("enabled-agent"));
        using var cts = new CancellationTokenSource();

        await service.StartAsync(cts.Token);
        await Task.Delay(150);
        cts.Cancel();

        await service.ExecuteTask!;
        await service.StopAsync(CancellationToken.None);

        registry.Verify(r => r.RegisterAsync(
            It.IsAny<IAgent>(),
            It.Is<BackgroundAgentConfig>(c => c.Id == "enabled-agent"),
            It.IsAny<CancellationToken>()), Times.Once);
        registry.Verify(r => r.StartAllAsync(It.IsAny<CancellationToken>()), Times.Once);
        registry.Verify(r => r.StopAllAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_skips_disabled_agents()
    {
        var registry = new Mock<IBackgroundAgentRegistry>();
        registry.Setup(r => r.RegisterAsync(It.IsAny<IAgent>(), It.IsAny<BackgroundAgentConfig>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        registry.Setup(r => r.StartAllAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["BackgroundAgents:Agents:0:Id"] = "disabled-agent",
                ["BackgroundAgents:Agents:0:Name"] = "Disabled",
                ["BackgroundAgents:Agents:0:Role"] = "monitor",
                ["BackgroundAgents:Agents:0:Enabled"] = "false",
                ["BackgroundAgents:Agents:0:MaxDataSensitivity"] = "Public",
                ["BackgroundAgents:Agents:0:Commands:0"] = "ping",
                ["BackgroundAgents:Agents:0:Schedule:Type"] = "Interval",
                ["BackgroundAgents:Agents:0:Schedule:Interval"] = "00:01:00",
            })
            .Build();

        var service = CreateService(registry.Object, CreateLoader(config));
        using var cts = new CancellationTokenSource();

        await service.StartAsync(cts.Token);
        await Task.Delay(100);
        cts.Cancel();
        await service.ExecuteTask!;
        await service.StopAsync(CancellationToken.None);

        registry.Verify(
            r => r.RegisterAsync(It.IsAny<IAgent>(), It.IsAny<BackgroundAgentConfig>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_continues_when_one_agent_registration_fails()
    {
        var registry = new Mock<IBackgroundAgentRegistry>();
        registry
            .Setup(r => r.RegisterAsync(It.IsAny<IAgent>(), It.Is<BackgroundAgentConfig>(c => c.Id == "bad-agent"), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("register failed"));
        registry
            .Setup(r => r.RegisterAsync(It.IsAny<IAgent>(), It.Is<BackgroundAgentConfig>(c => c.Id == "good-agent"), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        registry.Setup(r => r.StartAllAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["BackgroundAgents:Agents:0:Id"] = "bad-agent",
                ["BackgroundAgents:Agents:0:Name"] = "Bad",
                ["BackgroundAgents:Agents:0:Role"] = "monitor",
                ["BackgroundAgents:Agents:0:Enabled"] = "true",
                ["BackgroundAgents:Agents:0:MaxDataSensitivity"] = "Public",
                ["BackgroundAgents:Agents:0:Commands:0"] = "ping",
                ["BackgroundAgents:Agents:0:Schedule:Type"] = "Interval",
                ["BackgroundAgents:Agents:0:Schedule:Interval"] = "00:01:00",
                ["BackgroundAgents:Agents:1:Id"] = "good-agent",
                ["BackgroundAgents:Agents:1:Name"] = "Good",
                ["BackgroundAgents:Agents:1:Role"] = "monitor",
                ["BackgroundAgents:Agents:1:Enabled"] = "true",
                ["BackgroundAgents:Agents:1:MaxDataSensitivity"] = "Public",
                ["BackgroundAgents:Agents:1:Commands:0"] = "ping",
                ["BackgroundAgents:Agents:1:Schedule:Type"] = "Interval",
                ["BackgroundAgents:Agents:1:Schedule:Interval"] = "00:01:00",
            })
            .Build();

        var service = CreateService(registry.Object, CreateLoader(config));
        using var cts = new CancellationTokenSource();

        await service.StartAsync(cts.Token);
        await Task.Delay(150);
        cts.Cancel();
        await service.ExecuteTask!;
        await service.StopAsync(CancellationToken.None);

        registry.Verify(
            r => r.RegisterAsync(It.IsAny<IAgent>(), It.Is<BackgroundAgentConfig>(c => c.Id == "good-agent"), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_loader_failure_rethrows_and_stops_service()
    {
        var badLoader = new BackgroundAgentConfigLoader(
            new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["BackgroundAgents:Agents:0:Id"] = "",
                    ["BackgroundAgents:Agents:0:Name"] = "Broken",
                    ["BackgroundAgents:Agents:0:Role"] = "monitor",
                    ["BackgroundAgents:Agents:0:MaxDataSensitivity"] = "Public",
                    ["BackgroundAgents:Agents:0:Commands:0"] = "ping",
                    ["BackgroundAgents:Agents:0:Schedule:Type"] = "Interval",
                    ["BackgroundAgents:Agents:0:Schedule:Interval"] = "00:01:00",
                })
                .Build(),
            new DataSensitivityRegistry(),
            null);

        var service = CreateService(Mock.Of<IBackgroundAgentRegistry>(), badLoader);
        using var cts = new CancellationTokenSource();

        await service.StartAsync(cts.Token);
        var act = async () => await service.ExecuteTask!;
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*Agent ID is required*");
        await service.StopAsync(CancellationToken.None);
    }

    private static BackgroundAgentService CreateService(IBackgroundAgentRegistry registry, BackgroundAgentConfigLoader loader)
    {
        var sensitivity = new DataSensitivityRegistry();
        return new BackgroundAgentService(
            loader,
            new BackgroundAgentSpecBuilder(sensitivity, null),
            CreateAgentFactory(),
            registry,
            sensitivity,
            Mock.Of<ILogger<BackgroundAgentService>>());
    }

    private static BackgroundAgentConfigLoader CreateLoaderForAgent(string agentId)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["BackgroundAgents:Agents:0:Id"] = agentId,
                ["BackgroundAgents:Agents:0:Name"] = "Service Agent",
                ["BackgroundAgents:Agents:0:Role"] = "monitor",
                ["BackgroundAgents:Agents:0:Enabled"] = "true",
                ["BackgroundAgents:Agents:0:MaxDataSensitivity"] = "Public",
                ["BackgroundAgents:Agents:0:Commands:0"] = "ping",
                ["BackgroundAgents:Agents:0:Schedule:Type"] = "Interval",
                ["BackgroundAgents:Agents:0:Schedule:Interval"] = "00:01:00",
            })
            .Build();
        return CreateLoader(config);
    }

    private static BackgroundAgentConfigLoader CreateLoader(IConfiguration config) =>
        new(config, new DataSensitivityRegistry(), null);

    private static AgentFactory CreateAgentFactory()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        var sp = services.BuildServiceProvider();
        return new AgentFactory(sp.GetRequiredService<ILogger<AgentFactory>>(), sp);
    }
}
