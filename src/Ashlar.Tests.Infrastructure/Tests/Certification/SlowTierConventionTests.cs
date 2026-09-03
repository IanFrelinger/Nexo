using System.Text.RegularExpressions;
using FluentAssertions;
using Ashlar.Core.Application.Paths;
using Ashlar.Tests.Infrastructure.Helpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit;

namespace Ashlar.Tests.Infrastructure.Tests.Certification;

/// <summary>
/// The cert-gate has a fast tier, and the split between fast and slow cannot rot.
///
/// <para><b>Why this exists.</b> Most of the gate is hermetic: Roslyn in memory, child processes
/// the certifier compiles itself, file reads. A handful of tests instead spawn a REAL
/// <c>dotnet msbuild</c> (through <c>BrickCertificationProjectLoader.LoadAsync</c> and the
/// <c>EvaluatedBrickProject</c> it evaluates) or a shell script, and each of those costs a restore
/// and a build — tens of seconds where the rest cost milliseconds. <c>scripts/run-cert-gate.sh
/// --fast</c> runs everything but that tier for the inner loop; CI stays on the full filter. The
/// split is a Trait, and a Trait is only as reliable as the convention that puts it on every class
/// that needs it: a new loader-driven test without the trait silently makes <c>--fast</c> slow, and
/// nobody notices until they wonder why. This test names the class.</para>
///
/// <para><b>How a class is classified.</b> By its TOKENS, not its text: every <c>*.cs</c> under this
/// assembly's <c>Tests/</c> is parsed with Roslyn and a class is slow-tier when its code (comments
/// and doc comments are trivia and do not count) uses an identifier that reaches a real build, or
/// starts a process and names a <c>scripts/*.sh</c> or a <c>dotnet</c> command line. Reading a
/// script's text — as <c>CertGateFilterCoverageTests</c> reads <c>cert-gate-config.sh</c> — is not
/// running it and is not slow.</para>
///
/// <para>Hermetic: file reads and a parse, no build, no network.</para>
/// </summary>
[Trait("Category", "Certification")]
public sealed class SlowTierConventionTests
{
    /// <summary>The trait value <c>CERT_GATE_FAST_FILTER</c> excludes. Change both or neither.</summary>
    internal const string SlowTierTrait = "SlowTier";

    private const string ConfigScriptRelativePath = "scripts/cert-gate-config.sh";
    private const string RunScriptRelativePath = "scripts/run-cert-gate.sh";
    private const string WorkflowRelativePath = ".github/workflows/cert-gate.yml";
    private const string TestSourcesRelativePath = "src/Ashlar.Tests.Infrastructure/Tests";

    /// <summary>Identifiers whose use means the test shells out to <c>dotnet msbuild</c>.</summary>
    private static readonly string[] RealBuildIdentifiers = ["BrickCertificationProjectLoader", "EvaluatedBrickProject"];

    /// <summary>Identifiers whose use means the class can start a process at all.</summary>
    private static readonly string[] ProcessIdentifiers = ["Process", "ProcessStartInfo"];

    private static readonly Regex ScriptPath = new(@"(^|[\s""'/])scripts/[A-Za-z0-9_./-]+\.sh\b", RegexOptions.CultureInvariant);
    private static readonly Regex DotnetCommand = new(@"^dotnet(\s|$)", RegexOptions.CultureInvariant);

    [Fact(Timeout = TestTimeouts.Integration)]
    public Task Every_test_class_that_reaches_a_real_build_or_a_script_carries_the_slow_tier_trait()
    {
        var classified = ClassifyTestClasses(RepoPathResolver.FindRepoRoot());

        var untagged = classified
            .Where(c => c.Reason is not null && !c.HasSlowTierTrait)
            .Select(c => $"{c.Name} ({c.File}): {c.Reason}")
            .OrderBy(s => s, StringComparer.Ordinal)
            .ToList();

        untagged.Should().BeEmpty(
            "a test that spawns a real dotnet build or a shell script is the slow tier, and "
            + "scripts/run-cert-gate.sh --fast can only skip what is marked. Add "
            + "[Trait(\"Category\", \"{0}\")] beside the class's Certification trait. Untagged: {1}",
            SlowTierTrait, string.Join("; ", untagged));

        return Task.CompletedTask;
    }

