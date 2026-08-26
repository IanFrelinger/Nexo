using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Ashlar.BackgroundAgents.Forge;
using Ashlar.BackgroundAgents.HostRunners;
using Ashlar.Manifest;
using Ashlar.Manifest.Admission;
using Ashlar.Manifest.Packaging;
using Ashlar.Manifest.Signing;
using Xunit;

namespace Ashlar.Tests.BackgroundAgents.SelfExtend;

/// <summary>
/// Pins the producer wiring: a self-extend cycle inside an ashlar project lands in the same
/// gate store <c>ashlar gates</c> reads, under the project policy's mode semantics — and
/// claims only the courses the cycle actually evidences.
/// </summary>
public sealed class SelfExtendAdmissionBridgeTests : IDisposable
{
    private readonly string _repo;

    public SelfExtendAdmissionBridgeTests()
    {
        _repo = Path.Combine(Path.GetTempPath(), "bridge-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_repo);
    }

    public void Dispose()
    {
        if (Directory.Exists(_repo))
        {
            Directory.Delete(_repo, recursive: true);
        }
    }

    private void WritePolicy(string mode, string gates = "[sandbox]", string mayAdd = "[brick]", int budget = 3) =>
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
              mayAdd: {mayAdd}
              gatesRequired: {gates}
            never:
              - modify_gate
              - widen_sandbox
              - access_signing_keys
              - truncate_ledger
              - grant_capability
            """);

    private Task<string?> RecordAsync(int denied = 0, IReadOnlyList<string>? writes = null) =>
        SelfExtendAdmissionBridge.TryRecordAsync(
            _repo, "night-agent", "handle the failing invoices",
            writes ?? ["src/Fix.cs"], toolCallsExecuted: 5, toolCallsDenied: denied,
            // autoShare pinned OFF so an exported ASHLAR_MESH_AUTOSHARE=1 cannot leak test
            // packages into the developer's real mesh store; the env path is pinned separately.
            NullLogger.Instance, autoShare: false);

    [Fact]
    public async Task Outside_an_ashlar_project_the_bridge_is_a_no_op()
    {
        // No policy file in the repo: the runner behaves exactly as before the wiring.
        (await RecordAsync()).Should().BeNull();
        Directory.Exists(Path.Combine(_repo, ".ashlar")).Should().BeFalse("a no-op writes nothing");
    }

    [Fact]
    public async Task Nothing_written_means_nothing_proposed()
    {
        WritePolicy("proposing");

        (await RecordAsync(writes: [])).Should().BeNull();
    }

    [Fact]
    public async Task A_confined_cycle_in_proposing_mode_lands_in_the_held_queue()
    {
        WritePolicy("proposing");

        var outcome = await RecordAsync();

        outcome.Should().Contain("held").And.Contain("ashlar gates");
        var held = await new GateStore(Path.Combine(_repo, ".ashlar")).ListAsync(ProposalState.Held);
        held.Should().ContainSingle();
        held[0].Proposal.ProposedBy.Should().Be("night-agent");
        held[0].Proposal.Courses.Should().ContainSingle(c => c.Name == "sandbox" && c.Passed);
    }

    [Fact]
    public async Task With_an_operator_key_the_runtimes_own_proposal_is_signed()
    {
        // A self-extend cycle's recorded verdict carries the same provenance as a manual gate
        // decision. The identity is injected here (a machine-key TryLoad would make the test
        // depend on the developer's ~/.ashlar/keys); production loads it from the machine.
        WritePolicy("proposing");
        var signer = OperatorKey.Generate(Path.Combine(_repo, "keys"));

        var outcome = await SelfExtendAdmissionBridge.TryRecordAsync(
            _repo, "night-agent", "handle the failing invoices",
            ["src/Fix.cs"], toolCallsExecuted: 5, toolCallsDenied: 0,
            NullLogger.Instance, default, forgeProposalIds: null, signer: signer);

        outcome.Should().Contain("held");

        // ListAsync verifies fail-closed, so a returned record with a signature is proof the
        // runtime signed it AND that it verifies — read here by a keyless store, via the
        // record's own embedded key.
        var held = (await new GateStore(Path.Combine(_repo, ".ashlar")).ListAsync(ProposalState.Held)).Single();
        held.Signer.Should().Be(signer.PublicKeyBase64);
        held.Sig.Should().NotBeNullOrEmpty("the runtime's proposal is signed with the operator identity");
    }

    [Fact]
    public async Task A_cycle_with_denials_fails_the_sandbox_course_and_is_rejected()
    {
        WritePolicy("proposing");

        var outcome = await RecordAsync(denied: 2);

        outcome.Should().Contain("rejected");
        var records = await new GateStore(Path.Combine(_repo, ".ashlar")).ListAsync();
        records.Should().ContainSingle().Which.State.Should().Be(ProposalState.Rejected);
    }

    [Fact]
    public async Task A_policy_requiring_gates_the_cycle_did_not_run_rejects_fail_closed()
    {
        // The bridge claims only what the cycle evidences (sandbox). A policy demanding
        // tests must reject with "did not run" — the runtime may not claim courses it did
        // not run, and the operator's policy decides whether the evidence suffices.
        WritePolicy("proposing", gates: "[sandbox, tests]");

        var outcome = await RecordAsync();

        outcome.Should().Contain("rejected").And.Contain("did not run");
    }

    [Fact]
    public async Task Sealed_mode_records_the_rejected_attempt()
    {
        WritePolicy("sealed", gates: "[]", mayAdd: "[]", budget: 0);

        var outcome = await RecordAsync();

        outcome.Should().Contain("rejected").And.Contain("sealed");
        // The attempt is in the ledger — sealed refuses, it does not forget.
        var records = await new GateStore(Path.Combine(_repo, ".ashlar")).ListAsync();
        records.Should().ContainSingle().Which.State.Should().Be(ProposalState.Rejected);
    }

    [Fact]
    public async Task An_unreadable_policy_is_a_loud_gate_error_never_a_skipped_gate()
    {
        File.WriteAllText(Path.Combine(_repo, "ashlar.policy.yaml"), "kind: {nonsense");

        var outcome = await RecordAsync();

        outcome.Should().Contain("GATE ERROR").And.Contain("REJECTED");
    }

    [Fact]
    public void The_mapped_proposal_passes_the_store_id_allowlist_and_claims_only_sandbox()
    {
        var proposal = SelfExtendAdmissionBridge.BuildProposal(
            "night-agent", "objective", ["a.cs", "b.cs"], 7, 0);

        proposal.Id.Should().MatchRegex("^ext-[0-9a-f]{12}$");
        proposal.Kind.Should().Be("brick");
        proposal.Courses.Should().ContainSingle().Which.Name.Should().Be("sandbox");
        proposal.Diff.Should().Contain("~ a.cs").And.Contain("~ b.cs");
    }

    // ─────────────────────────── auto-share (co-production) ───────────────────────────

    private string ParkForgeWrite(string content = "// coprod v1")
    {
        var forge = AshlarProjectMediation.ProjectStore(_repo);
        return forge.Add(new ChangeProposal
        {
            Id = "forge-" + Guid.NewGuid().ToString("N")[..8],
            TargetPath = "src/Coprod.cs",
            NewContent = content,
            Summary = "parked by the cycle",
            CreatedAt = DateTimeOffset.UtcNow,
        }).Id;
    }

    [Fact]
    public async Task An_admitted_cycle_auto_shares_a_sealed_package_to_the_mesh()
    {
        WritePolicy("self-extending");
        var signer = OperatorKey.Generate(Path.Combine(_repo, "keys"));
        var forgeId = ParkForgeWrite();
        var mesh = Path.Combine(_repo, "mesh");

        var outcome = await SelfExtendAdmissionBridge.TryRecordAsync(
            _repo, "night-agent", "co-produce the classifier", writePaths: [],
            toolCallsExecuted: 4, toolCallsDenied: 0, NullLogger.Instance,
            forgeProposalIds: [forgeId], signer: signer, autoShare: true, meshDir: mesh);

        outcome.Should().Contain("admitted").And.Contain("shared");
        var package = Directory.EnumerateFiles(mesh, "*.ashpkg").Should().ContainSingle().Which;
        ExtensionPackaging.TryOpen(File.ReadAllText(package), out var pkg, out var reason).Should().BeTrue(reason);
        pkg!.Files.Should().ContainSingle().Which.Content.Should().Be("// coprod v1",
            "what travels is exactly the admitted, applied write");
        File.ReadAllText(Path.Combine(_repo, "src", "Coprod.cs")).Should().Be("// coprod v1",
            "the admission applied before the share");
    }

    [Fact]
    public async Task A_share_failure_annotates_the_outcome_but_never_fails_the_cycle()
    {
        WritePolicy("self-extending");
        var signer = OperatorKey.Generate(Path.Combine(_repo, "keys"));
        var forgeId = ParkForgeWrite();
        // A FILE where the store directory should be: CreateDirectory refuses, the share fails —
        // best-effort means the admission and its applied write survive that untouched.
        var blocked = Path.Combine(_repo, "not-a-dir");
        File.WriteAllText(blocked, "occupied");

        var outcome = await SelfExtendAdmissionBridge.TryRecordAsync(
            _repo, "night-agent", "co-produce the classifier", writePaths: [],
            toolCallsExecuted: 4, toolCallsDenied: 0, NullLogger.Instance,
            forgeProposalIds: [forgeId], signer: signer, autoShare: true, meshDir: blocked);

        outcome.Should().Contain("admitted", "the admission stands whatever the share does")
            .And.Contain("auto-share failed");
        File.Exists(Path.Combine(_repo, "src", "Coprod.cs")).Should().BeTrue(
            "the applied write is untouched by the share failure");
    }

    [Fact]
    public async Task Auto_share_stays_off_unless_opted_in()
    {
        WritePolicy("self-extending");
        var signer = OperatorKey.Generate(Path.Combine(_repo, "keys"));
        var forgeId = ParkForgeWrite();
        var mesh = Path.Combine(_repo, "mesh");

        var outcome = await SelfExtendAdmissionBridge.TryRecordAsync(
            _repo, "night-agent", "co-produce the classifier", writePaths: [],
            toolCallsExecuted: 4, toolCallsDenied: 0, NullLogger.Instance,
            forgeProposalIds: [forgeId], signer: signer, autoShare: false, meshDir: mesh);

        outcome.Should().Contain("admitted").And.NotContain("shared");
        Directory.Exists(mesh).Should().BeFalse("nothing leaves the project without opt-in");
    }
}
