using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Ashlar.Core.Application.Adaptation.Models;
using Ashlar.Core.Domain.Bricks;
using Ashlar.Core.Domain.Execution;
using Ashlar.Infrastructure.Adaptation.Generation;
using Ashlar.Infrastructure.Certification;
using Ashlar.Infrastructure.Testing.CodeAnalysis;
using Ashlar.Tests.Infrastructure.Certification.Fixtures;
using Xunit;

namespace Ashlar.Tests.Infrastructure.Tests.Certification;

/// <summary>
/// The operator family of the mutation catalog: arithmetic, compound-assignment, relational
/// boundary, unary and logical-not mutants. Two properties matter and both are pinned here.
///
/// <para>SOUNDNESS: every operator mutant must compile. The engine scores a non-compiling mutant
/// as killed, so an operator swap that ignores types (<c>"a" + "b"</c> → <c>"a" - "b"</c>) would
/// inflate the kill count of a signed certificate with mutants that were dead on arrival. The
/// catalog consults the semantic model, and <see cref="EveryOperatorMutant_Compiles"/> compiles
/// every mutant of a brick that exercises each operand type the rules reason about.</para>
///
/// <para>STABILITY: adding operator kinds must not renumber or rename the mutants the pre-existing
/// kinds produced, or certification records signed before the change stop being reproducible.</para>
/// </summary>
[Trait("Category", "Certification")]
public sealed class OperatorMutationCatalogTests
{
    private static readonly string[] OperatorKinds =
    [
        "swap-arithmetic-op",
        "swap-arithmetic-assign",
        "shift-relational-boundary",
        "swap-unary-op",
        "remove-logical-not",
    ];

    private static readonly IReadOnlyList<string> BrickReferences =
    [
        typeof(DomainBrick).Assembly.Location,
        typeof(BrickInput).Assembly.Location,
    ];

    [Fact]
    public void ContradictoryBrick_GainsExactlyTheArithmeticMutant_AndKeepsEveryPreExistingId()
    {
        var mutations = AstMutationCatalog.CollectMutations(ContradictoryDamageBrickSource.Subtracting, BrickReferences);
        var ids = mutations.Select(m => m.Id).ToArray();

        // The five ids the pre-fix catalog produced for this exact source, verbatim from the run
        // that reproduced the defect. They must survive unchanged: a record signed against them
        // is only reproducible if the same edit still carries the same name.
        ids.Should().Contain(
        [
            "mutate-int-literal-33",
            "mutate-string-literal-31",
            "mutate-string-literal-32",
            "mutate-string-literal-35",
            "remove-statement-31",
        ]);

        var arithmetic = mutations.Should().ContainSingle(m => m.Id.StartsWith("swap-arithmetic-op", StringComparison.Ordinal)).Subject;
        arithmetic.Id.Should().Be("swap-arithmetic-op-33");
        arithmetic.Site.OriginalText.Should().Be("-");
        arithmetic.Site.MutatedText.Should().Be("+");
        arithmetic.Site.LineText.Should().Contain("Math.Max(0, baseDamage - armor)");
        arithmetic.ToSource().Should().Contain("baseDamage + armor").And.NotContain("baseDamage - armor");

        // Nothing else in this brick is an operator site, so the operator family adds exactly one.
        ids.Where(id => OperatorKinds.Any(kind => id.StartsWith(kind, StringComparison.Ordinal)))
            .Should().ContainSingle();
    }

    [Fact]
    public void AddingBrick_GetsTheMirrorMutant()
    {
        var mutations = AstMutationCatalog.CollectMutations(ContradictoryDamageBrickSource.Adding, BrickReferences);

        var arithmetic = mutations.Should().ContainSingle(m => m.Id.StartsWith("swap-arithmetic-op", StringComparison.Ordinal)).Subject;
        arithmetic.Site.OriginalText.Should().Be("+");
        arithmetic.Site.MutatedText.Should().Be("-");
    }

    [Fact]
    public void OperatorMutants_NeedTheOperandTypes_SoTheEngineMustPassItsReferences()
    {
        // input.Get<int>(...) lives in Ashlar.Brick.Contracts. Without that reference the operand
        // types are error types and the catalog emits NO arithmetic mutant rather than a guess —
        // which is exactly why the engine hands its compilation references to the catalog.
        var blind = AstMutationCatalog.CollectMutations(ContradictoryDamageBrickSource.Subtracting);
        var sighted = AstMutationCatalog.CollectMutations(ContradictoryDamageBrickSource.Subtracting, BrickReferences);

        blind.Should().NotContain(m => m.Id.StartsWith("swap-arithmetic-op", StringComparison.Ordinal),
            "an unresolvable operand type must produce no operator mutant, not a non-compiling one");
        sighted.Should().Contain(m => m.Id.StartsWith("swap-arithmetic-op", StringComparison.Ordinal));
    }

