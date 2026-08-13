using FluentAssertions;
using Nexo.Core.Domain.Bricks.Ports;
using Xunit;
using static Nexo.Analyzers.Tests.TrustLoopAnalyzerHarness;

namespace Nexo.Analyzers.Tests;

/// <summary>
/// Triads for the manifest-derived rules (extension spec A2): each rule proves a true positive,
/// a true negative, and the honesty case. The A2.2 payoff cases prove the rules are
/// symbol-resolved, not textual: aliasing and full qualification cannot dodge them, while
/// tokens inside strings and comments never trip them. Every violation message must restate
/// the manifest instruction verbatim from the SAME manifest instance (A2.3/A3.2).
/// </summary>
public sealed class BrickConstraintManifestAnalyzerTests
{
    /// <summary>The using directives <see cref="TrustLoopAnalyzerHarness.BrickSource"/> always declares.</summary>
    private static readonly string[] HarnessUsings =
    {
        "System",
        "System.Threading",
        "System.Threading.Tasks",
        "Nexo.Core.Domain.Bricks",
        "Nexo.Core.Domain.Execution",
    };

    // --- NEXO0010: using allowlist -------------------------------------------------------

    [Fact]
    public async Task Using_outside_the_allowlist_is_flagged_with_the_instruction_verbatim()
    {
        var manifest = new BrickConstraintManifest { AllowedUsings = HarnessUsings };
        var source = BrickSource(extraUsings: "using System.Text;");

        var diagnostics = await AnalyzeAsync(source, new BrickConstraintManifestAnalyzer(manifest));

        var finding = diagnostics.Should().ContainSingle(d => d.Id == BrickConstraintManifestAnalyzer.DisallowedUsingId).Subject;
        finding.GetMessage().Should().Contain("using System.Text;")
            .And.Contain(manifest.UsingsInstruction(),
                "the violation must restate the constraint verbatim from the same manifest instance (A2.3)");
    }

    [Fact]
    public async Task Allowlisted_usings_are_clean()
    {
        var manifest = new BrickConstraintManifest { AllowedUsings = HarnessUsings };

        var diagnostics = await AnalyzeAsync(BrickSource(), new BrickConstraintManifestAnalyzer(manifest));

        diagnostics.Should().NotContain(d => d.Id == BrickConstraintManifestAnalyzer.DisallowedUsingId);
    }

    [Fact]
    public async Task Empty_allowlist_means_usings_are_unrestricted()
    {
        var manifest = new BrickConstraintManifest { ForbiddenApiTokens = new[] { "Guid.NewGuid" } };
        var source = BrickSource(extraUsings: "using System.Text;");

        var diagnostics = await AnalyzeAsync(source, new BrickConstraintManifestAnalyzer(manifest));

        diagnostics.Should().NotContain(d => d.Id == BrickConstraintManifestAnalyzer.DisallowedUsingId);
    }

    // --- NEXO0011: forbidden API tokens --------------------------------------------------

    [Fact]
    public async Task TypeMember_token_flags_the_resolved_member()
    {
        var manifest = new BrickConstraintManifest { ForbiddenApiTokens = new[] { "Guid.NewGuid" } };
        var source = BrickSource(body: "var id = Guid.NewGuid();");

        var diagnostics = await AnalyzeAsync(source, new BrickConstraintManifestAnalyzer(manifest));

        var finding = diagnostics.Should().ContainSingle(d => d.Id == BrickConstraintManifestAnalyzer.ForbiddenApiId).Subject;
        finding.GetMessage().Should().Contain("'Guid.NewGuid'")
            .And.Contain(manifest.ForbiddenApiInstruction("Guid.NewGuid"));
    }

    [Fact]
    public async Task Bare_type_token_bans_creations_of_the_type_even_when_seeded()
    {
        // Distinct from NEXO0007: the manifest token bans the TYPE, so even a deterministic
        // seeded Random is a manifest violation for this brick class.
        var manifest = new BrickConstraintManifest { ForbiddenApiTokens = new[] { "Random" } };
        var source = BrickSource(body: "var r = new Random(42); output.Set(\"n\", r.Next());");

        var diagnostics = await AnalyzeAsync(source, new BrickConstraintManifestAnalyzer(manifest));

        diagnostics.Should().Contain(d => d.Id == BrickConstraintManifestAnalyzer.ForbiddenApiId);
    }

