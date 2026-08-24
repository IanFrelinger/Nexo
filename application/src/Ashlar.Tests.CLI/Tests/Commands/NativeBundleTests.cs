using System.Text.Json;
using Ashlar.CLI.Commands;
using Ashlar.Manifest;
using Ashlar.Manifest.Ledger;
using Ashlar.Manifest.Signing;
using FluentAssertions;
using Xunit;

namespace Ashlar.Tests.CLI.Tests.Commands;

/// <summary>
/// Pins the deterministic half of `ashlar export native`: what a project IS (verified / certified)
/// and the portable bundle laid out around it — the launchers, the descriptor, the staged app.
/// The self-contained runtime build is a separate publish step, smoke-tested by the e2e loop.
/// </summary>
[Trait("Category", "CLI")]
public sealed class NativeBundleTests : IDisposable
{
    private readonly string _dir;
    private readonly string _keyDir;

    public NativeBundleTests()
    {
        _dir = Path.Combine(Path.GetTempPath(), "native-" + Guid.NewGuid().ToString("N"));
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
        ProjectScaffold.TryScaffold("triage", out var m, out var p, out var reason).Should().BeTrue(reason);
        File.WriteAllText(Path.Combine(_dir, "ashlar.yaml"), m);
        File.WriteAllText(Path.Combine(_dir, "ashlar.policy.yaml"), p);
    }

    private async Task Certify()
    {
        var signer = OperatorKey.Generate(_keyDir);
        var m = File.ReadAllText(Path.Combine(_dir, "ashlar.yaml"));
        var p = File.ReadAllText(Path.Combine(_dir, "ashlar.policy.yaml"));
        await new InstanceLedger(Path.Combine(_dir, ".ashlar")).AppendVerificationAsync(
            signer, InstanceLedger.Subject(m, p), verified: true,
            [new LedgerCourse { Name = "contract", Passed = true, Detail = "ok" }], DateTimeOffset.UtcNow);
    }

    [Fact]
    public void Describe_reads_a_scaffolded_project_as_verified_but_uncertified()
    {
        Scaffold();

        var info = NativeBundle.Describe(_dir, "linux-x64");

        info.Name.Should().Be("triage");
        info.Verified.Should().BeTrue();
        info.Certified.Should().BeFalse("no signed ledger yet");
        info.SignerFingerprint.Should().BeNull();
        info.LedgerEntries.Should().Be(0);
    }

    [Fact]
    public async Task Describe_reads_a_certified_project_with_its_signer_and_ledger()
    {
        Scaffold();
        await Certify();

        var info = NativeBundle.Describe(_dir, "linux-x64");

        info.Certified.Should().BeTrue();
        info.SignerFingerprint.Should().StartWith("ed25519:");
        info.LedgerEntries.Should().Be(1);
    }

    [Fact]
    public async Task Stage_lays_out_a_self_proving_bundle()
    {
        Scaffold();
        await Certify();
        var info = NativeBundle.Describe(_dir, "linux-x64");
        var bundle = Path.Combine(_dir, "out");

        var written = NativeBundle.Stage(_dir, bundle, info);

        File.Exists(Path.Combine(bundle, "app", "ashlar.yaml")).Should().BeTrue();
        File.Exists(Path.Combine(bundle, "app", "ashlar.policy.yaml")).Should().BeTrue();
        Directory.Exists(Path.Combine(bundle, "app", ".ashlar")).Should().BeTrue("the signed ledger travels with the app");
        File.Exists(Path.Combine(bundle, "run.sh")).Should().BeTrue();
        File.Exists(Path.Combine(bundle, "run.cmd")).Should().BeTrue();
        File.Exists(Path.Combine(bundle, "README.md")).Should().BeTrue();
        written.Should().Contain("bundle.json");

        // The launcher verifies BEFORE it runs — that is the self-proving contract.
        var runSh = File.ReadAllText(Path.Combine(bundle, "run.sh"));
        var verifyAt = runSh.IndexOf("verify --path", StringComparison.Ordinal);
        var runAt = runSh.IndexOf("run \"$@\"", StringComparison.Ordinal);
        verifyAt.Should().BeGreaterThan(-1, "the launcher must verify");
        runAt.Should().BeGreaterThan(verifyAt, "verify must come before run");

        var descriptor = JsonSerializer.Deserialize<JsonElement>(File.ReadAllText(Path.Combine(bundle, "bundle.json")));
        descriptor.GetProperty("format").GetString().Should().Be(NativeBundle.Format);
        descriptor.GetProperty("name").GetString().Should().Be("triage");
        descriptor.GetProperty("rid").GetString().Should().Be("linux-x64");
        descriptor.GetProperty("certified").GetBoolean().Should().BeTrue();
        descriptor.GetProperty("runtime").GetString().Should().Be("ashlar");
    }

    [Fact]
    public void Stage_uses_the_windows_exe_name_and_launcher_for_a_win_rid()
    {
        Scaffold();
        var info = NativeBundle.Describe(_dir, "win-x64");
        var bundle = Path.Combine(_dir, "out-win");

        NativeBundle.Stage(_dir, bundle, info);

        var descriptor = JsonSerializer.Deserialize<JsonElement>(File.ReadAllText(Path.Combine(bundle, "bundle.json")));
        descriptor.GetProperty("runtime").GetString().Should().Be("ashlar.exe");
        File.ReadAllText(Path.Combine(bundle, "run.cmd")).Should().Contain("ashlar.exe");
    }
}