    [Fact]
    public void StringConcatenation_EnumOffsets_AndUserDefinedOperators_AreNeverSwapped()
    {
        const string source = """
            public sealed class NotArithmeticBrick
            {
                private enum Phase { Warmup, Active, Done }

                public string ExecuteAsync(string a, string b, int n, System.DateTime at)
                {
                    var joined = a + b;
                    var tagged = a + n;
                    joined += b;
                    var phase = Phase.Warmup + 1;
                    var elapsed = at - System.DateTime.UnixEpoch;
                    var later = at + System.TimeSpan.FromSeconds(n);
                    var early = elapsed < System.TimeSpan.FromDays(1);
                    var ordered = later >= System.DateTime.UnixEpoch;
                    return joined + tagged + phase + early + ordered;
                }
            }
            """;

        var mutations = AstMutationCatalog.CollectMutations(source);

        mutations.Should().NotContain(m => m.Id.StartsWith("swap-arithmetic", StringComparison.Ordinal),
            "string concatenation, enum offsets and DateTime/TimeSpan arithmetic have no swappable operator");
        mutations.Should().NotContain(m => m.Id.StartsWith("shift-relational-boundary", StringComparison.Ordinal),
            "TimeSpan/DateTime comparisons are user-defined operators; the shifted operator is not guaranteed to exist");
    }

    [Fact]
    public void EachOperatorKind_IsEmitted_WithTheEditItDescribes()
    {
        const string source = """
            public sealed class OperatorShapesBrick
            {
                public int ExecuteAsync(int count, int limit, bool flag)
                {
                    var total = 0;
                    if (count < limit)
                        total += count;
                    var negative = -total;
                    if (!flag)
                        total--;
                    total++;
                    return total + negative;
                }
            }
            """;

        var mutations = AstMutationCatalog.CollectMutations(source);
        string[] Edits(string kind) => mutations
            .Where(m => m.Id.StartsWith(kind, StringComparison.Ordinal))
            .Select(m => $"{m.Site.OriginalText} -> {m.Site.MutatedText}")
            .ToArray();

        Edits("shift-relational-boundary").Should().Equal("< -> <=");
        Edits("swap-arithmetic-assign").Should().Equal("+= -> -=");
        Edits("swap-arithmetic-op").Should().Equal("+ -> -");
        Edits("swap-unary-op").Should().Equal("- -> +", "-- -> ++", "++ -> --");
        Edits("remove-logical-not").Should().Equal("!flag -> flag");
    }

    [Fact]
    public void ConstantExpressions_AndLoopControlSteps_AreLeftAlone()
    {
        const string source = """
            public sealed class HazardsBrick
            {
                public int ExecuteAsync(int n)
                {
                    const int k = 1 + 2;
                    var zero = n * 0;
                    var minValue = -2147483648;
                    var minusOne = -1;
                    var total = 0;
                    for (var i = 0; i < n; i++)
                        total += i;
                    var j = n;
                    while (j > 0)
                    {
                        j -= 1;
                        total = total + j;
                    }
                    return k + zero + minValue + minusOne + total;
                }
            }
            """;

        var mutations = AstMutationCatalog.CollectMutations(source);
        var operatorMutants = mutations
            .Where(m => OperatorKinds.Any(kind => m.Id.StartsWith(kind, StringComparison.Ordinal)))
            .Select(m => $"{m.Id}: {m.Site.OriginalText} -> {m.Site.MutatedText} in {m.Site.LineText}")
            .ToArray();

        operatorMutants.Should().NotContain(s => s.Contains("1 + 2", StringComparison.Ordinal),
            "a constant expression folds at compile time, and a swapped constant can be a compile error");
        operatorMutants.Should().NotContain(s => s.Contains("n * 0", StringComparison.Ordinal) && s.Contains("* -> /", StringComparison.Ordinal),
            "n / 0 is a constant-zero divisor");
        operatorMutants.Should().NotContain(s => s.Contains("swap-unary-op", StringComparison.Ordinal) && (s.Contains("2147483648", StringComparison.Ordinal) || s.Contains("-1;", StringComparison.Ordinal)),
            "sign flips on literals belong to mutate-int-literal, and +2147483648 does not fit an int");
        operatorMutants.Should().NotContain(s => s.Contains("swap-unary-op", StringComparison.Ordinal) && s.Contains("i++", StringComparison.Ordinal),
            "reversing the step of a for loop whose condition reads i never terminates");
        operatorMutants.Should().NotContain(s => s.Contains("j -= 1", StringComparison.Ordinal),
            "j is read by the enclosing while condition, so j += 1 never terminates");
        operatorMutants.Should().Contain(s => s.Contains("total += i", StringComparison.Ordinal),
            "total is not read by the for condition, so its step can be reversed safely");
        operatorMutants.Should().Contain(s => s.Contains("total = total + j", StringComparison.Ordinal),
            "total is not read by the while condition either");
        operatorMutants.Should().Contain(s => s.Contains("shift-relational-boundary", StringComparison.Ordinal) && s.Contains("i < n", StringComparison.Ordinal),
            "shifting a loop bound by one still terminates, so it is a legitimate boundary mutant");
    }

