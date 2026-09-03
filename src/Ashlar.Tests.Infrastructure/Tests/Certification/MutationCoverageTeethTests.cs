using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Ashlar.Core.Application.Certification.Models;
using Ashlar.Core.Domain.Bricks;
using Ashlar.Core.Domain.Execution;
using Ashlar.Infrastructure.Certification;
using Ashlar.Infrastructure.Testing.CodeAnalysis;
using Ashlar.Tests.Infrastructure.Certification.Fixtures;
using Ashlar.Tests.Infrastructure.Certification.Reuse;
using Ashlar.Tests.Infrastructure.Helpers;
using Xunit;

namespace Ashlar.Tests.Infrastructure.Tests.Certification;

/// <summary>
/// Mutation coverage must not stop silently. Two ways it did, both found by adversarial review and
/// both reproduced by contradictory bricks that certified TOGETHER against one witness:
///
/// <para>(a) A per-kind cap of four qualifying sites, in document order. A brick with six
/// arithmetic sites had its fifth and sixth never mutated, so a witness blind to those two sites
/// still scored <c>escape_rate=0</c>, and nothing in the record said the leg had stopped early.</para>
///
/// <para>(b) A scope of <c>ExecuteAsync</c> plus PRIVATE INSTANCE methods. Arithmetic in a
/// <c>private static</c>, <c>internal</c> or <c>public</c> helper was never mutated at all.</para>
///
/// <para>Alongside those, two ways the kill count was inflated with kills the witness never earned:
/// a mutated lookup key (<c>input.Get("baseDamagX")</c>) throws on every input whatever the
/// witness expects, and a statement removal that no longer compiles was scored as a kill. Both
/// are removed at the catalog: lookup keys are not mutated and non-compiling mutants are not
/// emitted, so every mutant the engine judges is one only an expectation can kill.</para>
///
/// <para>Widening the scope raises the chance an honest brick meets an EQUIVALENT mutant — a
/// rewrite no witness could ever observe. The two the widening exposed are pinned here too:
/// identity arithmetic (<c>x * 1</c>, <c>x + 0</c>) is not swapped, and a constructor statement
/// that writes brick metadata nothing executes ever reads (<c>Id = "..."</c>) is not mutated,
/// while constructor state <c>ExecuteAsync</c> does read still is.</para>
/// </summary>
[Trait("Category", "Certification")]
public sealed class MutationCoverageTeethTests
{
    private static readonly IReadOnlyList<string> BrickReferences =
    [
        typeof(DomainBrick).Assembly.Location,
        typeof(BrickInput).Assembly.Location,
    ];

    // ── (a) the per-kind cap ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// surcharge=0 and discount=0 on every case, so <c>+ discount</c> and <c>- discount</c> agree
    /// and this witness cannot tell the two shipping bricks apart. It is the witness the
    /// adversarial fixture shipped with, verbatim.
    /// </summary>
    private static readonly WitnessSpec CapWitness = new(
        ShippingArithmeticBrickSource.BrickId,
        [
            new WitnessCase(
                new Dictionary<string, object>
                {
                    ["length"] = 100, ["width"] = 100, ["height"] = 100, ["weight"] = 10,
                    ["ratePerKg"] = 2, ["surcharge"] = 0, ["discount"] = 0,
                },
                new Dictionary<string, object> { ["total"] = 400 }),
            new WitnessCase(
                new Dictionary<string, object>
                {
                    ["length"] = 10, ["width"] = 10, ["height"] = 10, ["weight"] = 3,
                    ["ratePerKg"] = 5, ["surcharge"] = 0, ["discount"] = 0,
                },
                new Dictionary<string, object> { ["total"] = 15 }),
        ]);

    /// <summary>The cap witness plus one case with a non-zero surcharge AND discount: 15 + 7 - 3.</summary>
    private static readonly WitnessSpec StrengthenedCapWitness = new(
        ShippingArithmeticBrickSource.BrickId,
        [
            CapWitness.Cases[0],
            CapWitness.Cases[1],
            new WitnessCase(
                new Dictionary<string, object>
                {
                    ["length"] = 10, ["width"] = 10, ["height"] = 10, ["weight"] = 3,
                    ["ratePerKg"] = 5, ["surcharge"] = 7, ["discount"] = 3,
                },
                new Dictionary<string, object> { ["total"] = 19 }),
        ]);

