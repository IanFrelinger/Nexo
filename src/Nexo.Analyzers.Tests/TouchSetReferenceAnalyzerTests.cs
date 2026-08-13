using FluentAssertions;
using Xunit;
using static Nexo.Analyzers.Tests.TrustLoopAnalyzerHarness;

namespace Nexo.Analyzers.Tests;

/// <summary>
/// Triads for the touch-set reference rules (autonomy spec R3.2, analyzer leg): the
/// candidate's RESOLVED reference graph must stay inside the declared touch-set, an
/// undeclared reference into the trust kernel is its own diagnostic, declared kernel
/// namespaces (the Tier-1 flow) are permitted here — admission handles them — and
/// reflection strings are honestly out of scope. The kernel list is a constructor
/// argument, so these tests drive it with a test namespace instead of coupling to the
/// production enumeration (which the gate supplies from <c>TrustKernel</c>).
/// </summary>
public sealed class TouchSetReferenceAnalyzerTests
{
    private static Nexo.Analyzers.TouchSetReferenceAnalyzer Analyzer(
        string[]? declared = null, string[]? kernel = null) => new(
        declared ?? Array.Empty<string>(),
        kernel ?? Array.Empty<string>());

    [Fact]
    public async Task BaselineOnlyCandidate_IsClean_WithAnEmptyDeclaration()
    {
        // System*, threading, tasks, and the Nexo.Core.Domain contracts are the floor
        // every brick stands on — never touch-set violations.
        var source = BrickSource(body: "var t = Task.CompletedTask; output.Set(\"ok\", t.IsCompleted);");

        var diagnostics = await AnalyzeAsync(source, Analyzer());

        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public async Task ReferenceOutsideTheDeclaration_IsFlagged()
    {
        var source = BrickSource(body: "Xunit.Assert.True(true);");

        var diagnostics = await AnalyzeAsync(source, Analyzer(), typeof(Xunit.Assert));

        var finding = diagnostics.Should().ContainSingle(
            d => d.Id == Nexo.Analyzers.TouchSetReferenceAnalyzer.OutsideTouchSetId).Subject;
        finding.GetMessage().Should().Contain("Xunit").And.Contain("does not declare");
    }

    [Fact]
    public async Task DeclaredNamespaces_ArePermitted()
    {
        var source = BrickSource(body: "Xunit.Assert.True(true);");

        var diagnostics = await AnalyzeAsync(
            source, Analyzer(declared: new[] { "Xunit" }), typeof(Xunit.Assert));

        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public async Task UndeclaredKernelReference_GetsTheSmugglingDiagnostic()
    {
        var source = BrickSource(body: "Xunit.Assert.True(true);");

        var diagnostics = await AnalyzeAsync(
            source, Analyzer(kernel: new[] { "Xunit" }), typeof(Xunit.Assert));

        var finding = diagnostics.Should().ContainSingle(
            d => d.Id == Nexo.Analyzers.TouchSetReferenceAnalyzer.UndeclaredKernelReferenceId).Subject;
        finding.GetMessage().Should().Contain("trust-kernel").And.Contain("I-1");
    }

    [Fact]
    public async Task DeclaredKernelNamespaces_AreTheTier1Flow_AndPassTheAnalyzer()
    {
        // A Tier-1 objective legitimately declares kernel surfaces; the analyzer proves
        // conformance to the declaration — the ADMISSION gate is what holds the artifact.
        var source = BrickSource(body: "Xunit.Assert.True(true);");

        var diagnostics = await AnalyzeAsync(
            source,
            Analyzer(declared: new[] { "Xunit" }, kernel: new[] { "Xunit" }),
            typeof(Xunit.Assert));

        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public async Task BaseTypesCount_AsReferences()
    {
        var source = BrickSource(extraMembers: """
            private sealed class SneakyException : Xunit.Sdk.XunitException
            {
                public SneakyException() : base("x") { }
            }
            """);

        var diagnostics = await AnalyzeAsync(source, Analyzer(), typeof(Xunit.Sdk.XunitException));

        diagnostics.Should().Contain(
            d => d.Id == Nexo.Analyzers.TouchSetReferenceAnalyzer.OutsideTouchSetId);
    }

    [Fact]
    public async Task ReflectionStrings_AreHonestlyOutOfScope()
    {
        // The other legs of the R3.2 triple check (swap host, runtime watch) own this;
        // claiming string-based detection here would be a lie.
        var source = BrickSource(body: """
            var name = "Nexo.Policies.PolicyEngine";
            output.Set("t", name);
            """);

        var diagnostics = await AnalyzeAsync(
            source, Analyzer(kernel: new[] { "Nexo.Policies" }));

        diagnostics.Should().BeEmpty();
    }
}