    [Fact]
    public void Ids_AreDeterministic_AndCollisionsOnOneLineAreDisambiguated()
    {
        const string source = """
            public sealed class TwoOnOneLineBrick
            {
                public int ExecuteAsync(int a, int b, int c) => Helper(a, b, c);

                private int Helper(int a, int b, int c)
                {
                    var v = a - b + c;
                    return v;
                }
            }
            """;

        var first = AstMutationCatalog.CollectMutations(source).Select(m => m.Id).ToArray();
        var second = AstMutationCatalog.CollectMutations(source).Select(m => m.Id).ToArray();

        second.Should().Equal(first, "the same source must always yield the same ids, or records are not reproducible");
        first.Where(id => id.StartsWith("swap-arithmetic-op", StringComparison.Ordinal))
            .Should().Equal("swap-arithmetic-op-7", "swap-arithmetic-op-7#2");
    }

    [Fact]
    public void StrategyNames_NameEveryKindTheCatalogEmits()
    {
        var names = new BrickMutationEngine().GetMutationStrategyNames();
        var emitted = AstMutationCatalog.CollectMutations(OperatorZooBrickSource.Code, BrickReferences)
            .Select(m => m.Id[..m.Id.LastIndexOf('-')])
            .Distinct();

        emitted.Should().BeSubsetOf(names, "a kind the engine cannot name is a kind an operator cannot look up");
        names.Should().Contain(OperatorKinds);
    }

    /// <summary>
    /// The soundness contract, executed: every operator mutant of a brick that mixes ints, doubles,
    /// decimals, nullable ints, chars, enums, strings, DateTime/TimeSpan, bool and bool? compiles
    /// against the same wrap and references the certification compile uses. Statement removal is
    /// exempt — dropping a declaration legitimately breaks later uses and always has.
    /// </summary>
    [Theory]
    [InlineData("zoo")]
    [InlineData("contradictory")]
    [InlineData("probe")]
    [InlineData("damage-resolver")]
    [InlineData("line-substring-counter")]
    public async Task EveryOperatorMutant_Compiles(string which)
    {
        var source = which switch
        {
            "zoo" => OperatorZooBrickSource.Code,
            "contradictory" => ContradictoryDamageBrickSource.Subtracting,
            "probe" => MutationProbeBrickSource.Code,
            "damage-resolver" => DamageResolverSources.Honest(new WitnessSignature("damage-resolver", [], [])),
            _ => LineSubstringCounterSources.Correct(new WitnessSignature("line-substring-counter", [], [])),
        };

        var mutations = AstMutationCatalog.CollectMutations(source, BrickReferences);
        var candidates = mutations.Where(m => !m.Id.StartsWith("remove-statement", StringComparison.Ordinal)).ToArray();
        candidates.Should().NotBeEmpty();
        if (which == "zoo")
        {
            candidates.Select(m => m.Id[..m.Id.LastIndexOf('-')]).Distinct()
                .Should().Contain(OperatorKinds, "the zoo must exercise every operator kind, or this test proves nothing about it");
        }

        // The original must compile, or a failing mutant would prove nothing.
        (await CompileAsync(source)).Should().BeEmpty("the fixture itself must compile: {0}", which);

        var failures = new List<string>();
        foreach (var mutation in candidates)
        {
            var errors = await CompileAsync(mutation.ToSource());
            if (errors.Count > 0)
                failures.Add($"{mutation.Id}: {mutation.Site.OriginalText} -> {mutation.Site.MutatedText} in '{mutation.Site.LineText}': {string.Join(" | ", errors)}");
        }

        failures.Should().BeEmpty(
            "every operator mutant must compile — a non-compiling mutant is scored as a kill the witness never earned");
    }

    private static async Task<IReadOnlyList<string>> CompileAsync(string candidateSource)
    {
        var dir = Path.Combine(Path.GetTempPath(), "ashlar-op-catalog", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            var name = $"OpCatalog_{Guid.NewGuid():N}";
            var result = await new RoslynCodeAnalysisService(NullLogger<RoslynCodeAnalysisService>.Instance).CompileAsync(
                CandidateSourceWrapper.Wrap(candidateSource),
                name,
                Path.Combine(dir, name + ".dll"),
                BrickReferences);
            return result.Success ? [] : result.Errors.ToArray();
        }
        finally
        {
            try { Directory.Delete(dir, recursive: true); } catch { /* best effort */ }
        }
    }
}