    [Fact]
    public void SixArithmeticSites_AreAllMutated_NoneSilentlyCapped()
    {
        var source = ShippingArithmeticBrickSource.SubtractingDiscount;
        var mutations = AstMutationCatalog.CollectMutations(source, BrickReferences);

        var arithmetic = mutations
            .Where(m => m.Id.StartsWith("swap-arithmetic-op", StringComparison.Ordinal))
            .ToArray();

        var expectedLines = ShippingArithmeticBrickSource.ArithmeticLines
            .Select(text => LineOf(source, text))
            .ToArray();
        var expectedIds = new[]
        {
            $"swap-arithmetic-op-{expectedLines[0]}",
            $"swap-arithmetic-op-{expectedLines[0]}#2",
            $"swap-arithmetic-op-{expectedLines[1]}",
            $"swap-arithmetic-op-{expectedLines[2]}",
            $"swap-arithmetic-op-{expectedLines[3]}",
            $"swap-arithmetic-op-{expectedLines[3]}#2",
        };

        arithmetic.Select(m => m.Id).Should().BeEquivalentTo(expectedIds,
            "every one of the six arithmetic sites must be mutated; a cap that stops after four leaves the two that "
            + "distinguish the contradictory bricks unexamined. Emitted: [{0}]",
            string.Join(", ", arithmetic.Select(m => $"{m.Id} ({m.Site.OriginalText} -> {m.Site.MutatedText})")));

        arithmetic.Select(m => m.Site.LineText).Should().Contain(text => text.Contains("- discount", StringComparison.Ordinal),
            "the sixth site — the operator that separates the two bricks — must carry a mutant");
    }

    [Fact(Timeout = TestTimeouts.Integration)]
    public async Task ContradictoryShippingBricks_CannotBothCertify_AgainstTheCapWitness()
    {
        var gate = CreateGate();
        var subtracting = await gate.CertifyAsync(ShippingRequest(ShippingArithmeticBrickSource.SubtractingDiscount, CapWitness));
        var adding = await gate.CertifyAsync(ShippingRequest(ShippingArithmeticBrickSource.AddingDiscount, CapWitness));

        (subtracting.Admitted && adding.Admitted).Should().BeFalse(
            "'+ discount' and '- discount' cannot both be right, so a witness that admits both has not exercised the sixth site "
            + "(subtracting: admitted={0} {1}; adding: admitted={2} {3})",
            subtracting.Admitted, subtracting.Record.Reason, adding.Admitted, adding.Record.Reason);

        // Neither certifies, and each rejection names the site the witness never exercised. The
        // proposer needs the edit, not just the id, to know which case to add.
        foreach (var decision in new[] { subtracting, adding })
        {
            decision.Admitted.Should().BeFalse(decision.Record.Reason);
            decision.FailureCheck.Should().Be("mutation");
            decision.Record.SurvivingMutantIds.Should().Contain(
                id => id.StartsWith("swap-arithmetic-op", StringComparison.Ordinal),
                "the survivor is an arithmetic swap on a site the cap used to hide; survivors: [{0}]",
                string.Join(", ", decision.Record.SurvivingMutantIds));
            decision.Record.Reason.Should().Contain("discount");
        }
    }

    [Fact(Timeout = TestTimeouts.Integration)]
    public async Task StrengthenedCapWitness_CertifiesExactlyOneShippingBrick()
    {
        // Positive control: one case with a non-zero surcharge and discount kills every arithmetic
        // mutant of the honest brick, which certifies; the contradictory brick is the wrong
        // program and falls at correctness.
        var gate = CreateGate();
        var subtracting = await gate.CertifyAsync(ShippingRequest(ShippingArithmeticBrickSource.SubtractingDiscount, StrengthenedCapWitness));
        var adding = await gate.CertifyAsync(ShippingRequest(ShippingArithmeticBrickSource.AddingDiscount, StrengthenedCapWitness));

        subtracting.Admitted.Should().BeTrue(subtracting.Record.Reason);
        subtracting.Record.EscapeRate.Should().Be(0);
        subtracting.Record.KilledMutants.Count(id => id.StartsWith("swap-arithmetic-op", StringComparison.Ordinal))
            .Should().Be(6, "all six arithmetic mutants must be judged and killed, not four; killed: [{0}]",
                string.Join(", ", subtracting.Record.KilledMutants));

        adding.Admitted.Should().BeFalse();
        adding.FailureCheck.Should().Be("correctness");
    }

