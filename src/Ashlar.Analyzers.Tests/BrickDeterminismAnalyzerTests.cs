using FluentAssertions;
using Microsoft.CodeAnalysis;
using Xunit;
using static Ashlar.Analyzers.Tests.TrustLoopAnalyzerHarness;

namespace Ashlar.Analyzers.Tests;

/// <summary>
/// ASHLAR0006/0007/0008 triads: wall-clock reads, unseeded randomness, and mutable statics must
/// fire; their deterministic counterparts must not; and mutability the analyzer cannot resolve
/// (readonly containers) must stay silent by design.
/// </summary>
public sealed class BrickDeterminismAnalyzerTests
{
    private static Task<IReadOnlyList<Diagnostic>> RunAsync(string source)
        => AnalyzeAsync(source, new BrickDeterminismAnalyzer());

    // ---------------------------------------------------------- must fire

    [Fact]
    public async Task DateTime_Now_is_flagged_with_the_utc_fix_target()
    {
        var diagnostics = await RunAsync(BrickSource(body: """output.Set("t", DateTime.Now.ToString());"""));

        var diagnostic = diagnostics.Should().ContainSingle().Subject;
        diagnostic.Id.Should().Be(BrickDeterminismAnalyzer.WallClockId);
        diagnostic.GetMessage().Should().Contain("DateTime.Now").And.Contain("DateTime.UtcNow",
            "the message must name the fix target");
    }

    [Fact]
    public async Task DateTimeOffset_Now_is_flagged()
    {
        var diagnostics = await RunAsync(BrickSource(body: """output.Set("t", DateTimeOffset.Now.ToString());"""));

        diagnostics.Should().ContainSingle().Which.Id.Should().Be(BrickDeterminismAnalyzer.WallClockId);
    }

    [Fact]
    public async Task Unseeded_random_construction_is_flagged()
    {
        var diagnostics = await RunAsync(BrickSource(body: """output.Set("n", new Random().Next());"""));

        diagnostics.Should().ContainSingle().Which.Id.Should().Be(BrickDeterminismAnalyzer.UnseededRandomId);
    }

    [Fact]
    public async Task Random_Shared_is_flagged()
    {
        var diagnostics = await RunAsync(BrickSource(body: """output.Set("n", Random.Shared.Next());"""));

        diagnostics.Should().ContainSingle().Which.Id.Should().Be(BrickDeterminismAnalyzer.UnseededRandomId);
    }

    [Fact]
    public async Task Mutable_static_field_is_flagged()
    {
        var diagnostics = await RunAsync(BrickSource(extraMembers: "private static int _executionCount;"));

        var diagnostic = diagnostics.Should().ContainSingle().Subject;
        diagnostic.Id.Should().Be(BrickDeterminismAnalyzer.MutableStaticId);
        diagnostic.GetMessage().Should().Contain("_executionCount");
    }

    // ------------------------------------------------------- must not fire

    [Fact]
    public async Task UtcNow_seeded_random_and_immutable_statics_are_clean()
    {
        var diagnostics = await RunAsync(BrickSource(
            body: """
                output.Set("t", DateTime.UtcNow.ToString());
                output.Set("n", new Random(42).Next());
                """,
            extraMembers: """
                private const int Limit = 10;
                private static readonly string Name = "sample";
                private int _instanceState;
                """));

        diagnostics.Should().BeEmpty(
            "UtcNow, seeded randomness, const, static readonly, and instance state are all deterministic-legal");
    }

    // -------------------------------------- deliberately unresolvable: silent

    [Fact]
    public async Task Readonly_mutable_container_is_left_alone()
    {
        var diagnostics = await RunAsync(BrickSource(
            extraMembers: "private static readonly System.Collections.Generic.List<string> Cache = new();"));

        diagnostics.Should().BeEmpty(
            "whether a readonly container is mutated cannot be resolved statically without "
            + "call-graph guessing, and this catalog never guesses");
    }

    [Fact]
    public async Task Wall_clock_in_a_non_brick_type_is_left_alone()
    {
        const string source = """
            using System;

            public static class NotABrick
            {
                public static string Stamp() => DateTime.Now.ToString();
            }
            """;

        (await RunAsync(source)).Should().BeEmpty("the rule is scoped to brick code");
    }
}
