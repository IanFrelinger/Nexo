using FluentAssertions;
using Microsoft.CodeAnalysis;
using Xunit;
using static Nexo.Analyzers.Tests.TrustLoopAnalyzerHarness;

namespace Nexo.Analyzers.Tests;

/// <summary>
/// NEXO0009 triad: empty catch blocks in brick code must fire; a catch that does anything must
/// not; catches outside brick types are out of scope by design.
/// </summary>
public sealed class BrickEmptyCatchAnalyzerTests
{
    private static Task<IReadOnlyList<Diagnostic>> RunAsync(string source)
        => AnalyzeAsync(source, new BrickEmptyCatchAnalyzer());

    // ---------------------------------------------------------- must fire

    [Fact]
    public async Task Empty_catch_in_ExecuteAsync_is_flagged()
    {
        var diagnostics = await RunAsync(BrickSource(body: """
            try { output.Set("v", 1); }
            catch (Exception) { }
            """));

        var diagnostic = diagnostics.Should().ContainSingle().Subject;
        diagnostic.Id.Should().Be(BrickEmptyCatchAnalyzer.EmptyCatchId);
        diagnostic.GetMessage().Should().Contain("SampleBrick").And.Contain("silent");
    }

    [Fact]
    public async Task Empty_catch_containing_only_a_comment_is_still_flagged()
    {
        var diagnostics = await RunAsync(BrickSource(body: """
            try { output.Set("v", 1); }
            catch { /* nothing to do */ }
            """));

        diagnostics.Should().ContainSingle().Which.Id.Should().Be(BrickEmptyCatchAnalyzer.EmptyCatchId);
    }

    // ------------------------------------------------------- must not fire

    [Fact]
    public async Task Catch_that_records_the_failure_is_not_flagged()
    {
        var diagnostics = await RunAsync(BrickSource(body: """
            try { output.Set("v", 1); }
            catch (Exception ex) { output.Set("error", ex.Message); }
            """));

        diagnostics.Should().BeEmpty("recording the failure into the output is an explained failure");
    }

    [Fact]
    public async Task Catch_that_rethrows_is_not_flagged()
    {
        var diagnostics = await RunAsync(BrickSource(body: """
            try { output.Set("v", 1); }
            catch { throw; }
            """));

        diagnostics.Should().BeEmpty();
    }

    // -------------------------------------- deliberately out of scope: silent

    [Fact]
    public async Task Empty_catch_in_a_non_brick_type_is_left_alone()
    {
        const string source = """
            using System;

            public static class NotABrick
            {
                public static void Swallow()
                {
                    try { Console.WriteLine("x"); }
                    catch { }
                }
            }
            """;

        (await RunAsync(source)).Should().BeEmpty("the rule is scoped to brick execution paths");
    }
}
