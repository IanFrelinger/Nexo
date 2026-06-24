using System.Text.Json;
using FluentAssertions;
using Nexo.Abstractions;
using Nexo.BackgroundAgents.HostRunners;
using Nexo.BackgroundAgents.Security;
using Nexo.Runtime;
using Xunit;

namespace Nexo.Tests.BackgroundAgents.SelfExtend;

/// <summary>
/// SX-ENFORCE invariant A — cert-gate inheritance on the self-extend path.
/// </summary>
public sealed class SelfExtendInvariantACertGateTests
{
    private const string BrickClassName = "UncertifiedSelfProposedBrick";
    private const string BrickRelativePath = "src/Nexo.Bricks.AuditGap/UncertifiedSelfProposedBrick.cs";
    private const string BrickSource =
        "namespace Nexo.Bricks.AuditGap; public sealed class UncertifiedSelfProposedBrick { }";

    [Fact]
    public void Characterization_selfExtend_policy_chain_includes_certification_gate_when_store_wired()
    {
        var (_, policies, _) = RepoFsToolboxFactory.CreateWithBuildTest(
            certificationStore: new Nexo.Infrastructure.Certification.InMemoryCertificationRecordStore());

        var policyTypes = SelfExtendAuditTestSupport.GetPolicies(policies)
            .Select(p => p.GetType().Name)
            .ToList();

        policyTypes.Should().Contain(nameof(SelfProducedBrickCertificationPolicy));
    }

    [Fact]
    public void Rejection_uncertified_self_proposed_brick_is_refused_at_registration_or_use()
    {
        var (_, policies, _) = RepoFsToolboxFactory.CreateWithBuildTest(
            certificationStore: new Nexo.Infrastructure.Certification.InMemoryCertificationRecordStore());

        var call = new ToolCall(
            "repo.fs.write",
            JsonSerializer.SerializeToElement(new { path = BrickRelativePath, content = BrickSource }));

        var snapshot = new WorldSnapshot(0, new Dictionary<string, object?>
        {
            ["RepoRoot"] = "/workspace",
            ["selfExtendAdmission"] = true
        });

        policies.Approve(call, snapshot, out var reason).Should().BeFalse();
        reason.Should().Contain("no certification record");
    }

    [Fact]
    public void Admission_certified_self_proposed_brick_with_matching_content_hash_is_allowed()
    {
        var (store, brickId, source) = SelfExtendAuditTestSupport.SeedCertifiedBrickAdmission(BrickClassName);
        var (_, policies, _) = RepoFsToolboxFactory.CreateWithBuildTest(certificationStore: store);

        var call = new ToolCall(
            "repo.fs.write",
            JsonSerializer.SerializeToElement(new { path = BrickRelativePath, content = source }));

        var snapshot = new WorldSnapshot(0, new Dictionary<string, object?>
        {
            ["RepoRoot"] = "/workspace",
            ["selfExtendAdmission"] = true
        });

        policies.Approve(call, snapshot, out var reason).Should().BeTrue(
            $"certified brick '{brickId}' with content-bound record should be admitted");
        reason.Should().Be("OK");
    }

    [Fact]
    public void Rejection_tampered_self_proposed_brick_content_is_refused()
    {
        var (store, _, source) = SelfExtendAuditTestSupport.SeedCertifiedBrickAdmission(BrickClassName);
        var (_, policies, _) = RepoFsToolboxFactory.CreateWithBuildTest(certificationStore: store);
        var tampered = source.Replace("{ }", "{ public int X => 1; }");

        var call = new ToolCall(
            "repo.fs.write",
            JsonSerializer.SerializeToElement(new { path = BrickRelativePath, content = tampered }));

        var snapshot = new WorldSnapshot(0, new Dictionary<string, object?>
        {
            ["RepoRoot"] = "/workspace",
            ["selfExtendAdmission"] = true
        });

        policies.Approve(call, snapshot, out var reason).Should().BeFalse();
        reason.Should().Contain("content-hash-mismatch");
    }
}
