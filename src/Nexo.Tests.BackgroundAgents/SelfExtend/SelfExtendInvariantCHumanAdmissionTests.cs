using Nexo.Abstractions;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Nexo.BackgroundAgents.Agents;
using Nexo.BackgroundAgents.Configuration;
using Nexo.BackgroundAgents.Registry;
using Nexo.BackgroundAgents.Tools;
using Nexo.Orchestration.Agents;
using Nexo.Runtime;
using Xunit;

namespace Nexo.Tests.BackgroundAgents.SelfExtend;

/// <summary>
/// SX-AUDIT invariant C — human admission seam (ApprovalBridge) before live mesh activation.
/// See <c>docs/SELF-EXTEND-AUDIT.md#invariant-c-human-admission-seam</c>.
/// </summary>
public sealed class SelfExtendInvariantCHumanAdmissionTests
{
    [Fact]
    public async Task Characterization_enable_agent_starts_registered_agent_without_approval_bridge()
    {
        var registry = SelfExtendAuditTestSupport.CreateRegistry();
        var config = SelfExtendAuditTestSupport.ExtenderConfig("pending-extender", Environment.CurrentDirectory);
        await registry.RegisterAsync(
            new GenericAgent(SelfExtendAuditTestSupport.BuildSpec(config), NullLogger<GenericAgent>.Instance),
            config);

        var tool = new EnableAgentTool(registry);
        var result = await tool.InvokeAsync(
            new ToolCall("enable_agent", System.Text.Json.JsonSerializer.SerializeToElement(new { agentId = config.Id })),
            new WorldSnapshot(0, new Dictionary<string, object?>()),
            CancellationToken.None);

        result.Payload.Should().BeAssignableTo<object>();
        registry.GetAgent(config.Id)!.State.Should().Be(BackgroundAgentState.Running,
            "EnableAgentTool calls StartAsync directly — no ApprovalBridge token is consulted");
    }

    [Fact]
    public async Task Characterization_extender_default_active_mode_runs_without_human_approval_gate()
    {
        var runner = new SelfExtendAuditTestSupport.CountingSelfExtendRunner();
        var registry = SelfExtendAuditTestSupport.CreateRegistry(selfExtendRunner: runner);
        var config = SelfExtendAuditTestSupport.ExtenderConfig("auto-active-extender", Environment.CurrentDirectory);

        await registry.RegisterAsync(
            new GenericAgent(SelfExtendAuditTestSupport.BuildSpec(config), NullLogger<GenericAgent>.Instance),
            config);
        await registry.ExecuteOnceAsync(config.Id);

        runner.CallCount.Should().Be(1,
            "missing mode store defaults to Active — extender executes without SemiActive IApprovalGate or ApprovalBridge");
    }

    [Fact]
    public void Characterization_semi_active_gate_is_IApprovalGate_not_mesh_admission_bridge()
    {
        typeof(IApprovalGate).Namespace.Should().Be("Nexo.BackgroundAgents.Configuration",
            "SemiActive extender cycles consult IApprovalGate only — not commercial Discord ApprovalBridge");
        typeof(NoApprovalGate).Name.Should().Be("NoApprovalGate",
            "default DI registration denies SemiActive cycles when no real gate is wired");
    }

    /// <summary>
    /// Rejection test for invariant C. Skipped because activation does not require ApprovalBridge human admission.
    /// </summary>
    [Fact(Skip = "GAP: Newly registered agents activate via StartAsync/enable_agent without ApprovalBridge — see docs/SELF-EXTEND-AUDIT.md#invariant-c-human-admission-seam")]
    public async Task Rejection_self_created_agent_auto_enable_without_approval_token_remains_disabled()
    {
        var registry = SelfExtendAuditTestSupport.CreateRegistry();
        var config = SelfExtendAuditTestSupport.ExtenderConfig("must-stay-disabled", Environment.CurrentDirectory);
        await registry.RegisterAsync(
            new GenericAgent(SelfExtendAuditTestSupport.BuildSpec(config), NullLogger<GenericAgent>.Instance),
            config);

        var tool = new EnableAgentTool(registry);
        await tool.InvokeAsync(
            new ToolCall("enable_agent", System.Text.Json.JsonSerializer.SerializeToElement(new { agentId = config.Id })),
            new WorldSnapshot(0, new Dictionary<string, object?>()),
            CancellationToken.None);

        registry.GetAgent(config.Id)!.State.Should().Be(BackgroundAgentState.Idle,
            "invariant C requires explicit human ApprovalBridge admission before live execution");
    }
}
