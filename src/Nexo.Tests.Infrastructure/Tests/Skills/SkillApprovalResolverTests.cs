using FluentAssertions;
using Nexo.Core.Application.Mesh.Models;
using Nexo.Core.Application.Skills.Models;
using Nexo.Core.Application.Skills.Ports;
using Nexo.Infrastructure.Skills;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.Skills;

public sealed class SkillApprovalResolverTests
{
    [Fact]
    public async Task ResolveScriptApprovalAsync_auto_approves_when_policy_matches()
    {
        var evaluator = new StubPolicyEvaluator(autoApprove: true);
        var store = new InMemorySkillApprovalStore();
        var resolver = new SkillApprovalResolver(evaluator, store, new HumanInTheLoopSkillApprovalGate());

        var status = await resolver.ResolveScriptApprovalAsync(
            new SkillScriptApprovalKey("skill", "scripts/a.sh", PeerTrustTier.Trusted, "internal", "pack"),
            "test");

        status.Should().Be(NexoSkillApprovalStatus.AutoApproved);
        store.GetPending().Should().BeEmpty();
    }

    [Fact]
    public async Task ResolveScriptApprovalAsync_waits_for_operator_approve()
    {
        var evaluator = new StubPolicyEvaluator(autoApprove: false);
        var store = new InMemorySkillApprovalStore();
        var resolver = new SkillApprovalResolver(
            evaluator,
            store,
            new HumanInTheLoopSkillApprovalGate(),
            approvalTimeout: TimeSpan.FromSeconds(5));

        var resolveTask = resolver.ResolveScriptApprovalAsync(
            new SkillScriptApprovalKey("skill", "scripts/a.sh", PeerTrustTier.Untrusted, "internal", "pack"),
            "test");

        await WaitForPendingAsync(store);
        var pending = store.GetPending().Single();
        store.TryResolve(pending.RequestId, NexoSkillApprovalStatus.Approved, out _).Should().BeTrue();

        var status = await resolveTask;
        status.Should().Be(NexoSkillApprovalStatus.Approved);
        store.GetPending().Should().BeEmpty();
    }

    [Fact]
    public async Task ResolveScriptApprovalAsync_returns_denied_when_gate_denies()
    {
        var evaluator = new StubPolicyEvaluator(autoApprove: false);
        var store = new InMemorySkillApprovalStore();
        var resolver = new SkillApprovalResolver(evaluator, store, new DenySkillApprovalGate());

        var status = await resolver.ResolveScriptApprovalAsync(
            new SkillScriptApprovalKey("skill", "scripts/a.sh", PeerTrustTier.Untrusted, "internal", "pack"),
            "test");

        status.Should().Be(NexoSkillApprovalStatus.Denied);
        store.GetPending().Should().BeEmpty();
    }

    private static async Task WaitForPendingAsync(INexoSkillApprovalStore store)
    {
        for (var i = 0; i < 50; i++)
        {
            if (store.GetPending().Count > 0)
                return;
            await Task.Delay(20);
        }

        throw new TimeoutException("Pending approval was not registered.");
    }

    private sealed class StubPolicyEvaluator(bool autoApprove) : INexoSkillPolicyEvaluator
    {
        public bool IsSkillVisible(NexoSkillDescriptor skill, NexoSkillExecutionContext context) => true;

        public bool IsScriptAllowed(string skillName, string scriptPath, NexoSkillExecutionContext context) => true;

        public bool IsScriptAutoApproved(string skillName, string scriptPath, NexoSkillExecutionContext context) => autoApprove;

        public NexoScriptRunOptions GetScriptLimits(NexoSkillExecutionContext context)
            => new(TimeSpan.FromSeconds(5), 4096, Path.GetTempPath());

        public SkillPolicyRules? GetActiveSkillRules() => null;
    }
}