    // ── (b) the modifier-gated scope ─────────────────────────────────────────────────────────

    /// <summary>armor=0 on every case, so <c>- armor</c> and <c>+ armor</c> agree. Verbatim from the fixture.</summary>
    private static readonly WitnessSpec ToothlessHelperWitness = new(
        HelperContradictionBrickSource.BrickId,
        [
            new WitnessCase(
                new Dictionary<string, object> { ["baseDamage"] = 5, ["armor"] = 0 },
                new Dictionary<string, object> { ["finalDamage"] = 5 }),
            new WitnessCase(
                new Dictionary<string, object> { ["baseDamage"] = 12, ["armor"] = 0 },
                new Dictionary<string, object> { ["finalDamage"] = 12 }),
        ]);

    /// <summary>The toothless witness plus one case with non-zero armour, and one at the floor.</summary>
    private static readonly WitnessSpec StrengthenedHelperWitness = new(
        HelperContradictionBrickSource.BrickId,
        [
            ToothlessHelperWitness.Cases[0],
            ToothlessHelperWitness.Cases[1],
            new WitnessCase(
                new Dictionary<string, object> { ["baseDamage"] = 50, ["armor"] = 10 },
                new Dictionary<string, object> { ["finalDamage"] = 40 }),
            new WitnessCase(
                new Dictionary<string, object> { ["baseDamage"] = 3, ["armor"] = 10 },
                new Dictionary<string, object> { ["finalDamage"] = 0 }),
        ]);

    public static TheoryData<string> HelperModifiers()
    {
        var data = new TheoryData<string>();
        foreach (var modifiers in HelperContradictionBrickSource.HelperModifiers)
            data.Add(modifiers);
        return data;
    }

    [Theory]
    [MemberData(nameof(HelperModifiers))]
    public void HelperArithmetic_IsMutated_WhateverTheModifiers(string modifiers)
    {
        var source = HelperContradictionBrickSource.Subtracting(modifiers);
        var mutations = AstMutationCatalog.CollectMutations(source, BrickReferences);

        var arithmetic = mutations.Should().ContainSingle(
            m => m.Id.StartsWith("swap-arithmetic-op", StringComparison.Ordinal),
            "the only arithmetic in the brick is in the '{0}' helper, and it must be mutated; emitted: [{1}]",
            modifiers, string.Join(", ", mutations.Select(m => m.Id))).Subject;
        arithmetic.Site.OriginalText.Should().Be("-");
        arithmetic.Site.MutatedText.Should().Be("+");
        arithmetic.Site.LineText.Should().Contain("Resolve(int baseDamage, int armor)");
    }

    [Fact]
    public void EveryMemberBody_IsInScope_NotOnlyMethods()
    {
        // Constructors of nested types, property accessors, expression-bodied properties, indexers,
        // local functions and lambdas all carry logic a witness must be shown to observe.
        const string source = """
            public sealed class ShapesBrick
            {
                private readonly Table _table = new Table(3);

                public int ExecuteAsync(int n)
                {
                    int Twice(int v) => v * 2;
                    System.Func<int, int> thrice = v => v * 3;
                    return Twice(n) + thrice(n) + _table[n] + _table.Bias + Bias;
                }

                private int Bias => _table.Size - 1;

                private sealed class Table
                {
                    private readonly int _base;

                    public Table(int size)
                    {
                        Size = size;
                        _base = size + 10;
                    }

                    public int Size { get; }

                    public int Bias
                    {
                        get { return _base - Size; }
                    }

                    public int this[int i] => _base * i;
                }
            }
            """;

        var mutations = AstMutationCatalog.CollectMutations(source);
        var arithmeticLines = mutations
            .Where(m => m.Id.StartsWith("swap-arithmetic-op", StringComparison.Ordinal))
            .Select(m => m.Site.LineText)
            .ToArray();

        arithmeticLines.Should().Contain(l => l.Contains("v * 2", StringComparison.Ordinal), "local function body");
        arithmeticLines.Should().Contain(l => l.Contains("v * 3", StringComparison.Ordinal), "lambda body");
        arithmeticLines.Should().Contain(l => l.Contains("_table.Size - 1", StringComparison.Ordinal), "expression-bodied property");
        arithmeticLines.Should().Contain(l => l.Contains("_base = size + 10", StringComparison.Ordinal), "nested type's constructor");
        arithmeticLines.Should().Contain(l => l.Contains("_base - Size", StringComparison.Ordinal), "block-bodied getter");
        arithmeticLines.Should().Contain(l => l.Contains("_base * i", StringComparison.Ordinal), "expression-bodied indexer");
        arithmeticLines.Where(l => l.Contains("Twice(n) + thrice(n)", StringComparison.Ordinal))
            .Should().HaveCount(4, "every operator of the four-term sum in ExecuteAsync is a site");
    }