    /// <summary>
    /// The scan above passes vacuously if it stops finding anything — a rename of the loader, a
    /// moved Tests directory. The shipped-sample test is the canonical member of the tier.
    /// </summary>
    [Fact(Timeout = TestTimeouts.Integration)]
    public Task The_slow_tier_is_not_empty_and_includes_the_shipped_sample_certification()
    {
        var classified = ClassifyTestClasses(RepoPathResolver.FindRepoRoot());

        classified.Should().NotBeEmpty("the scan must see this assembly's test classes under {0}", TestSourcesRelativePath);
        classified.Where(c => c.Reason is not null).Should().NotBeEmpty(
            "some test in this assembly drives the real loader; finding none means the scan is broken, not that the tier emptied");
        classified.Should().Contain(c => c.Name == "ShippedSampleCertificationTests" && c.Reason != null,
            "the shipped-sample certification loads a tracked project through the real loader and is the tier's canonical member");

        return Task.CompletedTask;
    }

    [Fact(Timeout = TestTimeouts.Quick)]
    public Task The_fast_filter_is_the_full_filter_minus_the_slow_tier()
    {
        var root = RepoPathResolver.FindRepoRoot();
        var config = ReadRepoFile(root, ConfigScriptRelativePath);

        // Composed from CERT_GATE_FILTER, never restated: a namespace added to the gate is in the
        // fast tier too, and the only thing --fast removes is the trait.
        config.Should().MatchRegex(
            @"CERT_GATE_FAST_FILTER=[""']?\(\$\{CERT_GATE_FILTER\}\)&Category!=" + Regex.Escape(SlowTierTrait) + @"[""']?[ \t]*(\r?\n|$)",
            "{0} must define CERT_GATE_FAST_FILTER as the full filter with Category!={1} subtracted, and nothing else",
            ConfigScriptRelativePath, SlowTierTrait);

        var run = ReadRepoFile(root, RunScriptRelativePath);
        run.Should().Contain("--fast", "{0} accepts --fast to run the fast tier", RunScriptRelativePath);
        run.Should().Contain("CERT_GATE_FAST_FILTER", "{0} --fast selects by CERT_GATE_FAST_FILTER", RunScriptRelativePath);

        return Task.CompletedTask;
    }

    [Fact(Timeout = TestTimeouts.Quick)]
    public Task CI_runs_the_full_gate_never_the_fast_tier()
    {
        var workflow = ReadRepoFile(RepoPathResolver.FindRepoRoot(), WorkflowRelativePath);

        workflow.Should().Contain("scripts/run-cert-gate.sh", "the required check runs the gate script");
        workflow.Should().NotContain("--fast",
            "--fast is a developer inner-loop tier; the merge-blocking check must run every test in the filter");

        return Task.CompletedTask;
    }

    private sealed record ClassifiedClass(string Name, string File, string? Reason, bool HasSlowTierTrait);

