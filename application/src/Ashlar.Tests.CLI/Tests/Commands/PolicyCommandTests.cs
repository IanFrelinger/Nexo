using FluentAssertions;
using Ashlar.CLI.Commands;
using Ashlar.Manifest;
using Xunit;

namespace Ashlar.Tests.CLI.Tests.Commands;

/// <summary>
/// The staged auto-apply opt-in verb: <c>ashlar policy set self_extend &lt;mode&gt;</c> (the command the
/// gate refusal and the scaffold already tell operators to run). Pins that it flips ONLY the mode,
/// preserves the rest of the policy, validates the result before writing (fail-closed), and refuses
/// anything that would widen the governance floor.
/// </summary>
public sealed class PolicyCommandTests : IDisposable
{
    private readonly string _dir;

    public PolicyCommandTests()
    {
        _dir = Path.Combine(Path.GetTempPath(), "ashlar-policy-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_dir);
    }

    public void Dispose()
    {
        try { Directory.Delete(_dir, recursive: true); } catch { /* best effort */ }
    }

    private string WritePolicy(string mode, string gates = "[sandbox]", string extra = "")
    {
        var yaml = $"""
            apiVersion: ashlar/v1
            kind: Policy
            sandbox:
              root: .
              writable: []
            selfExtend:
              # raise the dial deliberately
              mode: {mode}{extra}
              budget:
                extensions: 3
                window: 24h
              mayAdd: [brick]
              gatesRequired: {gates}
            never:
              - modify_gate
              - widen_sandbox
              - access_signing_keys
              - truncate_ledger
              - grant_capability
            """;
        var path = Path.Combine(_dir, "ashlar.policy.yaml");
        File.WriteAllText(path, yaml);
        return path;
    }

    private (int rc, string stdout, string stderr) Set(string key, string value)
    {
        var so = new StringWriter();
        var se = new StringWriter();
        var rc = PolicyCommand.Set(key, value, _dir, so, se);
        return (rc, so.ToString(), se.ToString());
    }

    private string CurrentMode(string path)
    {
        PolicyLoader.TryLoad(File.ReadAllText(path), out var p, out _).Should().BeTrue();
        return p!.SelfExtend.Mode;
    }

    [Fact]
    public void Set_sealedToProposing_flipsTheMode()
    {
        var path = WritePolicy("sealed");

        var (rc, stdout, _) = Set("self_extend", "proposing");

        rc.Should().Be(0);
        stdout.Should().Contain("sealed → proposing");
        CurrentMode(path).Should().Be("proposing");
    }

    [Fact]
    public void Set_toSelfExtending_arms_andWarns()
    {
        var path = WritePolicy("proposing");

        var (rc, stdout, _) = Set("self_extend", "self-extending");

        rc.Should().Be(0);
        stdout.Should().Contain("ARMED").And.Contain("AUTO-APPLY");
        CurrentMode(path).Should().Be("self-extending");
    }

    [Fact]
    public void Set_toSelfExtending_withNoGates_isRefused_andWritesNothing()
    {
        // sealed + [] gates loads fine; flipping it to self-extending would need gates, so it must be refused.
        var path = WritePolicy("sealed", gates: "[]");
        var before = File.ReadAllText(path);

        var (rc, _, stderr) = Set("self_extend", "self-extending");

        rc.Should().Be(1);
        stderr.Should().Contain("gatesRequired");
        File.ReadAllText(path).Should().Be(before, "a flip that would make the policy invalid must write nothing");
        CurrentMode(path).Should().Be("sealed");
    }

    [Fact]
    public void Set_unknownMode_isRejected()
    {
        var path = WritePolicy("sealed");
        var (rc, _, stderr) = Set("self_extend", "yolo");
        rc.Should().Be(1);
        stderr.Should().Contain("unknown mode");
        CurrentMode(path).Should().Be("sealed");
    }

    [Fact]
    public void Set_unsupportedKey_isRejected_soTheFloorStaysReadOnly()
    {
        WritePolicy("sealed");
        var (rc, _, stderr) = Set("never", "[]");
        rc.Should().Be(1);
        stderr.Should().Contain("self_extend");
        stderr.Should().Contain("not editable");
    }

    [Fact]
    public void Set_alreadyAtTarget_isIdempotentNoOp()
    {
        var path = WritePolicy("proposing");
        var before = File.ReadAllText(path);

        var (rc, stdout, _) = Set("self_extend", "proposing");

        rc.Should().Be(0);
        stdout.Should().Contain("already");
        File.ReadAllText(path).Should().Be(before, "an idempotent set changes nothing on disk");
    }

    [Fact]
    public void Set_disarmBackToSealed_worksEvenWithGatesPresent()
    {
        var path = WritePolicy("self-extending");
        var (rc, _, _) = Set("self_extend", "sealed");
        rc.Should().Be(0);
        CurrentMode(path).Should().Be("sealed");
    }

    [Fact]
    public void Set_preservesCommentsAndOtherFields()
    {
        var path = WritePolicy("sealed");

        Set("self_extend", "proposing").rc.Should().Be(0);

        var after = File.ReadAllText(path);
        after.Should().Contain("# raise the dial deliberately", "comments must survive the edit");
        after.Should().Contain("extensions: 3").And.Contain("mayAdd: [brick]");
        after.Should().Contain("- modify_gate", "the never-list must be untouched");
    }

    [Fact]
    public void Show_printsTheMode()
    {
        WritePolicy("proposing");
        var so = new StringWriter();
        var rc = PolicyCommand.Show(_dir, so, new StringWriter());
        rc.Should().Be(0);
        so.ToString().Should().Contain("self-extend mode : proposing");
    }

    [Fact]
    public void Set_withDuplicateModeKey_refuses_ratherThanSilentlyLeavingItArmed()
    {
        // A policy with TWO selfExtend.mode entries: YAML resolves last-wins (effective = self-extending),
        // but a naive textual edit would change the FIRST (proposing). Disarming to sealed must NOT
        // silently succeed while the node stays armed — it must refuse and write nothing.
        var path = Path.Combine(_dir, "ashlar.policy.yaml");
        File.WriteAllText(path, """
            apiVersion: ashlar/v1
            kind: Policy
            sandbox:
              root: .
              writable: []
            selfExtend:
              mode: proposing
              budget:
                extensions: 3
                window: 24h
              mayAdd: [brick]
              gatesRequired: [sandbox]
              mode: self-extending
            never:
              - modify_gate
              - widen_sandbox
              - access_signing_keys
              - truncate_ledger
              - grant_capability
            """);
        var before = File.ReadAllText(path);

        var (rc, _, stderr) = Set("self_extend", "sealed");

        rc.Should().Be(1, "a set that cannot be trusted to take effect must refuse");
        File.ReadAllText(path).Should().Be(before, "nothing may be written when the outcome can't be guaranteed");
        stderr.Should().NotBeNullOrWhiteSpace();
    }

    // ---- pure edit logic ----------------------------------------------------------------------

    [Fact]
    public void TryReplace_scopesToSelfExtend_andLeavesADecoyModeUntouched()
    {
        // A `mode:` under a different block must NOT be rewritten — only selfExtend's.
        var text = "sandbox:\n  mode: keepme\nselfExtend:\n  mode: sealed\n  budget:\n    extensions: 1\n";

        PolicyCommand.TryReplaceSelfExtendMode(text, "proposing", out var result).Should().BeTrue();

        result.Should().Contain("mode: keepme", "the decoy under sandbox is out of scope");
        result.Should().Contain("selfExtend:\n  mode: proposing");
    }

    [Fact]
    public void TryReplace_returnsFalse_whenNoSelfExtendBlock()
    {
        PolicyCommand.TryReplaceSelfExtendMode("sandbox:\n  root: .\n", "proposing", out _)
            .Should().BeFalse();
    }

    [Fact]
    public void TryReplace_preservesCrlfNewlines()
    {
        var text = "selfExtend:\r\n  mode: sealed\r\n  mayAdd: [brick]\r\n";
        PolicyCommand.TryReplaceSelfExtendMode(text, "proposing", out var result).Should().BeTrue();
        result.Should().Be("selfExtend:\r\n  mode: proposing\r\n  mayAdd: [brick]\r\n");
    }
}