    [Theory(Timeout = TestTimeouts.Integration)]
    [MemberData(nameof(HelperModifiers))]
    public async Task ContradictoryHelperBricks_CannotBothCertify_AgainstTheToothlessWitness(string modifiers)
    {
        var gate = CreateGate();
        var subtracting = await gate.CertifyAsync(HelperRequest(HelperContradictionBrickSource.Subtracting(modifiers), ToothlessHelperWitness));
        var adding = await gate.CertifyAsync(HelperRequest(HelperContradictionBrickSource.Adding(modifiers), ToothlessHelperWitness));

        (subtracting.Admitted && adding.Admitted).Should().BeFalse(
            "'base - armor' and 'base + armor' in a '{0}' helper cannot both be right (subtracting: admitted={1} {2}; adding: admitted={3} {4})",
            modifiers, subtracting.Admitted, subtracting.Record.Reason, adding.Admitted, adding.Record.Reason);

        foreach (var decision in new[] { subtracting, adding })
        {
            decision.FailureCheck.Should().Be("mutation", "with armor=0 everywhere the witness has no teeth on the helper");
            decision.Record.SurvivingMutantIds.Should().Contain(
                id => id.StartsWith("swap-arithmetic-op", StringComparison.Ordinal),
                "survivors: [{0}]", string.Join(", ", decision.Record.SurvivingMutantIds));
        }
    }

    [Theory(Timeout = TestTimeouts.Integration)]
    [MemberData(nameof(HelperModifiers))]
    public async Task StrengthenedHelperWitness_CertifiesExactlyOneBrick(string modifiers)
    {
        var gate = CreateGate();
        var subtracting = await gate.CertifyAsync(HelperRequest(HelperContradictionBrickSource.Subtracting(modifiers), StrengthenedHelperWitness));
        var adding = await gate.CertifyAsync(HelperRequest(HelperContradictionBrickSource.Adding(modifiers), StrengthenedHelperWitness));

        subtracting.Admitted.Should().BeTrue(subtracting.Record.Reason);
        subtracting.Record.EscapeRate.Should().Be(0);
        subtracting.Record.KilledMutants.Should().Contain(
            id => id.StartsWith("swap-arithmetic-op", StringComparison.Ordinal),
            "escape_rate=0 must be earned against the helper's arithmetic, not scored in its absence");

        adding.Admitted.Should().BeFalse();
        adding.FailureCheck.Should().Be("correctness");
    }

    // ── kills the witness never earned ───────────────────────────────────────────────────────

    [Fact(Timeout = TestTimeouts.Integration)]
    public async Task EveryEmittedMutant_Compiles_StatementRemovalsIncluded()
    {
        // The engine scores a non-compiling mutant as killed. Removing `var baseDamage = input.Get(...)`
        // leaves a dangling use, so that "kill" is owed to the compiler, not to the witness. The
        // catalog must not emit it.
        foreach (var source in new[]
                 {
                     HelperContradictionBrickSource.Subtracting("private static"),
                     ShippingArithmeticBrickSource.SubtractingDiscount,
                     ContradictoryDamageBrickSource.Subtracting,
                 })
        {
            var mutations = AstMutationCatalog.CollectMutations(source, BrickReferences);
            mutations.Should().NotBeEmpty();
            (await CompileAsync(source)).Should().BeEmpty("the fixture itself must compile");

            var failures = new List<string>();
            foreach (var mutation in mutations)
            {
                var errors = await CompileAsync(mutation.ToSource());
                if (errors.Count > 0)
                    failures.Add($"{mutation.Id}: {mutation.Site.OriginalText} -> {mutation.Site.MutatedText}: {string.Join(" | ", errors)}");
            }

            failures.Should().BeEmpty(
                "a mutant that does not compile is dead before the witness sees it, and must not be counted as a kill");
        }
    }

