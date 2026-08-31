using Ashlar.Core.Application.Certification.Ports;
using Ashlar.Infrastructure.Certification;
using FluentAssertions;
using Xunit;

namespace Ashlar.Tests.Infrastructure.Tests.Certification;

/// <summary>
/// A4 canary unit tests: <see cref="RoslynPostApplyVerification"/> reads the files AS THEY LANDED and
/// recompiles them in-process, so the apply path can revert a change that does not hold together on
/// disk. Pins clean-passes, broken-fails, docs-only-passes-trivially, and fail-closed on an unreadable
/// applied file.
///
/// <para>In <c>...Tests.Certification</c> so it rides cert-gate. Hermetic: in-process Roslyn, no SDK.</para>
/// </summary>
[Trait("Category", "Certification")]
public sealed class PostApplyVerificationTests : IDisposable
{
    private readonly string _root;
    private static readonly RoslynPostApplyVerification Verifier = new();

    public PostApplyVerificationTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "ashlar-postapply-" + Guid.NewGuid().ToString("N")[..12]);
        Directory.CreateDirectory(_root);
    }

    public void Dispose()
    {
        try { Directory.Delete(_root, recursive: true); } catch { /* best effort */ }
    }

    [Fact]
    public async Task CleanAppliedCode_passes()
    {
        var applied = Write("src/Greeter.cs", "namespace Demo; public sealed class Greeter { public string Hi() => \"hi\"; }");

        var r = await Verifier.VerifyAsync(_root, new[] { applied });

        r.Passed.Should().BeTrue(r.Detail);
        r.Detail.Should().Contain("verified clean");
    }

    [Fact]
    public async Task BrokenAppliedCode_fails_withDiagnostics()
    {
        var applied = Write("src/Broken.cs", "namespace Demo; public sealed class Broken { public int Oops() => ; }");

        var r = await Verifier.VerifyAsync(_root, new[] { applied });

        r.Passed.Should().BeFalse();
        r.Detail.Should().Contain("post-apply error");
    }

    [Fact]
    public async Task DocsOnly_passesTrivially()
    {
        var applied = Write("docs/readme.md", "# just docs");

        (await Verifier.VerifyAsync(_root, new[] { applied })).Passed
            .Should().BeTrue("a change that applied no .cs has nothing to recompile");
    }

    [Fact]
    public async Task UnreadableAppliedFile_isFailClosed()
    {
        // A .cs file the gate admitted that is not on disk when the canary runs is an anomaly, not a pass.
        var missing = new AppliedFile("src/Gone.cs", Path.Combine(_root, "src", "Gone.cs"));

        var r = await Verifier.VerifyAsync(_root, new[] { missing });

        r.Passed.Should().BeFalse("an applied file that cannot be read back must fail closed");
        r.Detail.Should().Contain("could not read");
    }

    private AppliedFile Write(string relative, string content)
    {
        var full = Path.Combine(_root, relative.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(full)!);
        File.WriteAllText(full, content);
        return new AppliedFile(relative, full);
    }
}
