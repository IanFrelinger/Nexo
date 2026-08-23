using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Ashlar.BackgroundAgents.Configuration;
using Ashlar.BackgroundAgents.DataSensitivity;
using Xunit;

namespace Ashlar.Tests.BackgroundAgents.Security;

/// <summary>
/// Regression tests for config loading: malformed, missing required fields, invalid schedule, unknown sensitivity.
/// </summary>
public class ConfigLoaderRegressionTests
{
    private static BackgroundAgentConfigLoader CreateLoader(IConfiguration config)
    {
        var sensitivityRegistry = new DataSensitivityRegistry();
        return new BackgroundAgentConfigLoader(config, sensitivityRegistry, null);
    }

    [Fact]
    public async Task LoadAsync_MissingId_Throws()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["BackgroundAgents:Agents:0:Name"] = "Test",
                ["BackgroundAgents:Agents:0:Role"] = "monitor",
                ["BackgroundAgents:Agents:0:Commands:0"] = "ping",
                ["BackgroundAgents:Agents:0:Schedule:Type"] = "Interval",
                ["BackgroundAgents:Agents:0:Schedule:Interval"] = "00:01:00",
                ["BackgroundAgents:Agents:0:MaxDataSensitivity"] = "Public"
            })
            .Build();
        var loader = CreateLoader(config);
        var act = () => loader.LoadAsync(default);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Agent ID is required*");
    }

    [Fact]
    public async Task LoadAsync_MissingName_Throws()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["BackgroundAgents:Agents:0:Id"] = "agent-1",
                ["BackgroundAgents:Agents:0:Role"] = "monitor",
                ["BackgroundAgents:Agents:0:Commands:0"] = "ping",
                ["BackgroundAgents:Agents:0:Schedule:Type"] = "Interval",
                ["BackgroundAgents:Agents:0:Schedule:Interval"] = "00:01:00",
                ["BackgroundAgents:Agents:0:MaxDataSensitivity"] = "Public"
            })
            .Build();
        var loader = CreateLoader(config);
        var act = () => loader.LoadAsync(default);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*must have a name*");
    }

    [Fact]
    public async Task LoadAsync_UnknownSensitivityLevel_Throws()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["BackgroundAgents:Agents:0:Id"] = "agent-1",
                ["BackgroundAgents:Agents:0:Name"] = "Test",
                ["BackgroundAgents:Agents:0:Role"] = "monitor",
                ["BackgroundAgents:Agents:0:Commands:0"] = "ping",
                ["BackgroundAgents:Agents:0:Schedule:Type"] = "Interval",
                ["BackgroundAgents:Agents:0:Schedule:Interval"] = "00:01:00",
                ["BackgroundAgents:Agents:0:MaxDataSensitivity"] = "NonExistentLevel"
            })
            .Build();
        var loader = CreateLoader(config);
        var act = () => loader.LoadAsync(default);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*unknown sensitivity level*");
    }

    [Fact]
    public async Task LoadAsync_IntervalScheduleWithoutInterval_Throws()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["BackgroundAgents:Agents:0:Id"] = "agent-1",
                ["BackgroundAgents:Agents:0:Name"] = "Test",
                ["BackgroundAgents:Agents:0:Role"] = "monitor",
                ["BackgroundAgents:Agents:0:Commands:0"] = "ping",
                ["BackgroundAgents:Agents:0:Schedule:Type"] = "Interval",
                ["BackgroundAgents:Agents:0:MaxDataSensitivity"] = "Public"
            })
            .Build();
        var loader = CreateLoader(config);
        var act = () => loader.LoadAsync(default);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Interval*");
    }

    [Fact]
    public async Task LoadAsync_CronScheduleWithoutCronExpression_Throws()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["BackgroundAgents:Agents:0:Id"] = "agent-1",
                ["BackgroundAgents:Agents:0:Name"] = "Test",
                ["BackgroundAgents:Agents:0:Role"] = "monitor",
                ["BackgroundAgents:Agents:0:Commands:0"] = "ping",
                ["BackgroundAgents:Agents:0:Schedule:Type"] = "Cron",
                ["BackgroundAgents:Agents:0:MaxDataSensitivity"] = "Public"
            })
            .Build();
        var loader = CreateLoader(config);
        var act = () => loader.LoadAsync(default);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*CronExpression*");
    }

    [Fact]
    public async Task LoadAsync_InvalidCronExpression_Throws()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["BackgroundAgents:Agents:0:Id"] = "agent-1",
                ["BackgroundAgents:Agents:0:Name"] = "Test",
                ["BackgroundAgents:Agents:0:Role"] = "monitor",
                ["BackgroundAgents:Agents:0:Commands:0"] = "ping",
                ["BackgroundAgents:Agents:0:Schedule:Type"] = "Cron",
                ["BackgroundAgents:Agents:0:Schedule:CronExpression"] = "invalid",
                ["BackgroundAgents:Agents:0:MaxDataSensitivity"] = "Public"
            })
            .Build();
        var loader = CreateLoader(config);
        var act = () => loader.LoadAsync(default);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Invalid cron expression*invalid*");
    }

    [Fact]
    public async Task LoadAsync_RAGEnabledWithoutProvider_Throws()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["BackgroundAgents:Agents:0:Id"] = "agent-1",
                ["BackgroundAgents:Agents:0:Name"] = "Test",
                ["BackgroundAgents:Agents:0:Role"] = "monitor",
                ["BackgroundAgents:Agents:0:Commands:0"] = "ping",
                ["BackgroundAgents:Agents:0:Schedule:Type"] = "Interval",
                ["BackgroundAgents:Agents:0:Schedule:Interval"] = "00:01:00",
                ["BackgroundAgents:Agents:0:MaxDataSensitivity"] = "Public",
                ["BackgroundAgents:Agents:0:RAG:Enabled"] = "true"
            })
            .Build();
        var loader = CreateLoader(config);
        var act = () => loader.LoadAsync(default);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*RAG*provider*");
    }

    [Fact]
    public async Task LoadAsync_ValidConfig_ReturnsConfigs()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["BackgroundAgents:Agents:0:Id"] = "agent-1",
                ["BackgroundAgents:Agents:0:Name"] = "Test",
                ["BackgroundAgents:Agents:0:Role"] = "monitor",
                ["BackgroundAgents:Agents:0:Commands:0"] = "ping",
                ["BackgroundAgents:Agents:0:Schedule:Type"] = "Interval",
                ["BackgroundAgents:Agents:0:Schedule:Interval"] = "00:01:00",
                ["BackgroundAgents:Agents:0:MaxDataSensitivity"] = "Public"
            })
            .Build();
        var loader = CreateLoader(config);
        var configs = await loader.LoadAsync(default);
        configs.Should().HaveCount(1);
        configs[0].Id.Should().Be("agent-1");
        configs[0].MaxDataSensitivity.Should().Be("Public");
    }
}