    [Fact]
    public void LookupKeyLiterals_AreNotMutated_ButStoreKeysAndValuesAre()
    {
        // A mutated lookup key fails on every input regardless of what the witness expects; a
        // mutated store key drops a declared output, which only an expectation can notice.
        const string source = """
            using System.Collections.Generic;

            public sealed class KeysBrick
            {
                private readonly Dictionary<string, int> _rates = new();

                public string ExecuteAsync(Ashlar.Core.Domain.Execution.BrickInput input, Dictionary<string, int> table)
                {
                    var name = input.Get<string>("name");
                    var optional = input.Get<string>("nickname", "none");
                    var found = table.TryGetValue("rate", out var rate);
                    var has = table.ContainsKey("bonus");
                    var indexed = table["floor"];
                    var fallback = table.GetValueOrDefault("ceiling");
                    _rates["stored"] = rate;
                    var output = new Ashlar.Core.Domain.Execution.BrickOutput();
                    output.Set("message", "Hello " + name);
                    output["mode"] = optional;
                    return name + "!" + found + has + indexed + fallback;
                }
            }
            """;

        var mutations = AstMutationCatalog.CollectMutations(source, BrickReferences);
        var literals = mutations
            .Where(m => m.Id.StartsWith("mutate-string-literal", StringComparison.Ordinal))
            .Select(m => m.Site.OriginalText)
            .ToArray();

        literals.Should().NotContain(["\"name\"", "\"nickname\"", "\"rate\"", "\"bonus\"", "\"floor\"", "\"ceiling\""],
            "keys of Get / TryGetValue / ContainsKey / an indexer read / GetValueOrDefault are lookups");
        literals.Should().Contain(["\"none\"", "\"stored\"", "\"message\"", "\"mode\"", "\"Hello \"", "\"!\""],
            "a default value, a store key (Set, an indexer write) and text that reaches the output are all witness-observable");
    }

    // ── equivalent mutants the widening exposed ──────────────────────────────────────────────

    [Fact]
    public void IdentityArithmetic_IsNotSwapped()
    {
        const string source = """
            public sealed class IdentityBrick
            {
                public double ExecuteAsync(int n, double d)
                {
                    var a = n * 1;
                    var b = n / 1;
                    var c = n + 0;
                    var e = n - 0;
                    var f = d * 1.0;
                    var g = n * 2;
                    var h = 1 * n;
                    return a + b + c + e + f + g + h;
                }
            }
            """;

        var edits = AstMutationCatalog.CollectMutations(source)
            .Where(m => m.Id.StartsWith("swap-arithmetic-op", StringComparison.Ordinal))
            .Select(m => m.Site.LineText)
            .ToArray();

        edits.Should().NotContain(l => l.Contains("n * 1;", StringComparison.Ordinal), "n * 1 and n / 1 are the same program");
        edits.Should().NotContain(l => l.Contains("n / 1;", StringComparison.Ordinal));
        edits.Should().NotContain(l => l.Contains("n + 0;", StringComparison.Ordinal), "n + 0 and n - 0 are the same program");
        edits.Should().NotContain(l => l.Contains("n - 0;", StringComparison.Ordinal));
        edits.Should().NotContain(l => l.Contains("d * 1.0;", StringComparison.Ordinal));
        edits.Should().Contain(l => l.Contains("n * 2;", StringComparison.Ordinal), "n / 2 is a different program");
        edits.Should().Contain(l => l.Contains("1 * n;", StringComparison.Ordinal), "1 / n is a different program");
    }

