using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nexo.BackgroundAgents.Configuration;
using Nexo.BackgroundAgents.DataSensitivity;
using Nexo.BackgroundAgents.Extending;
using Nexo.BackgroundAgents.Logging;
using Nexo.BackgroundAgents.Registry;
using Nexo.BackgroundAgents.Scheduling;
using Nexo.Orchestration.Agents;
using Xunit;

namespace Nexo.Tests.BackgroundAgents.Configuration;

/// <summary>
/// P3.1: Verify all four background agent aggressiveness modes.
/// </summary>
public sealed class AggressivenessModeTests
{
    [Fact]
    public async Task BackgroundAgent_PassiveMode_ExtenderSkipped_TakesNoAction()
    {
        var modeStore = new InMemoryAggressivenessModeStore();
        modeStore.SetMode(BackgroundAgentAggressivenessMode.Passive);

        var runCount = 0;
        var mockExtend = new MockSelfExtendRunner(() => runCount++);

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["BackgroundAgents:Agents:0:Id"] = "extender-passive",
                ["BackgroundAgents:Agents:0:Name"] = "Extender",
                ["BackgroundAgents:Agents:0:Role"] = "extender",
                ["BackgroundAgents:Agents:0:Enabled"] = "true",
                ["BackgroundAgents:Agents:0:MaxDataSensitivity"] = "Public",
                ["BackgroundAgents:Agents:0:Commands:0"] = "extend",
                ["BackgroundAgents:Agents:0:Parameters:RepoRoot"] = Environment.CurrentDirectory,
                ["BackgroundAgents:Agents:0:Schedule:Type"] = "Interval",
                ["BackgroundAgents:Agents:0:Schedule:Interval"] = "00:01:00",
            })
            .Build();

        var services = new ServiceCollection()
            .AddLogging(b => b.AddConsole().SetMinimumLevel(LogLevel.Warning))
            .BuildServiceProvider();

        var sensitivityRegistry = new DataSensitivityRegistry();
        var configLoader = new BackgroundAgentConfigLoader(config, sensitivityRegistry, null);
        var specBuilder = new BackgroundAgentSpecBuilder(sensitivityRegistry, null);
        var logStore = new InMemoryAgentLogStore();
        var scheduleExecutor = new ScheduleExecutor();
        var scheduler = new AgentScheduler(scheduleExecutor, null);
        var registry = new BackgroundAgentRegistry(scheduler, null, logStore, null, null, mockExtend, modeStore);

        var configs = await configLoader.LoadAsync(default);
        var agentConfig = configs[0];
        var spec = specBuilder.BuildSpec(agentConfig);
        var agent = new GenericAgent(spec, services.GetRequiredService<ILogger<GenericAgent>>());
        await registry.RegisterAsync(agent, agentConfig, default);

        await registry.ExecuteOnceAsync("extender-passive", default);

        runCount.Should().Be(0, "Passive mode must skip extender execution");
        var logs = logStore.GetRecent("extender-passive", 20, null, null);
        logs.Should().Contain(l => l.Message.Contains("Passive mode") || l.Message.Contains("observe only"));
    }

    [Fact]
    public async Task BackgroundAgent_ActiveMode_ExtenderRuns()
    {
        var modeStore = new InMemoryAggressivenessModeStore();
        modeStore.SetMode(BackgroundAgentAggressivenessMode.Active);

        var runCount = 0;
        var mockExtend = new MockSelfExtendRunner(() => runCount++);

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["BackgroundAgents:Agents:0:Id"] = "extender-active",
                ["BackgroundAgents:Agents:0:Name"] = "Extender",
                ["BackgroundAgents:Agents:0:Role"] = "extender",
                ["BackgroundAgents:Agents:0:Enabled"] = "true",
                ["BackgroundAgents:Agents:0:MaxDataSensitivity"] = "Public",
                ["BackgroundAgents:Agents:0:Commands:0"] = "extend",
                ["BackgroundAgents:Agents:0:Parameters:RepoRoot"] = Environment.CurrentDirectory,
                ["BackgroundAgents:Agents:0:Schedule:Type"] = "Interval",
                ["BackgroundAgents:Agents:0:Schedule:Interval"] = "00:01:00",
            })
            .Build();

        var services = new ServiceCollection()
            .AddLogging(b => b.AddConsole().SetMinimumLevel(LogLevel.Warning))
            .BuildServiceProvider();

        var sensitivityRegistry = new DataSensitivityRegistry();
        var configLoader = new BackgroundAgentConfigLoader(config, sensitivityRegistry, null);
        var specBuilder = new BackgroundAgentSpecBuilder(sensitivityRegistry, null);
        var logStore = new InMemoryAgentLogStore();
        var scheduleExecutor = new ScheduleExecutor();
        var scheduler = new AgentScheduler(scheduleExecutor, null);
        var registry = new BackgroundAgentRegistry(scheduler, null, logStore, null, null, mockExtend, modeStore);

        var configs = await configLoader.LoadAsync(default);
        var agentConfig = configs[0];
        var spec = specBuilder.BuildSpec(agentConfig);
        var agent = new GenericAgent(spec, services.GetRequiredService<ILogger<GenericAgent>>());
        await registry.RegisterAsync(agent, agentConfig, default);

        await registry.ExecuteOnceAsync("extender-active", default);

        runCount.Should().Be(1, "Active mode must run extender");
    }

    [Fact]
    public void AllFourModes_CanBeSetAndRetrieved()
    {
        var modeStore = new InMemoryAggressivenessModeStore();

        modeStore.SetMode(BackgroundAgentAggressivenessMode.Passive);
        modeStore.GetMode().Should().Be(BackgroundAgentAggressivenessMode.Passive);

        modeStore.SetMode(BackgroundAgentAggressivenessMode.SemiActive);
        modeStore.GetMode().Should().Be(BackgroundAgentAggressivenessMode.SemiActive);

        modeStore.SetMode(BackgroundAgentAggressivenessMode.Active);
        modeStore.GetMode().Should().Be(BackgroundAgentAggressivenessMode.Active);

        modeStore.SetMode(BackgroundAgentAggressivenessMode.Ambient);
        modeStore.GetMode().Should().Be(BackgroundAgentAggressivenessMode.Ambient);
    }

    [Fact]
    public void Mode_SwitchableAtRuntime_NoRestartRequired()
    {
        var modeStore = new InMemoryAggressivenessModeStore();
        modeStore.SetMode(BackgroundAgentAggressivenessMode.Passive);
        modeStore.SetMode(BackgroundAgentAggressivenessMode.Active);
        modeStore.GetMode().Should().Be(BackgroundAgentAggressivenessMode.Active);
    }

    private sealed class MockSelfExtendRunner : ISelfExtendRunner
    {
        private readonly Action _onRun;

        public MockSelfExtendRunner(Action onRun) => _onRun = onRun;

        public Task<SelfExtendRunResult> RunAsync(string repoRoot, CancellationToken cancellationToken = default)
        {
            _onRun();
            return Task.FromResult(new SelfExtendRunResult(true, 0, 0, "Mock"));
        }
    }
}
