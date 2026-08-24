using Ashlar.CLI.Commands;
using Ashlar.Manifest;
using Ashlar.Manifest.Ledger;
using Ashlar.Manifest.Signing;
using FluentAssertions;
using Xunit;

namespace Ashlar.Tests.CLI.Tests.Commands;

/// <summary>
/// Pins the "is THIS project ready to ship?" ladder the doctor renders: not-a-project is skipped,
/// and a real project climbs NOT CERTIFIED → READY TO CERTIFY → CERTIFIED as a key and a signed
/// ledger appear — while a corrupt key or ledger is a hard BLOCK.
/// </summary>
[Trait("Category", "CLI")]
public sealed class DoctorProjectReadinessTests : IDisposable
{
    private readonly string _dir;
    private readonly string _keyDir;

    public DoctorProjectReadinessTests()
    {
        _dir = Path.Combine(Path.GetTempPath(), "doctor-ready-" + Guid.NewGuid().ToString("N"));
        _keyDir = Path.Combine(_dir, "keys");
        Directory.CreateDirectory(_dir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_dir))
        {
            Directory.Delete(_dir, recursive: true);
        }
    }

    private void Scaffold()
    {
        ProjectScaffold.TryScaffold("doctor-demo", out var m, out var p, out var reason).Should().BeTrue(reason);
        File.WriteAllText(Path.Combine(_dir, "ashlar.yaml"), m);
        File.WriteAllText(Path.Combine(_dir, "ashlar.policy.yaml"), p);
    }

    private async Task<LedgerEntry> Certify(SigningIdentity signer)
    {
        var m = File.ReadAllText(Path.Combine(_dir, "ashlar.yaml"));
        var p = File.ReadAllText(Path.Combine(_dir, "ashlar.policy.yaml"));
        return await new InstanceLedger(Path.Combine(_dir, ".ashlar")).AppendVerificationAsync(
            signer, InstanceLedger.Subject(m, p), verified: true,
            [new LedgerCourse { Name = "contract", Passed = true, Detail = "ok" }], DateTimeOffset.UtcNow);
    }

    [Fact]
    public void A_non_project_directory_yields_no_readiness_block()
    {
        DoctorProjectReadiness.Assess(_dir, _keyDir).Should().BeNull("no manifest/policy — nothing to assess");
    }

    [Fact]
    public void A_verified_project_with_no_key_is_not_certified()
    {
        Scaffold();

        var r = DoctorProjectReadiness.Assess(_dir, _keyDir)!;

        r.Verified.Should().BeTrue();
        r.KeyStatus.Should().Be("absent");
        r.Verdict.Should().Be("NOT CERTIFIED");
        r.NextStep.Should().Contain("keys init");
        r.Ready.Should().BeFalse();
    }

    [Fact]
    public void A_keyed_project_with_no_ledger_is_ready_to_certify()
    {
        Scaffold();
        OperatorKey.Generate(_keyDir);

        var r = DoctorProjectReadiness.Assess(_dir, _keyDir)!;

        r.KeyStatus.Should().Be("present");
        r.Fingerprint.Should().StartWith("ed25519:");
        r.LedgerStatus.Should().Be("none");
        r.Verdict.Should().Be("READY TO CERTIFY");
    }

    [Fact]
    public async Task A_keyed_project_with_a_signed_ledger_is_certified_and_ready()
    {
        Scaffold();
        var signer = OperatorKey.Generate(_keyDir);
        await Certify(signer);

        var r = DoctorProjectReadiness.Assess(_dir, _keyDir)!;

        r.Verdict.Should().Be("CERTIFIED");
        r.LedgerStatus.Should().Be("intact");
        r.LedgerEntries.Should().Be(1);
        r.Ready.Should().BeTrue();
    }

    [Fact]
    public async Task A_corrupt_ledger_is_a_hard_block()
    {
        Scaffold();
        var signer = OperatorKey.Generate(_keyDir);
        await Certify(signer);
        await File.WriteAllTextAsync(Path.Combine(_dir, ".ashlar", "ledger", "000001.json"), "{ not valid");

        var r = DoctorProjectReadiness.Assess(_dir, _keyDir)!;

        r.LedgerStatus.Should().Be("corrupt");
        r.Verdict.Should().Be("BLOCKED");
        r.Ready.Should().BeFalse();
    }
}
