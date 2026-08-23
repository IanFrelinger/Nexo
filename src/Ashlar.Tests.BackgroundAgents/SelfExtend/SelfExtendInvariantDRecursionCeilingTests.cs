using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Ashlar.Abstractions;
using Ashlar.BackgroundAgents.Agents;
using Ashlar.BackgroundAgents.Configuration;
using Ashlar.BackgroundAgents.Extending;
using Ashlar.BackgroundAgents.Logging;
using Ashlar.Orchestration.Agents;
using Ashlar.Policies.Dev;
using Xunit;
using Ashlar.Tests.BackgroundAgents.Registry;

namespace Ashlar.Tests.BackgroundAgents.SelfExtend;

/// <summary>
/// SX-AUDIT invariant D — recursion / runaway ceiling for extender-triggered extension.
/// See <c>docs/SELF-EXTEND-AUDIT.md#invariant-d-recursion-runaway-ceiling</c>.
///
/// <para>Per-cycle caps (ReAct iterations, build/test budget) were always there; what was
/// missing was anything ACROSS cycles. <see cref="ExtensionCeiling"/> adds three, enforced by
/// the registry before the runner is invoked: lineage depth (recursion), unattended cycles
/// since a human armed the agent (runaway), and cycles per trailing hour (rate). Every
/// override may only lower a ceiling, and re-arm is an operator surface — an agent cannot
/// re-arm itself, including by re-registering.</para>
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
        var build = new ToolCall("dotnet.build", JsonSerializer.SerializeToElement(new { }));
        var test = new ToolCall("dotnet.test", JsonSerializer.SerializeToElement(new { }));

        budget.Approve(build, snapshot, out _).Should().BeTrue();
        budget.Approve(build, snapshot, out var buildReason).Should().BeFalse(buildReason);
        budget.Reset();
        budget.Approve(test, snapshot, out _).Should().BeTrue();
        budget.Approve(test, snapshot, out var testReason).Should().BeFalse(testReason);
    }

    /// <summary>
    /// The default ceiling bounds an Active extender across cycles, and every refusal is
    /// visible: a Warning in the agent log and a Warn observation with the reason. Back-to-back
    /// cycles hit the hourly rate first (4/h), before the unattended count (8).
    /// </summary>
    [Fact]
    public async Task Enforcement_default_ceiling_bounds_cross_cycle_extension_and_refusals_are_observable()
    {
        var runner = new SelfExtendAuditTestSupport.CountingSelfExtendRunner();
        var log = new InMemoryAgentLogStore();
        var observations = new SelfExtendAuditTestSupport.ListObservationStore();
        var registry = SelfExtendAuditTestSupport.CreateRegistry(
            selfExtendRunner: runner,
            modeStore: SelfExtendAuditTestSupport.ActiveModeStore(),
            logStore: log,
            observations: observations);
        var config = SelfExtendAuditTestSupport.ExtenderConfig("bounded-extender", Environment.CurrentDirectory);
        await registry.RegisterAuthoredAsync(
            new GenericAgent(SelfExtendAuditTestSupport.BuildSpec(config), NullLogger<GenericAgent>.Instance),
            config);

        const int cycles = 12;
        for (var i = 0; i < cycles; i++)
            await registry.ExecuteOnceAsync(config.Id);

        runner.CallCount.Should().Be(ExtensionCeiling.Default.MaxCyclesPerHour,
            "back-to-back cycles reach the hourly rate ceiling first; everything past it is refused");

        var refusals = log.GetRecent(config.Id, maxCount: 200)
            .Where(e => e.Message.Contains("Extension refused (invariant D)", StringComparison.Ordinal))
            .ToList();
        refusals.Should().HaveCount(cycles - ExtensionCeiling.Default.MaxCyclesPerHour);
        refusals.Should().OnlyContain(e => e.Level == "Warning");
        refusals.Should().OnlyContain(e => e.Message.Contains("MaxCyclesPerHour", StringComparison.Ordinal));

        observations.All.Where(o => o.facts is not null && o.facts.TryGetValue("stopped_reason", out var r) && r == "extension_ceiling")
            .Should().HaveCount(cycles - ExtensionCeiling.Default.MaxCyclesPerHour,
                "a held extender must be visible to the planner and the digest, not silent");
        registry.GetExtensionLedger(config.Id).UnattendedCycles.Should().Be(ExtensionCeiling.Default.MaxCyclesPerHour,
            "only cycles that reached the runner consume budget");
    }

    /// <summary>Rejection test for invariant D — the one that was skipped as a GAP.</summary>
    [Fact]
    public async Task Rejection_extension_past_configured_recursion_ceiling_is_refused()
    {
        var runner = new SelfExtendAuditTestSupport.CountingSelfExtendRunner();
        var registry = SelfExtendAuditTestSupport.CreateRegistry(
            selfExtendRunner: runner,
            modeStore: SelfExtendAuditTestSupport.ActiveModeStore());
        var config = SelfExtendAuditTestSupport.ExtenderConfig("ceiling-extender", Environment.CurrentDirectory);
        await registry.RegisterAuthoredAsync(
            new GenericAgent(SelfExtendAuditTestSupport.BuildSpec(config), NullLogger<GenericAgent>.Instance),
            config);

        for (var i = 0; i < 20; i++)
            await registry.ExecuteOnceAsync(config.Id);

        runner.CallCount.Should().BeLessThan(20,
            "invariant D requires extension past configured depth/rate ceiling to be REFUSED");
    }

    [Fact]
    public async Task Unattended_ceiling_binds_when_rate_is_not_the_limit()
    {
        var runner = new SelfExtendAuditTestSupport.CountingSelfExtendRunner();
        var registry = SelfExtendAuditTestSupport.CreateRegistry(
            selfExtendRunner: runner,
            modeStore: SelfExtendAuditTestSupport.ActiveModeStore(),
            extensionCeiling: new ExtensionCeiling(MaxLineageDepth: 1, MaxUnattendedCycles: 3, MaxCyclesPerHour: 100));
        var config = SelfExtendAuditTestSupport.ExtenderConfig("unattended-extender", Environment.CurrentDirectory);
        await registry.RegisterAuthoredAsync(
            new GenericAgent(SelfExtendAuditTestSupport.BuildSpec(config), NullLogger<GenericAgent>.Instance),
            config);

        for (var i = 0; i < 10; i++)
            await registry.ExecuteOnceAsync(config.Id);

        runner.CallCount.Should().Be(3, "three unattended cycles, then hold for a human");
    }

    /// <summary>
    /// The heart of the invariant: an agent cannot re-arm itself. Re-registration is something
    /// an agent can do to itself (UpdateAgentConfigTool registers as Authored), so it must not
    /// reset the ledger; only the operator surface does.
    /// </summary>
    [Fact]
    public async Task Rearm_is_an_operator_act_and_reregistration_does_not_reset_the_ledger()
    {
        var runner = new SelfExtendAuditTestSupport.CountingSelfExtendRunner();
        var registry = SelfExtendAuditTestSupport.CreateRegistry(
            selfExtendRunner: runner,
            modeStore: SelfExtendAuditTestSupport.ActiveModeStore(),
            extensionCeiling: new ExtensionCeiling(MaxLineageDepth: 1, MaxUnattendedCycles: 2, MaxCyclesPerHour: 100));
        var config = SelfExtendAuditTestSupport.ExtenderConfig("rearm-extender", Environment.CurrentDirectory);
        var agent = new GenericAgent(SelfExtendAuditTestSupport.BuildSpec(config), NullLogger<GenericAgent>.Instance);
        await registry.RegisterAuthoredAsync(agent, config);

        for (var i = 0; i < 4; i++)
            await registry.ExecuteOnceAsync(config.Id);
        runner.CallCount.Should().Be(2);

        // The agent re-registers itself (what UpdateAgentConfigTool does) — no relief.
        await registry.RegisterAsync(agent, config, AgentRegistrationOrigin.Authored);
        await registry.ExecuteOnceAsync(config.Id);
        runner.CallCount.Should().Be(2, "re-registration must not re-arm; agents can re-register themselves");

        // A human re-arms — the next cycle runs.
        registry.RearmExtension(config.Id).Should().Be(2, "the cleared unattended count is reported");
        await registry.ExecuteOnceAsync(config.Id);
        runner.CallCount.Should().Be(3);
    }

    /// <summary>
    /// Recursion proper: extension that spawns extenders that extend. Roots (depth 0) and their
    /// children (depth 1) may run; a grandchild (depth 2) may not under the default ceiling.
    /// </summary>
    [Fact]
    public async Task Lineage_depth_ceiling_refuses_machine_spawned_grandchildren()
    {
        var runner = new SelfExtendAuditTestSupport.CountingSelfExtendRunner();
        var registry = SelfExtendAuditTestSupport.CreateRegistry(
            selfExtendRunner: runner,
            modeStore: SelfExtendAuditTestSupport.ActiveModeStore(),
            extensionCeiling: new ExtensionCeiling(MaxLineageDepth: 1, MaxUnattendedCycles: 100, MaxCyclesPerHour: 100));

        var root = SelfExtendAuditTestSupport.ExtenderConfig("root", Environment.CurrentDirectory);
        var child = SelfExtendAuditTestSupport.ExtenderConfig("child", Environment.CurrentDirectory);
        child.ParentId = root.Id;
        var grandchild = SelfExtendAuditTestSupport.ExtenderConfig("grandchild", Environment.CurrentDirectory);
        grandchild.ParentId = child.Id;

        foreach (var config in new[] { root, child, grandchild })
        {
            await registry.RegisterAuthoredAsync(
                new GenericAgent(SelfExtendAuditTestSupport.BuildSpec(config), NullLogger<GenericAgent>.Instance),
                config);
        }

        registry.LineageDepth(root).Should().Be(0);
        registry.LineageDepth(child).Should().Be(1);
        registry.LineageDepth(grandchild).Should().Be(2);

        await registry.ExecuteOnceAsync(root.Id);
        await registry.ExecuteOnceAsync(child.Id);
        runner.CallCount.Should().Be(2, "a root and its direct child may extend");

        await registry.ExecuteOnceAsync(grandchild.Id);
        runner.CallCount.Should().Be(2, "a grandchild is refused: MaxLineageDepth 1");
    }

    [Fact]
    public void Environment_may_only_lower_the_ceiling_never_raise_it()
    {
        var env = new Dictionary<string, string?>
        {
            [ExtensionCeiling.LineageDepthEnvVar] = "0",       // stricter: only roots
            [ExtensionCeiling.UnattendedCyclesEnvVar] = "999", // looser: ignored
            [ExtensionCeiling.CyclesPerHourEnvVar] = "garbage",
        };

        var resolved = ExtensionCeiling.Resolve(k => env.TryGetValue(k, out var v) ? v : null);

        resolved.MaxLineageDepth.Should().Be(0);
        resolved.MaxUnattendedCycles.Should().Be(ExtensionCeiling.Default.MaxUnattendedCycles,
            "a deployment may be stricter than the defaults, never looser");
        resolved.MaxCyclesPerHour.Should().Be(ExtensionCeiling.Default.MaxCyclesPerHour);
        ExtensionCeiling.Resolve(_ => null).Should().Be(ExtensionCeiling.Default);
    }

    [Fact]
    public void Agent_parameters_may_only_narrow_the_ceiling_whatever_their_json_shape()
    {
        var config = SelfExtendAuditTestSupport.ExtenderConfig("narrowing", Environment.CurrentDirectory);
        config.Parameters![ExtensionCeiling.UnattendedCyclesParameter] = 2;                                  // int, lower
        config.Parameters[ExtensionCeiling.CyclesPerHourParameter] = JsonSerializer.SerializeToElement(50);   // JSON number, higher
        config.Parameters[ExtensionCeiling.LineageDepthParameter] = "0";                                     // string, lower

        var narrowed = ExtensionCeiling.Default.NarrowedBy(config);

        narrowed.MaxUnattendedCycles.Should().Be(2);
        narrowed.MaxCyclesPerHour.Should().Be(ExtensionCeiling.Default.MaxCyclesPerHour,
            "an agent asking for MORE authority than the deployment grants is ignored (I-1)");
        narrowed.MaxLineageDepth.Should().Be(0);
    }

    [Fact]
    public void Ledger_rate_window_slides_and_rearm_does_not_forgive_the_rate()
    {
        var now = new DateTimeOffset(2026, 8, 16, 12, 0, 0, TimeSpan.Zero);
        var ledger = new ExtensionLedger(() => now);
        var ceiling = new ExtensionCeiling(MaxLineageDepth: 1, MaxUnattendedCycles: 100, MaxCyclesPerHour: 2);

        ledger.Refusal(ceiling, lineageDepth: 0).Should().BeNull();
        ledger.RecordCycle();
        ledger.RecordCycle();
        ledger.Refusal(ceiling, lineageDepth: 0).Should().Contain("MaxCyclesPerHour");
        ledger.CyclesInWindow.Should().Be(2);

        ledger.Rearm();
        ledger.Refusal(ceiling, lineageDepth: 0).Should().Contain("MaxCyclesPerHour",
            "a human re-arm clears the unattended count, not the last hour's cycles");

        now = now.AddMinutes(61);
        ledger.CyclesInWindow.Should().Be(0);
        ledger.Refusal(ceiling, lineageDepth: 0).Should().BeNull("the trailing-hour window has slid past both cycles");
        ledger.Refusal(ceiling, lineageDepth: 2).Should().Contain("MaxLineageDepth", "depth is checked regardless of rate");
    }
}
