using Nexo.Abstractions;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Nexo.BackgroundAgents.Agents;
using Nexo.Orchestration.Agents;
using Nexo.Policies.Dev;
using Xunit;

namespace Nexo.Tests.BackgroundAgents.SelfExtend;

/// <summary>
/// SX-AUDIT invariant D — recursion / runaway ceiling for extender-triggered extension.
/// See <c>docs/SELF-EXTEND-AUDIT.md#invariant-d-recursion-runaway-ceiling</c>.
/// </summary>
public sealed class SelfExtendInvariantDRecursionCeilingTests
{
    [Fact]
    public void Characterization_per_cycle_react_iteration_cap_is_configured()
    {
        ToolCallingAgent.DefaultMaxIterations.Should().Be(5,
            "single-cycle ReAct bound exists inside ToolCallingAgent.RunCycleAsync");
        ToolCallingAgent.DefaultPerCycleDeadline.Should().Be(TimeSpan.FromMinutes(5));
    }

    [Fact]
    public void Characterization_per_cycle_build_test_budget_denies_excess_invocations()
    {
        var budget = new BuildTestBudget(buildBudget: 1, testBudget: 1);
        var snapshot = new WorldSnapshot(0, new Dictionary<string, object?>());
        var build = new ToolCall("dotnet.build", System.Text.Json.JsonSerializer.SerializeToElement(new { }));
        var test = new ToolCall("dotnet.test", System.Text.Json.JsonSerializer.SerializeToElement(new { }));

        budget.Approve(build, snapshot, out _).Should().BeTrue();
        budget.Approve(build, snapshot, out var buildReason).Should().BeFalse(buildReason);
        budget.Reset();
        budget.Approve(test, snapshot, out _).Should().BeTrue();
        budget.Approve(test, snapshot, out var testReason).Should().BeFalse(testReason);
    }

    [Fact]
    public async Task Characterization_no_cross_cycle_extender_depth_ceiling()
    {
        var runner = new SelfExtendAuditTestSupport.CountingSelfExtendRunner();
        var registry = SelfExtendAuditTestSupport.CreateRegistry(
            selfExtendRunner: runner,
            modeStore: SelfExtendAuditTestSupport.ActiveModeStore());
        var config = SelfExtendAuditTestSupport.ExtenderConfig("deep-extender", Environment.CurrentDirectory);
        await registry.RegisterAsync(
            new GenericAgent(SelfExtendAuditTestSupport.BuildSpec(config), NullLogger<GenericAgent>.Instance),
            config);

        const int cycles = 12;
        for (var i = 0; i < cycles; i++)
            await registry.ExecuteOnceAsync(config.Id);

        runner.CallCount.Should().Be(cycles,
            "registry does not track extender recursion depth or cumulative extension rate across cycles");
    }

    /// <summary>
    /// Rejection test for invariant D. Skipped because only per-cycle caps exist; no extender depth/rate ceiling refuses further cycles.
    /// </summary>
    [Fact(Skip = "GAP: No extender recursion depth or cross-cycle rate ceiling refuses extension — see docs/SELF-EXTEND-AUDIT.md#invariant-d-recursion-runaway-ceiling")]
    public async Task Rejection_extension_past_configured_recursion_ceiling_is_refused()
    {
        var runner = new SelfExtendAuditTestSupport.CountingSelfExtendRunner();
        var registry = SelfExtendAuditTestSupport.CreateRegistry(
            selfExtendRunner: runner,
            modeStore: SelfExtendAuditTestSupport.ActiveModeStore());
        var config = SelfExtendAuditTestSupport.ExtenderConfig("ceiling-extender", Environment.CurrentDirectory);
        await registry.RegisterAsync(
            new GenericAgent(SelfExtendAuditTestSupport.BuildSpec(config), NullLogger<GenericAgent>.Instance),
            config);

        for (var i = 0; i < 20; i++)
            await registry.ExecuteOnceAsync(config.Id);

        runner.CallCount.Should().BeLessThan(20,
            "invariant D requires extension past configured depth/rate ceiling to be REFUSED");
    }
}