    [Fact]
    public void ConstructorMetadata_NothingReads_IsNotMutated_ButConstructorStateExecuteReadsIs()
    {
        // Id/Name/Version/Description are written by the constructor and read by nothing the
        // witness can drive, so no witness case could ever kill a mutant of them. A field the
        // constructor initialises and ExecuteAsync reads is logic, and stays in scope.
        const string source = """
            using Ashlar.Core.Domain.Bricks;
            using Ashlar.Core.Domain.Execution;

            public sealed class RateBrick : DomainBrick
            {
                private readonly int _rate;

                public RateBrick()
                {
                    Id = "rate-brick";
                    Name = "Rate Brick";
                    Version = "1.0.0";
                    Description = "Applies a fixed rate.";
                    _rate = 5;
                    Interface = new BrickInterface
                    {
                        Inputs = [new BrickInputDefinition("amount", "int", "Amount")],
                        Outputs = [new BrickOutputDefinition("charged", "int", "Charged")]
                    };
                }

                public override Task<BrickOutput> ExecuteAsync(
                    BrickInput input,
                    ImplementationType implementation,
                    IExecutionContext context,
                    CancellationToken cancellationToken = default)
                {
                    var amount = input.Get<int>("amount");
                    var output = new BrickOutput();
                    output.Set("charged", amount * _rate);
                    return Task.FromResult(output);
                }
            }
            """;

        var mutations = AstMutationCatalog.CollectMutations(source, BrickReferences);
        var sites = mutations.Select(m => $"{m.Id}: {m.Site.LineText}").ToArray();

        sites.Should().NotContain(s => s.Contains("Id = ", StringComparison.Ordinal) || s.Contains("Name = ", StringComparison.Ordinal)
                || s.Contains("Version = ", StringComparison.Ordinal) || s.Contains("Description = ", StringComparison.Ordinal),
            "metadata nothing executes reads cannot be observed by any witness; emitted: [{0}]", string.Join("; ", sites));
        sites.Should().NotContain(s => s.Contains("BrickInputDefinition", StringComparison.Ordinal) || s.Contains("BrickOutputDefinition", StringComparison.Ordinal),
            "the interface declaration is the analyzer fence's to check, not the witness's");
        sites.Should().Contain(s => s.StartsWith("mutate-int-literal", StringComparison.Ordinal) && s.Contains("_rate = 5", StringComparison.Ordinal),
            "a rate the constructor sets and ExecuteAsync multiplies by is logic the witness must be shown to observe");
        sites.Should().Contain(s => s.StartsWith("swap-arithmetic-op", StringComparison.Ordinal) && s.Contains("amount * _rate", StringComparison.Ordinal));
    }

    // ── plumbing ─────────────────────────────────────────────────────────────────────────────

    private static CertificationGate CreateGate() => new(new CertificationRecordSigner());

    private static CertificationRequest ShippingRequest(string source, WitnessSpec witness) =>
        Request(source, ShippingArithmeticBrickSource.TypeName, witness);

    private static CertificationRequest HelperRequest(string source, WitnessSpec witness) =>
        Request(source, HelperContradictionBrickSource.TypeName, witness);

    /// <summary>
    /// The correctness leg runs a compiled instance; the mutation leg compiles SourceCode. Both
    /// come from the SAME text, so nothing but the catalog decides what the leg can see.
    /// </summary>
    private static CertificationRequest Request(string source, string typeName, WitnessSpec witness) => new()
    {
        Brick = CertifiedBrickCompiler.InstantiateBrick(source, typeName),
        Witness = witness,
        SourceCode = source,
        ProjectPath = CreateCleanProjectFile(),
        CompilationReferences = BrickReferences,
        BrickTypeName = typeName,
    };

    private static int LineOf(string source, string text)
    {
        var lines = source.Split('\n');
        for (var i = 0; i < lines.Length; i++)
        {
            if (lines[i].Contains(text, StringComparison.Ordinal))
                return i + 1;
        }

        throw new InvalidOperationException($"'{text}' is not a line of the fixture");
    }

    private static async Task<IReadOnlyList<string>> CompileAsync(string candidateSource)
    {
        var dir = Path.Combine(Path.GetTempPath(), "ashlar-coverage-teeth", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            var name = $"CoverageTeeth_{Guid.NewGuid():N}";
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

    private static string CreateCleanProjectFile()
    {
        var path = Path.Combine(Path.GetTempPath(), $"ashlar-cert-clean-{Guid.NewGuid():N}.csproj");
        File.WriteAllText(path, """
<Project Sdk="Microsoft.NET.Sdk">
  <ItemGroup>
    <PackageReference Include="Ashlar.Brick.Contracts" Version="0.1.0" />
    <PackageReference Include="Ashlar.Authoring" Version="0.1.0" />
  </ItemGroup>
</Project>
""");
        return path;
    }
}
