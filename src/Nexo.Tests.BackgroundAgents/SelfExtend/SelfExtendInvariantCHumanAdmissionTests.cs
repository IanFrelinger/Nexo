using Nexo.Abstractions;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Nexo.BackgroundAgents.Agents;
using Nexo.BackgroundAgents.Configuration;
using Nexo.BackgroundAgents.Registry;
using Nexo.BackgroundAgents.Tools;
using Nexo.Orchestration.Agents;
using Xunit;

namespace Nexo.Tests.BackgroundAgents.SelfExtend;

/// <summary>
/// SX-ENFORCE invariant C — fail-closed default aggressiveness for extender cycles.
/// </summary>
public sealed class SelfExtendInvariantCHumanAdmissionTests
{
    [Fact]
    public async Task Rejection_unconfigured_extender_does_not_run_self_extend_cycle()
    {
        var runner = new SelfExtendAuditTestSupport.CountingSelfExtendRunner();
        var registry = SelfExtendAuditTestSupport.CreateRegistry(
            selfExtendRunner: runner,
            modeStore: null);
        var config = SelfExtendAuditTestSupport.ExtenderConfig("passive-default-extender", Environment.CurrentDirectory);

        await registry.RegisterAsync(
            new GenericAgent(SelfExtendAuditTestSupport.BuildSpec(config), NullLogger<GenericAgent>.Instance),
            config);
        await registry.ExecuteOnceAsync(config.Id);

        runner.CallCount.Should().Be(0,
            "missing mode store defaults to Passive — extender must not self-extend");
    }

    [Fact]
    public async Task Admission_explicitly_active_mode_runs_extender_cycle()
    {
        var runner = new SelfExtendAuditTestSupport.CountingSelfExtendRunner();
        var registry = SelfExtendAuditTestSupport.CreateRegistry(
            selfExtendRunner: runner,
            modeStore: SelfExtendAuditTestSupport.ActiveModeStore());
        var config = SelfExtendAuditTestSupport.ExtenderConfig("active-extender", Environment.CurrentDirectory);

        await registry.RegisterAsync(
            new GenericAgent(SelfExtendAuditTestSupport.BuildSpec(config), NullLogger<GenericAgent>.Instance),
            config);
        await registry.ExecuteOnceAsync(config.Id);

        runner.CallCount.Should().Be(1);
    }

    [Fact]
    public async Task Admission_semi_active_with_approval_runs_extender_cycle()
    {
        var runner = new SelfExtendAuditTestSupport.CountingSelfExtendRunner();
        var modeStore = new InMemoryAggressivenessModeStore();
        modeStore.SetMode(BackgroundAgentAggressivenessMode.SemiActive);
        var registry = SelfExtendAuditTestSupport.CreateRegistry(
            selfExtendRunner: runner,
            modeStore: modeStore,
            approvalGate: new AlwaysApprovalGate());
        var config = SelfExtendAuditTestSupport.ExtenderConfig("semi-active-extender", Environment.CurrentDirectory);

        await registry.RegisterAsync(
            new GenericAgent(SelfExtendAuditTestSupport.BuildSpec(config), NullLogger<GenericAgent>.Instance),
            config);
        await registry.ExecuteOnceAsync(config.Id);

        runner.CallCount.Should().Be(1);
    }

    [Fact]
    public async Task Rejection_semi_active_without_approval_does_not_run_extender_cycle()
    {
        var runner = new SelfExtendAuditTestSupport.CountingSelfExtendRunner();
        var modeStore = new InMemoryAggressivenessModeStore();
        modeStore.SetMode(BackgroundAgentAggressivenessMode.SemiActive);
        var registry = SelfExtendAuditTestSupport.CreateRegistry(
            selfExtendRunner: runner,
            modeStore: modeStore,
            approvalGate: null);
        var config = SelfExtendAuditTestSupport.ExtenderConfig("semi-active-denied", Environment.CurrentDirectory);

        await registry.RegisterAsync(
            new GenericAgent(SelfExtendAuditTestSupport.BuildSpec(config), NullLogger<GenericAgent>.Instance),
            config);
        await registry.ExecuteOnceAsync(config.Id);

        runner.CallCount.Should().Be(0);
    }

    [Fact]
    public async Task Characterization_enable_agent_starts_registered_monitor_without_extender_gate()
    {
        var registry = SelfExtendAuditTestSupport.CreateRegistry();
        var config = new BackgroundAgentConfig
        {
            Id = "map-validator",
            Role = "monitor",
            Enabled = true,
            MaxDataSensitivity = "Public",
            Commands = ["ping"],
            Schedule = new BackgroundAgentSchedule { Type = ScheduleType.Interval, Interval = TimeSpan.FromMinutes(1) },
        };
        await registry.RegisterAsync(
            new GenericAgent(SelfExtendAuditTestSupport.BuildSpec(config), NullLogger<GenericAgent>.Instance),
            config);

        var tool = new EnableAgentTool(registry);
        await tool.InvokeAsync(
            new ToolCall("enable_agent", System.Text.Json.JsonSerializer.SerializeToElement(new { agentId = config.Id })),
            new WorldSnapshot(0, new Dictionary<string, object?>()),
            CancellationToken.None);

        registry.GetAgent(config.Id)!.State.Should().Be(BackgroundAgentState.Running);
    }
}
