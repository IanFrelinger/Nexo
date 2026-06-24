using System.Text.Json;
using FluentAssertions;
using Nexo.Abstractions;
using Nexo.BackgroundAgents.HostRunners;
using Nexo.Runtime;
using Xunit;

namespace Nexo.Tests.BackgroundAgents.SelfExtend;

/// <summary>
/// SX-AUDIT invariant A — cert-gate inheritance on the self-extend path.
/// See <c>docs/SELF-EXTEND-AUDIT.md#invariant-a-cert-gate-inheritance</c>.
/// </summary>
public sealed class SelfExtendInvariantACertGateTests
{
    [Fact]
    public void Characterization_selfExtend_policy_chain_excludes_certification_gate()
    {
        var (_, policies, _) = RepoFsToolboxFactory.CreateWithBuildTest();

        var policyTypes = SelfExtendAuditTestSupport.GetPolicies(policies)
            .Select(p => p.GetType().FullName ?? p.GetType().Name)
            .ToList();

        policyTypes.Should().NotContain(t => t.Contains("Certification", StringComparison.Ordinal),
            "self-extend must not silently include a cert gate — presence would need an explicit executing call site");
        policyTypes.Should().NotContain(t => t.Contains("DataExfiltrationPolicy", StringComparison.Ordinal),
            "exfiltration policy is not wired on the self-extend toolbox path");
    }

    [Fact]
    public void Characterization_uncertified_brick_write_is_approved_by_self_extend_policies()
    {
        var (_, policies, _) = RepoFsToolboxFactory.CreateWithBuildTest();
        var call = new ToolCall(
            "repo.fs.write",
            JsonSerializer.SerializeToElement(new
            {
                path = "src/Nexo.Bricks.AuditGap/UncertifiedSelfProposedBrick.cs",
                content = "namespace Nexo.Bricks.AuditGap; public sealed class UncertifiedSelfProposedBrick { }"
            }));

        var snapshot = new WorldSnapshot(0, new Dictionary<string, object?>
        {
            ["RepoRoot"] = "/workspace",
            ["AgentName"] = "self-extend"
        });

        policies.Approve(call, snapshot, out var reason).Should().BeTrue(
            "only PathAllowlist/MaxWriteSize/BuildTestBudget apply — no CertificationGate executes");
        reason.Should().Be("OK");
    }

    /// <summary>
    /// Rejection test for invariant A. Skipped because the live path does not refuse uncertified bricks.
    /// </summary>
    [Fact(Skip = "GAP: Self-extend registration/write path never calls CertificationGate — see docs/SELF-EXTEND-AUDIT.md#invariant-a-cert-gate-inheritance")]
    public void Rejection_uncertified_self_proposed_brick_is_refused_at_registration_or_use()
    {
        var (_, policies, _) = RepoFsToolboxFactory.CreateWithBuildTest();
        var call = new ToolCall(
            "repo.fs.write",
            JsonSerializer.SerializeToElement(new
            {
                path = "src/Nexo.Bricks.AuditGap/UncertifiedSelfProposedBrick.cs",
                content = "namespace Nexo.Bricks.AuditGap; public sealed class UncertifiedSelfProposedBrick { }"
            }));
        var snapshot = new WorldSnapshot(0, new Dictionary<string, object?> { ["RepoRoot"] = "/workspace" });

        policies.Approve(call, snapshot, out _).Should().BeFalse(
            "invariant A requires absent/failing certification to be REFUSED, not runnable");
    }
}
