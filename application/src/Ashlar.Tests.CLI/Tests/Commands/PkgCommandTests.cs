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
}