    [Fact]
    public async Task Alias_cannot_dodge_a_token_because_matching_is_symbol_resolved()
    {
        // The A2.2 payoff: the generation-side textual validator cannot see through an alias;
        // the analyzer resolves the symbol and catches it anyway.
        var manifest = new BrickConstraintManifest { ForbiddenApiTokens = new[] { "DateTime.Now" } };
        var source = BrickSource(
            extraUsings: "using D = System.DateTime;",
            body: "var t = D.Now; output.Set(\"t\", t.ToString(\"O\"));");

        var diagnostics = await AnalyzeAsync(source, new BrickConstraintManifestAnalyzer(manifest));

        diagnostics.Should().Contain(d => d.Id == BrickConstraintManifestAnalyzer.ForbiddenApiId);
    }

    [Fact]
    public async Task Tokens_inside_strings_and_comments_and_different_members_stay_clean()
    {
        var manifest = new BrickConstraintManifest { ForbiddenApiTokens = new[] { "DateTime.Now" } };
        var source = BrickSource(body: """
            // DateTime.Now would be nondeterministic here.
            var label = "DateTime.Now";
            var t = DateTime.UtcNow;
            output.Set("t", label + t.ToString("O"));
            """);

        var diagnostics = await AnalyzeAsync(source, new BrickConstraintManifestAnalyzer(manifest));

        diagnostics.Should().NotContain(d => d.Id == BrickConstraintManifestAnalyzer.ForbiddenApiId,
            "matching is symbol-based: strings, comments, and DateTime.UtcNow must not trip DateTime.Now");
    }

    // --- NEXO0012: forbidden namespaces --------------------------------------------------

    [Fact]
    public async Task FullyQualified_reference_into_a_forbidden_namespace_is_flagged()
    {
        // No using directive at all — full qualification would dodge any directive-based check.
        var manifest = new BrickConstraintManifest { ForbiddenNamespaces = new[] { "System.IO" } };
        var source = BrickSource(body: "var p = System.IO.Path.Combine(\"a\", \"b\"); output.Set(\"p\", p);");

        var diagnostics = await AnalyzeAsync(
            source, new BrickConstraintManifestAnalyzer(manifest), typeof(System.IO.Path));

        var finding = diagnostics.Should().ContainSingle(d => d.Id == BrickConstraintManifestAnalyzer.ForbiddenNamespaceId).Subject;
        finding.GetMessage().Should().Contain("System.IO")
            .And.Contain(manifest.ForbiddenNamespaceInstruction("System.IO"));
    }

    [Fact]
    public async Task SubNamespace_references_are_inside_the_forbidden_parent()
    {
        var manifest = new BrickConstraintManifest { ForbiddenNamespaces = new[] { "System.Net" } };
        var source = BrickSource(body: "using var c = new System.Net.Sockets.TcpClient();");

        var diagnostics = await AnalyzeAsync(
            source, new BrickConstraintManifestAnalyzer(manifest), typeof(System.Net.Sockets.TcpClient));

        diagnostics.Should().Contain(d => d.Id == BrickConstraintManifestAnalyzer.ForbiddenNamespaceId);
    }

    [Fact]
    public async Task Using_directive_opening_a_forbidden_namespace_is_flagged()
    {
        var manifest = new BrickConstraintManifest { ForbiddenNamespaces = new[] { "System.IO" } };
        var source = BrickSource(extraUsings: "using System.IO;");

        var diagnostics = await AnalyzeAsync(
            source, new BrickConstraintManifestAnalyzer(manifest), typeof(System.IO.Path));

        diagnostics.Should().Contain(d => d.Id == BrickConstraintManifestAnalyzer.ForbiddenNamespaceId);
    }

    [Fact]
    public async Task Unrelated_namespaces_stay_clean()
    {
        var manifest = new BrickConstraintManifest { ForbiddenNamespaces = new[] { "System.IO", "System.Net" } };
        var source = BrickSource(body: "var t = Task.CompletedTask; output.Set(\"done\", t.IsCompleted);");

        var diagnostics = await AnalyzeAsync(source, new BrickConstraintManifestAnalyzer(manifest));

        diagnostics.Should().BeEmpty();
    }

    // --- honesty: what the analyzer deliberately does not claim --------------------------

    [Fact]
    public async Task A_declared_but_never_dereferenced_type_is_not_flagged()
    {
        // Documented limit: a Random? local that stays null creates no operation the analyzer
        // inspects. The generation-side textual validator still catches the token; claiming
        // semantic detection here would be a lie.
        var manifest = new BrickConstraintManifest { ForbiddenApiTokens = new[] { "Random" } };
        var source = BrickSource(body: "Random r = null; output.Set(\"has\", r == null);");

        var diagnostics = await AnalyzeAsync(source, new BrickConstraintManifestAnalyzer(manifest));

        diagnostics.Should().NotContain(d => d.Id == BrickConstraintManifestAnalyzer.ForbiddenApiId);
    }
}
