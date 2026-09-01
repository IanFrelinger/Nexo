using Ashlar.Core.Application.Paths;
using FluentAssertions;
using Xunit;

namespace Ashlar.Tests.Infrastructure.Tests.Certification;

/// <summary>
/// <see cref="MediatedWritePath"/> is the single governance-floor authority every mediated writer
/// (forge apply, package import, shared-adaptation adopt) routes through. These assertions pin the
/// floor directly, so a regression is caught here regardless of which writer calls it. In
/// <c>...Tests.Certification</c> so it rides cert-gate (ci/cert-gate-assertions.md). Hermetic:
/// pure string logic plus a temp dir for the containment cases.
/// </summary>
[Trait("Category", "Certification")]
public sealed class MediatedWritePathTests : IDisposable
{
    private readonly string _root;

    public MediatedWritePathTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "ashlar-mwp-" + Guid.NewGuid().ToString("N")[..12]);
        Directory.CreateDirectory(_root);
    }

    public void Dispose()
    {
        try { Directory.Delete(_root, recursive: true); } catch { /* best effort */ }
    }

    public static TheoryData<string> GovernancePaths() => new()
    {
        "ashlar.yaml", "ashlar.policy.yaml",
        ".ashlar/gates/g1.json", ".ashlar/ledger.jsonl",
        "Directory.Build.props", "Directory.Build.targets", "Directory.Packages.props",
        "Directory.Solution.props", "Directory.Solution.targets",
        "after.Ashlar.sln.targets", "before.Ashlar.sln.targets",
        "nested/dir/Directory.Build.targets", "build/custom.props", "x/y/z.targets",
        "nuget.config", "global.json", "Makefile", "GNUmakefile",
        ".editorconfig", "src/.editorconfig", ".pre-commit-config.yaml",
        ".globalconfig", ".config/dotnet-tools.json",
        "src/Foo/Foo.csproj", "src/Foo/Foo.fsproj", "src/Foo/Foo.vbproj", "tools/x.proj",
        "Ashlar.sln", "Ashlar.slnx",
        ".git/config", ".github/workflows/ci.yml", ".vscode/tasks.json",
        ".devcontainer/devcontainer.json", "scripts/run.sh",
    };

    public static TheoryData<string> BenignPaths() => new()
    {
        "src/Feature/Handler.cs", "docs/guide.md", "README.md", "bricks/b/logic.cs",
        "src/scripts.cs", "notes/global.jsonc", "src/data.csproj.txt",
        "docs/solution.md", "src/editorconfig.cs", "src/propshelper.cs",
    };

    public static TheoryData<string> UnsafePaths() => new()
    {
        "", "   ", ".", "..", "a/../b", "./a", "/rooted", "\\rooted",
        "C:/x", "a:b", "x::$DATA", "a/./b", "trail. /x", "x/con", "nul", "COM1", "a/b.",
    };

    public static TheoryData<string> SafePaths() => new()
    {
        "a", "a/b/c.cs", "src/Feature/Handler.cs", "config/settings.json", "console.cs",
    };

    [Theory]
    [MemberData(nameof(GovernancePaths))]
    public void IsGovernancePath_True(string p) => MediatedWritePath.IsGovernancePath(p).Should().BeTrue(p);

    [Theory]
    [MemberData(nameof(BenignPaths))]
    public void IsGovernancePath_False(string p) => MediatedWritePath.IsGovernancePath(p).Should().BeFalse(p);

    [Theory]
    [MemberData(nameof(UnsafePaths))]
    public void IsSafeRelativePath_False(string p) => MediatedWritePath.IsSafeRelativePath(p).Should().BeFalse(p);

    [Theory]
    [MemberData(nameof(SafePaths))]
    public void IsSafeRelativePath_True(string p) => MediatedWritePath.IsSafeRelativePath(p).Should().BeTrue(p);

    [Fact]
    public void Refuse_AllowsAnOrdinaryContentPath()
        => MediatedWritePath.Refuse(_root, "src/Feature/x.cs").Should().BeNull();

    [Theory]
    [InlineData("ashlar.policy.yaml")]
    [InlineData("./ashlar.policy.yaml")]
    [InlineData("a/../.ashlar/steal.json")]
    [InlineData("Directory.Solution.targets")]
    [InlineData("../outside.txt")]
    public void Refuse_RejectsGovernanceAndEscapes(string target)
        => MediatedWritePath.Refuse(_root, target).Should().NotBeNull();

    [Fact]
    public void Refuse_Allowlist_RejectsOutside_AdmitsInside_AndHonoursEveryEntry()
    {
        MediatedWritePath.Refuse(_root, "docs/x.md", new[] { "src" }).Should().Contain("allowlist");
        MediatedWritePath.Refuse(_root, "src-evil/x.cs", new[] { "src" }).Should().Contain("allowlist"); // prefix sibling
        MediatedWritePath.Refuse(_root, "src/deep/x.cs", new[] { "src" }).Should().BeNull();
        MediatedWritePath.Refuse(_root, "docs/x.md", new[] { "src", "docs" }).Should().BeNull();       // 2nd entry admits
    }

    [Fact]
    public void Refuse_RejectsAWriteThroughALeafSymlink()
    {
        var docs = Path.Combine(_root, "docs");
        Directory.CreateDirectory(docs);
        try
        {
            File.CreateSymbolicLink(Path.Combine(docs, "site.yaml"), Path.Combine("..", "ashlar.policy.yaml"));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or PlatformNotSupportedException)
        {
            return; // platform refuses unprivileged symlinks — nothing to assert
        }
        MediatedWritePath.Refuse(_root, "docs/site.yaml").Should().Contain("symlink");
    }
}
