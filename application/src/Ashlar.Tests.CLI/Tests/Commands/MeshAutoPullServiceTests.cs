using FluentAssertions;
using Ashlar.CLI.Commands.BackgroundAgent;
using Xunit;

namespace Ashlar.Tests.CLI.Tests.Commands;

/// <summary>
/// A5 consumer auto-pull (the new logic): <see cref="MeshAutoPullService.PullOnceAsync"/> enumerates
/// <c>*.ashpkg</c> and submits each through the shared import path. Pins the scan/aggregate/skip/
/// missing-dir behaviour. The trust-gate + admission decision themselves are Phase-3 tested
/// (PackageTrustGateTests + e2e-loop pkg-import-refuses-untrusted-signer) and proven end-to-end
/// across two nodes in the container lab; these keep the new wiring honest.
/// </summary>
public sealed class MeshAutoPullServiceTests : IDisposable
{
    private readonly string _dir;
    private readonly string _pull;
    private readonly string _project;

    public MeshAutoPullServiceTests()
    {
        _dir = Path.Combine(Path.GetTempPath(), "ashlar-autopull-" + Guid.NewGuid().ToString("N"));
        _pull = Path.Combine(_dir, "share");
        _project = Path.Combine(_dir, "project");
        Directory.CreateDirectory(_project);
    }

    public void Dispose()
    {
        try { Directory.Delete(_dir, recursive: true); } catch { /* best effort */ }
    }

    [Fact]
    public async Task MissingPullDir_isANoOp()
    {
        var s = await MeshAutoPullService.PullOnceAsync(_pull, _project);
        s.Should().Be(MeshPullSummary.Empty);
    }

    [Fact]
    public async Task EmptyPullDir_scansNothing()
    {
        Directory.CreateDirectory(_pull);
        var s = await MeshAutoPullService.PullOnceAsync(_pull, _project);
        s.Scanned.Should().Be(0);
    }

    [Fact]
    public async Task MalformedPackage_isRefused_notAnError()
    {
        Directory.CreateDirectory(_pull);
        await File.WriteAllTextAsync(Path.Combine(_pull, "junk.ashpkg"), "{ not a real package");

        var s = await MeshAutoPullService.PullOnceAsync(_pull, _project);

        s.Scanned.Should().Be(1);
        s.Refused.Should().Be(1, "an unopenable package is refused (fail-closed), never applied");
        s.Errors.Should().Be(0, "a refusal is an expected outcome, not a thrown error");
    }

    [Fact]
    public async Task Dotfiles_and_appleDoubleSidecars_areSkipped()
    {
        Directory.CreateDirectory(_pull);
        await File.WriteAllTextAsync(Path.Combine(_pull, "real.ashpkg"), "garbage");
        await File.WriteAllTextAsync(Path.Combine(_pull, "._real.ashpkg"), "garbage");  // macOS AppleDouble
        await File.WriteAllTextAsync(Path.Combine(_pull, ".hidden.ashpkg"), "garbage");

        var s = await MeshAutoPullService.PullOnceAsync(_pull, _project);

        s.Scanned.Should().Be(1, "only real.ashpkg is a package; dotfiles and AppleDouble sidecars are skipped");
    }

    [Fact]
    public async Task NonAshpkgFiles_areIgnored()
    {
        Directory.CreateDirectory(_pull);
        await File.WriteAllTextAsync(Path.Combine(_pull, "notes.txt"), "hello");
        await File.WriteAllTextAsync(Path.Combine(_pull, "package.nxpkg"), "{}");  // the unsigned path — never pulled

        var s = await MeshAutoPullService.PullOnceAsync(_pull, _project);

        s.Scanned.Should().Be(0, "only *.ashpkg is pulled; .txt and the unsigned .nxpkg path are ignored");
    }
}
