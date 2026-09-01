using System.CommandLine;
using FluentAssertions;
using Ashlar.CLI.Commands;
using Xunit;

namespace Ashlar.Tests.CLI.Tests.Commands;

/// <summary>
/// #466: <c>ashlar init &lt;name&gt; --path &lt;x&gt;</c> must refuse a non-directory --path legibly
/// instead of letting Directory.CreateDirectory throw an unhandled IOException (which leaks a stack
/// trace and source paths), and it must still scaffold cleanly into a fresh directory.
/// </summary>
public sealed class InitCommandTests : IDisposable
{
    private readonly string _dir;

    public InitCommandTests()
    {
        _dir = Path.Combine(Path.GetTempPath(), "ashlar-init-tests-" + Guid.NewGuid().ToString("N"));
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
            root.AddCommand(new InitCommand());
            var rc = await root.InvokeAsync(args).ConfigureAwait(false);
            return (rc, so.ToString(), se.ToString());
        }
        finally
        {
            // Restore to the known-good writers, never the (possibly foreign) inherited ones.
            Console.SetOut(ConsoleCapture.Out);
            Console.SetError(ConsoleCapture.Error);
        }
    }

    [Fact]
    public async Task Init_withPathThatIsAFile_isRejectedLegibly_notAStackTrace()
    {
        var file = Path.Combine(_dir, "not-a-dir");
        await File.WriteAllTextAsync(file, "i am a file");

        var (rc, _, stderr) = await RunAsync("init", "demo", "--path", file);

        rc.Should().NotBe(0, "a --path that is a file must fail");
        stderr.Should().Contain("REJECTED").And.Contain("not a directory");
        stderr.Should().NotContain("IOException", "the refusal must be legible, not a raw exception");
        stderr.Should().NotContain("   at ", "no stack trace may leak");
    }

    [Fact]
    public async Task Init_intoAFreshDirectory_writesBothDocuments()
    {
        var target = Path.Combine(_dir, "fresh-project");

        var (rc, _, _) = await RunAsync("init", "demo", "--path", target);

        rc.Should().Be(0);
        File.Exists(Path.Combine(target, "ashlar.yaml")).Should().BeTrue();
        File.Exists(Path.Combine(target, "ashlar.policy.yaml")).Should().BeTrue();
    }

    [Fact]
    public async Task Init_withAnOverLongName_isRejected_notScaffolded()
    {
        var target = Path.Combine(_dir, "long-name-project");
        var name = new string('a', 100_000);

        var (rc, _, stderr) = await RunAsync("init", name, "--path", target);

        rc.Should().NotBe(0);
        stderr.Should().Contain("REJECTED");
        File.Exists(Path.Combine(target, "ashlar.yaml")).Should().BeFalse("a refused name writes nothing");
    }
}
