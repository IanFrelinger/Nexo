using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Ashlar.BackgroundAgents.Forge;
using Ashlar.BackgroundAgents.HostRunners;
using Ashlar.Manifest.Admission;
using Xunit;

namespace Ashlar.Tests.BackgroundAgents.SelfExtend;

/// <summary>
/// Pins M1 enforcement ordering: propose → HOLD → apply, with the hold BEFORE the write.
/// Mediated writes are parked forge proposals; the gate's verdict decides whether they ever
/// reach disk. The sentence under test: <strong>sealed seals by construction</strong> —
/// nothing was on disk to begin with, and a rejection means nothing ever lands.
/// </summary>
public sealed class M1EnforcementTests : IDisposable
{
    private readonly string _repo;

    public M1EnforcementTests()
    {
        _repo = Path.Combine(Path.GetTempPath(), "m1-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_repo);
    }

    public void Dispose()
    {
        if (Directory.Exists(_repo))
        {
            Directory.Delete(_repo, recursive: true);
        }
    }

    private void WritePolicy(string mode, string gates = "[sandbox]", int budget = 3) =>
        File.WriteAllText(Path.Combine(_repo, "ashlar.policy.yaml"), $"""
            apiVersion: ashlar/v1
            kind: Policy
            sandbox:
              root: .
              writable: []
            selfExtend:
              mode: {mode}
              budget:
                extensions: {budget}
                window: 24h
              mayAdd: [brick]
              gatesRequired: {gates}
            never:
              - modify_gate
              - widen_sandbox
              - access_signing_keys
              - truncate_ledger
              - grant_capability
            """);

    private string Park(ChangeProposalStore forge, string target = "src/Generated.cs", string content = "// generated")
    {
        var proposal = forge.Add(new ChangeProposal
        {
            Id = "forge-" + Guid.NewGuid().ToString("N")[..8],
            TargetPath = target,
            NewContent = content,
            Summary = "parked by the cycle",
            CreatedAt = DateTimeOffset.UtcNow,
        });
        return proposal.Id;
    }

    private Task<string?> RecordAsync(IReadOnlyList<string> forgeIds) =>
        SelfExtendAdmissionBridge.TryRecordAsync(
            _repo, "night-agent", "improve the classifier", writePaths: [],
            toolCallsExecuted: 4, toolCallsDenied: 1 /* mediation steering, not a violation */,
            // autoShare pinned OFF: with the null default, an exported ASHLAR_MESH_AUTOSHARE=1 on
            // the machine would make this test publish into the developer's REAL mesh store.
            NullLogger.Instance, default, forgeIds, autoShare: false);

    // ─────────────────────────── the three modes ───────────────────────────

    [Fact]
    public async Task Proposing_holds_and_the_write_is_not_on_disk()
    {
        WritePolicy("proposing");
        var forge = AshlarProjectMediation.ProjectStore(_repo);
        var forgeId = Park(forge);

        var outcome = await RecordAsync([forgeId]);

        outcome.Should().Contain("held");
        File.Exists(Path.Combine(_repo, "src/Generated.cs")).Should().BeFalse(
            "the hold comes BEFORE the write — that is the whole point of M1");
        forge.Find(forgeId)!.Status.Should().Be(ChangeProposalStatus.Proposed);

        var record = (await new GateStore(Path.Combine(_repo, ".ashlar")).ListAsync(ProposalState.Held)).Single();
        record.Proposal.ForgeProposalIds.Should().ContainSingle().Which.Should().Be(forgeId);
        record.Proposal.Courses.Single(c => c.Name == "sandbox").Passed.Should().BeTrue(
            "mediated confinement is structural; steering denials are not violations");
    }

    [Fact]
    public async Task Sealed_seals_by_construction()
    {
        WritePolicy("sealed", gates: "[]", budget: 0);
        var forge = AshlarProjectMediation.ProjectStore(_repo);
        var forgeId = Park(forge);

        var outcome = await RecordAsync([forgeId]);

        outcome.Should().Contain("rejected").And.Contain("sealed");
        File.Exists(Path.Combine(_repo, "src/Generated.cs")).Should().BeFalse("nothing ever lands");
        forge.Find(forgeId)!.Status.Should().Be(ChangeProposalStatus.Rejected,
            "the gate's rejection rejects the parked write too — no orphaned pending work");
    }

    [Fact]
    public async Task SelfExtending_within_budget_applies_now()
    {
        WritePolicy("self-extending");
        var forge = AshlarProjectMediation.ProjectStore(_repo);
        var forgeId = Park(forge, content: "// admitted by the gate");

        var outcome = await RecordAsync([forgeId]);

        outcome.Should().Contain("admitted");
        File.ReadAllText(Path.Combine(_repo, "src/Generated.cs")).Should().Be("// admitted by the gate");
        forge.Find(forgeId)!.Status.Should().Be(ChangeProposalStatus.Applied);
    }

    // ─────────────────────────── the applier ───────────────────────────

    [Fact]
    public void An_escaping_target_fails_the_whole_batch_before_any_apply()
    {
        var forge = AshlarProjectMediation.ProjectStore(_repo);
        var good = Park(forge, target: "src/Ok.cs");
        var evil = Park(forge, target: "../outside.cs");

        var act = () => ForgeApplier.ApplyAll(forge, [good, evil], _repo, "tester");

        act.Should().Throw<InvalidOperationException>().WithMessage("*escapes the project root*whole batch*");
        File.Exists(Path.Combine(_repo, "src/Ok.cs")).Should().BeFalse("no partial applies on a bad batch");
    }

    [Theory]
    [InlineData("ashlar.policy.yaml")]
    [InlineData("ashlar.yaml")]
    [InlineData(".ashlar/gates/ext-x.json")]
    [InlineData(".ashlar/ledger/000001.json")]
    [InlineData(".ashlar/keys/operator.key")]
    public void An_admitted_write_can_never_touch_a_governance_path(string target)
    {
        // The critical hole the review caught: containment was only "inside the root", so an
        // admitted brick could overwrite the policy that governs it, the signed ledger, or the
        // gate records — the concrete acts the never-list forbids. The apply step now refuses
        // them structurally, for the WHOLE batch, before any write.
        var forge = AshlarProjectMediation.ProjectStore(_repo);
        var good = Park(forge, target: "src/Ok.cs");
        var evil = Park(forge, target: target);

        var act = () => ForgeApplier.ApplyAll(forge, [good, evil], _repo, "gate");

        act.Should().Throw<InvalidOperationException>().WithMessage("*governance path*whole batch*");
        File.Exists(Path.Combine(_repo, "src/Ok.cs")).Should().BeFalse("a governance target fails the whole batch — no partial apply");
        File.Exists(Path.Combine(_repo, target)).Should().BeFalse();
    }

    [Fact]
    public void IsGovernancePath_names_the_self_governance_surface_case_insensitively()
    {
        foreach (var p in new[] { "ashlar.policy.yaml", "ASHLAR.YAML", ".ashlar/gates/x.json", ".Ashlar/ledger/1.json" })
        {
            ForgeApplier.IsGovernancePath(p).Should().BeTrue(p);
        }
        foreach (var p in new[] { "src/ashlar.yaml", "docs/ashlar.policy.yaml", "src/Brick.cs", "ashlarish.cs" })
        {
            ForgeApplier.IsGovernancePath(p).Should().BeFalse(p);
        }
    }

    [Fact]
    public void Apply_writes_approves_and_marks_applied()
    {
        var forge = AshlarProjectMediation.ProjectStore(_repo);
        var id = Park(forge, target: "docs/note.md", content: "held then admitted");

        var applied = ForgeApplier.ApplyAll(forge, [id], _repo, "ian.f");

        applied.Should().ContainSingle().Which.Should().Be("docs/note.md");
        File.ReadAllText(Path.Combine(_repo, "docs/note.md")).Should().Be("held then admitted");
        forge.Find(id)!.Status.Should().Be(ChangeProposalStatus.Applied);
    }

    // ─────────────────────────── mediation enforcement ───────────────────────────

    [Fact]
    public void The_fixed_mode_store_cannot_be_switched_at_runtime()
    {
        var store = new FixedAggressivenessModeStore(Ashlar.Abstractions.BackgroundAgentAggressivenessMode.Passive);

        store.GetMode().Should().Be(Ashlar.Abstractions.BackgroundAgentAggressivenessMode.Passive);
        var act = () => store.SetMode(Ashlar.Abstractions.BackgroundAgentAggressivenessMode.Active);
        act.Should().Throw<InvalidOperationException>().WithMessage("*policy*not switchable*");
    }
}
