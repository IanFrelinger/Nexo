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
        var resolver = new SkillApprovalResolver(evaluator, store, new StubApprovalGate(NexoSkillApprovalStatus.Denied));

        var status = await resolver.ResolveScriptApprovalAsync(
            new SkillScriptApprovalKey("skill", "scripts/a.sh", PeerTrustTier.Trusted, "internal", "pack"),
            "test");

        status.Should().Be(NexoSkillApprovalStatus.AutoApproved);
        store.GetPending().Should().BeEmpty();
    }

    [Fact]
    public async Task ResolveScriptApprovalAsync_registers_pending_and_uses_gate()
    {
        var evaluator = new StubPolicyEvaluator(autoApprove: false);
        var store = new InMemorySkillApprovalStore();
        var resolver = new SkillApprovalResolver(evaluator, store, new StubApprovalGate(NexoSkillApprovalStatus.Approved));

        var status = await resolver.ResolveScriptApprovalAsync(
            new SkillScriptApprovalKey("skill", "scripts/a.sh", PeerTrustTier.Untrusted, "internal", "pack"),
            "test");

        status.Should().Be(NexoSkillApprovalStatus.Approved);
        store.GetPending().Should().ContainSingle(p => p.Status == NexoSkillApprovalStatus.Pending);
    }

    [Fact]
    public async Task ResolveScriptApprovalAsync_returns_denied_when_gate_denies()
    {
        var evaluator = new StubPolicyEvaluator(autoApprove: false);
        var store = new InMemorySkillApprovalStore();
        var resolver = new SkillApprovalResolver(evaluator, store, new StubApprovalGate(NexoSkillApprovalStatus.Denied));

        var status = await resolver.ResolveScriptApprovalAsync(
            new SkillScriptApprovalKey("skill", "scripts/a.sh", PeerTrustTier.Untrusted, "internal", "pack"),
            "test");

        status.Should().Be(NexoSkillApprovalStatus.Denied);
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

    private sealed class StubApprovalGate(NexoSkillApprovalStatus status) : INexoSkillApprovalGate
    {
        public Task<NexoSkillApprovalStatus> RequestApprovalAsync(
            string actionDescription,
            TimeSpan timeout,
            CancellationToken cancellationToken = default)
            => Task.FromResult(status);
    }
}
