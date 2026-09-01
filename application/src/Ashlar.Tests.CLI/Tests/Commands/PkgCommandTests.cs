using System.CommandLine;
using FluentAssertions;
using Ashlar.CLI.Commands;
using Xunit;

namespace Ashlar.Tests.CLI.Tests.Commands;

/// <summary>
/// #458: <c>ashlar pkg pull --from</c> takes a mesh store DIRECTORY. An HTTP URL used to be coerced
/// into a DirectoryInfo, mangling the URL into a nonsense path that then "does not exist"; it must be
/// refused legibly, pointing at the daemon's ASHLAR_MESH_PEERS instead. The ordinary directory path
/// must keep working after the option changed from DirectoryInfo to string.
/// </summary>
public sealed class PkgCommandTests : IDisposable
{
    private readonly string _dir;

    public PkgCommandTests()
    {
        _dir = Path.Combine(Path.GetTempPath(), "ashlar-pkg-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_dir);
    }

    public void Dispose()
    {
        try { Directory.Delete(_dir, recursive: true); } catch { /* best effort */ }
    }

    private static async Task<(int rc, string stdout, string stderr)> RunAsync(params string[] args)
    {
        var so = new StringWriter();  // not disposed: a disposed writer left on Console poisons later tests
        var se = new StringWriter();
        Console.SetOut(so);
        Console.SetError(se);
        try
        {
            var root = new RootCommand();
            root.AddCommand(new PkgCommand());
            var rc = await root.InvokeAsync(args).ConfigureAwait(false);
            return (rc, so.ToString(), se.ToString());
        }
        finally
        {
            Console.SetOut(ConsoleCapture.Out);
            Console.SetError(ConsoleCapture.Error);
        }
    }

    [Theory]
    [InlineData("http://peer.example:8080/store")]
    [InlineData("https://peer.example/store")]
    public async Task Pull_fromHttpUrl_isRefusedLegibly_notMangledIntoAPath(string url)
    {
        var project = Path.Combine(_dir, "proj");
        Directory.CreateDirectory(project);
        await File.WriteAllTextAsync(Path.Combine(project, "ashlar.yaml"), "kind: Application\n");
        await File.WriteAllTextAsync(Path.Combine(project, "ashlar.policy.yaml"), "kind: Policy\n");

        var (rc, _, stderr) = await RunAsync("pkg", "pull", "--from", url, "--path", project);

        rc.Should().NotBe(0);
        stderr.Should().Contain("takes a directory");
        stderr.Should().Contain("ASHLAR_MESH_PEERS", "the refusal must point at the daemon's HTTP-pull path");
        stderr.Should().NotContain("no such peer store", "the URL must not be mangled into a path that 'does not exist'");
    }

    [Fact]
    public async Task Pull_fromAnEmptyDirectoryStore_stillWorksAfterTheStringRefactor()
    {
        // Proves --from as a plain directory string still reaches the normal path: an existing but
        // empty store is "nothing to pull", exit 0 — no URL false-positive, no coercion breakage.
        var project = Path.Combine(_dir, "proj2");
        Directory.CreateDirectory(project);
        await File.WriteAllTextAsync(Path.Combine(project, "ashlar.yaml"), "kind: Application\n");
        await File.WriteAllTextAsync(Path.Combine(project, "ashlar.policy.yaml"), "kind: Policy\n");

        var store = Path.Combine(_dir, "empty-store");
        Directory.CreateDirectory(store);

        var (rc, stdout, _) = await RunAsync("pkg", "pull", "--from", store, "--path", project);

        rc.Should().Be(0);
        stdout.Should().Contain("nothing to pull");
    }

    private async Task<string> ProjectAsync(string name)
    {
        var project = Path.Combine(_dir, name);
        Directory.CreateDirectory(project);
        await File.WriteAllTextAsync(Path.Combine(project, "ashlar.yaml"), "kind: Application\n");
        await File.WriteAllTextAsync(Path.Combine(project, "ashlar.policy.yaml"), "kind: Policy\n");
        return project;
    }

    // Sparse. The guard reads FileInfo.Length and never the bytes, so there is nothing to fill in;
    // 16 MiB + 1 KiB is one kilobyte past the CLI's ceiling, which is also ExtensionPackaging's parse
    // ceiling — the two layers are separated by exactly this margin, which is why the assertions
    // below are on the WORDING and not on the exit code: both layers refuse with 65, and only the
    // wording says whether the file was bounded before it was buffered.
    private static void WriteOversizedPackage(string path)
    {
        using var fs = new FileStream(path, FileMode.Create, FileAccess.Write);
        fs.SetLength(16L * 1024 * 1024 + 1024);
    }

    [Fact]
    public async Task Publish_ofAnOversizedPackage_isRefusedBeforeTheFileIsRead()
    {
        // A .ashpkg handed to publish routinely arrived off a share, so it is no more trustworthy
        // here than at import. Publish read it unbounded and was rescued only by TryOpen's char cap —
        // a cap that measures a string which exists only once the read has already succeeded, or
        // exhausted memory trying.
        var giant = Path.Combine(_dir, "giant.ashpkg");
        WriteOversizedPackage(giant);
        var store = Path.Combine(_dir, "publish-store");

        var (rc, _, stderr) = await RunAsync("pkg", "publish", giant, "--store", store);

        rc.Should().Be(65, "an oversized package is a verification refusal, exactly as for pkg import and pkg show");
        stderr.Should().Contain("bytes", "the refusal must come from the pre-read byte guard, not the post-read char cap");
        stderr.Should().Contain("refusing before reading it");
        stderr.Should().NotContain("characters", "reaching the char cap means the whole file was buffered first");
        Directory.Exists(store).Should().BeFalse("a refused package never reaches the mesh store");
    }

    [Fact]
    public async Task Pull_ofAnOversizedPackage_refusesTheRowAndKeepsPulling()
    {
        var project = await ProjectAsync("proj-oversized");

        var store = Path.Combine(_dir, "planted-store");
        Directory.CreateDirectory(store);
        // Ordinal order puts the planted giant FIRST, so a pull that ends on it never reaches the
        // second file — which is the denial being pinned. A mesh store is a plain directory, so
        // planting needs no publish and no key.
        WriteOversizedPackage(Path.Combine(store, "aaa-planted.ashpkg"));
        await File.WriteAllTextAsync(Path.Combine(store, "zzz-junk.ashpkg"), "{ not a package");

        var (rc, stdout, _) = await RunAsync("pkg", "pull", "--from", store, "--path", project);

        rc.Should().Be(65, "a refusal is never masked — the pass exits non-zero so a script notices");
        stdout.Should().Contain("bytes", "the row must be refused by the byte guard, before the read");
        stdout.Should().Contain("refusing before reading it");
        stdout.Should().Contain("zzz-junk.ashpkg", "the loop must continue past the planted row");
        stdout.Should().Contain("pulled 2");
        stdout.Should().Contain("refused/rejected 2", "the oversized row is counted as refused, not skipped");
    }

    // file:/// spelled the way each platform spells it, built by hand rather than by asking Uri to
    // parse a bare path — that parse is exactly the host-dependent behaviour the fix refuses to
    // rely on, so a test must not lean on it either.
    private static string FileUrl(string directory)
    {
        var full = Path.GetFullPath(directory).Replace('\\', '/');
        return full.StartsWith('/') ? "file://" + full : "file:///" + full;
    }

    [Fact]
    public async Task Pull_fromFileUrl_reachesTheDirectoryItNames()
    {
        // A mesh store is a folder, and a sync client or a file manager hands out its location as a
        // file:// URL. That URL names the same directory a plain path does, so pull must reach it
        // instead of coercing "file:///…" into a DirectoryInfo and reporting the store as missing.
        var project = await ProjectAsync("proj-file-url");
        var store = Path.Combine(_dir, "file-url-store");
        Directory.CreateDirectory(store);

        var (rc, stdout, stderr) = await RunAsync("pkg", "pull", "--from", FileUrl(store), "--path", project);

        rc.Should().Be(0, "a file: URL names a directory, the one URL shape pull can honour");
        stdout.Should().Contain("nothing to pull");
        stderr.Should().NotContain("no such peer store", "the operator's own store must not be reported missing");
    }

    [Theory]
    [InlineData("ftp://peer.example/store")]
    [InlineData("ssh://peer.example/store")]
    [InlineData("s3://bucket/store")]
    public async Task Pull_fromAnUnsupportedScheme_isRefusedLegibly_notMangledIntoAPath(string url)
    {
        // Nothing stands behind these transports here. Coercing them into a DirectoryInfo produced
        // "no such peer store: <nonsense>", which reads as the store being gone rather than as the
        // scheme being unsupported.
        var project = await ProjectAsync("proj-scheme");

        var (rc, _, stderr) = await RunAsync("pkg", "pull", "--from", url, "--path", project);

        rc.Should().NotBe(0);
        stderr.Should().Contain("takes a directory");
        stderr.Should().NotContain("no such peer store", "the URL must not be mangled into a path that 'does not exist'");
    }

    [Fact]
    public async Task Pull_fromADirectoryWhoseNameLooksLikeUrlSyntax_isPassedThroughUntouched()
    {
        // Guards the tempting WRONG fix for file:// — routing every Uri.IsFile token through
        // Uri.LocalPath. On Unix an ordinary rooted path parses as an absolute file: URI, so that fix
        // would percent-decode "%20" and truncate at "#", then report a live store as missing. This
        // test cannot fail against the pre-fix code; it exists to fail against that one.
        var project = await ProjectAsync("proj-urlish");
        var store = Path.Combine(_dir, "store%20v1#2");
        Directory.CreateDirectory(store);

        var (rc, stdout, _) = await RunAsync("pkg", "pull", "--from", store, "--path", project);

        rc.Should().Be(0, "a directory name is not URL syntax to be decoded");
        stdout.Should().Contain("nothing to pull");
    }

    [Fact]
    public async Task Pull_fromAnEmptyToken_isRefusedLegibly_notAStackTrace()
    {
        // `new DirectoryInfo("")` throws, so the operator got an unhandled exception where a refusal
        // belongs — the same shape as any other unusable --from.
        var project = await ProjectAsync("proj-empty");

        var (rc, _, stderr) = await RunAsync("pkg", "pull", "--from", string.Empty, "--path", project);

        rc.Should().NotBe(0);
        stderr.Should().Contain("takes a directory");
        stderr.Should().NotContain("Unhandled exception");
    }
}