    /// <summary>
    /// Every top-level class under the assembly's <c>Tests/</c> sources that the gate selects, with
    /// the reason it belongs to the slow tier (or <c>null</c>) and whether it carries the trait.
    /// The tier is a property of the gate: a class outside <c>CERT_GATE_FILTER</c> is run by another
    /// gate with its own budget, and holding it to this split would fail the required check for a
    /// reason its own runner cannot see. The selectors are read from the script, not restated.
    /// </summary>
    private static List<ClassifiedClass> ClassifyTestClasses(string root)
    {
        var sources = Path.Combine(root, TestSourcesRelativePath.Replace('/', Path.DirectorySeparatorChar));
        Directory.Exists(sources).Should().BeTrue("{0} is where this assembly's tests live", TestSourcesRelativePath);

        var selectors = Regex.Matches(ReadRepoFile(root, ConfigScriptRelativePath), @"FullyQualifiedName~([A-Za-z0-9_.]+)")
            .Select(m => m.Groups[1].Value)
            .Distinct(StringComparer.Ordinal)
            .ToList();
        selectors.Should().NotBeEmpty("{0} defines what the gate runs; an empty parse would make this scan vacuous", ConfigScriptRelativePath);

        var result = new List<ClassifiedClass>();
        foreach (var file in Directory.EnumerateFiles(sources, "*.cs", SearchOption.AllDirectories))
        {
            var tree = CSharpSyntaxTree.ParseText(File.ReadAllText(file), path: file);
            var relative = Path.GetRelativePath(root, file).Replace('\\', '/');

            foreach (var declaration in tree.GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>())
            {
                // A nested class's tokens are its enclosing class's tokens; the trait sits on the outer one.
                if (declaration.Parent is TypeDeclarationSyntax)
                    continue;

                var ns = declaration.Ancestors().OfType<BaseNamespaceDeclarationSyntax>().FirstOrDefault()?.Name.ToString();
                var fullName = ns is null ? declaration.Identifier.ValueText : ns + "." + declaration.Identifier.ValueText;
                if (!selectors.Any(s => fullName.Contains(s, StringComparison.Ordinal)))
                    continue;

                result.Add(new ClassifiedClass(
                    declaration.Identifier.ValueText, relative, SlowTierReason(declaration), HasSlowTierTrait(declaration)));
            }
        }

        return result;
    }

    /// <summary>
    /// Why the class is slow-tier, or <c>null</c>. Tokens only: <c>DescendantTokens()</c> does not
    /// descend into trivia, so a <c>&lt;see cref="BrickCertificationProjectLoader"/&gt;</c> in a doc
    /// comment or a script named in a <c>//</c> remark does not classify.
    /// </summary>
    private static string? SlowTierReason(ClassDeclarationSyntax declaration)
    {
        var tokens = declaration.DescendantTokens().ToList();
        var identifiers = tokens
            .Where(t => t.IsKind(SyntaxKind.IdentifierToken))
            .Select(t => t.ValueText)
            .ToHashSet(StringComparer.Ordinal);

        var build = RealBuildIdentifiers.FirstOrDefault(identifiers.Contains);
        if (build is not null)
            return $"uses {build}, which shells out to dotnet msbuild";

        if (!ProcessIdentifiers.Any(identifiers.Contains))
            return null;

        var literals = tokens
            .Where(t => t.IsKind(SyntaxKind.StringLiteralToken)
                || t.IsKind(SyntaxKind.InterpolatedStringTextToken)
                || t.IsKind(SyntaxKind.SingleLineRawStringLiteralToken)
                || t.IsKind(SyntaxKind.MultiLineRawStringLiteralToken))
            .Select(t => t.ValueText)
            .ToList();

        var script = literals.FirstOrDefault(ScriptPath.IsMatch);
        if (script is not null)
            return $"starts a process and names a script ({script.Trim()})";

        if (literals.Any(l => DotnetCommand.IsMatch(l.Trim())))
            return "starts a process and names a dotnet command line";

        return null;
    }

    private static bool HasSlowTierTrait(ClassDeclarationSyntax declaration) =>
        declaration.AttributeLists
            .SelectMany(list => list.Attributes)
            .Where(a => a.Name.ToString() is "Trait" or "TraitAttribute" or "Xunit.Trait")
            .Select(a => a.ArgumentList?.Arguments.Select(arg => arg.Expression).OfType<LiteralExpressionSyntax>().Select(l => l.Token.ValueText).ToList())
            .Any(args => args is { Count: 2 } && args[0] == "Category" && args[1] == SlowTierTrait);

    private static string ReadRepoFile(string root, string relativePath)
    {
        var path = Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar));
        File.Exists(path).Should().BeTrue("{0} is part of the cert-gate contract this test enforces", relativePath);
        return File.ReadAllText(path);
    }
}
