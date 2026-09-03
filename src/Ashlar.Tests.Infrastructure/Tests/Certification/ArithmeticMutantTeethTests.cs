using FluentAssertions;
using Ashlar.Core.Application.Certification.Models;
using Ashlar.Core.Domain.Bricks;
using Ashlar.Core.Domain.Execution;
using Ashlar.Infrastructure.Certification;
using Ashlar.Tests.Infrastructure.Certification.Fixtures;
using Ashlar.Tests.Infrastructure.Certification.Reuse;
using Xunit;

namespace Ashlar.Tests.Infrastructure.Tests.Certification;

/// <summary>
/// The mutation leg's entire job is proving the witness would notice if the brick's logic were
/// wrong. It cannot do that for arithmetic it never mutates.
///
/// <para>Two contradictory bricks — <c>Math.Max(0, baseDamage - armor)</c> and
/// <c>Math.Max(0, baseDamage + armor)</c> — both certified <c>ADMIT escape_rate=0
/// mutants_killed=5</c> against the SAME witness, because the catalog generated three
/// string-literal mutants, one integer-literal mutant, one statement removal, and not a single
/// arithmetic-operator mutant. A witness that never exercised the arithmetic scored a perfect
/// kill rate, and the gate signed a certificate asserting that witness had teeth.</para>
///
/// <para>These tests pin the fix from both sides: the toothless witness must now be REJECTED
/// with the surviving arithmetic mutant named in the record, and the same witness strengthened
/// by one case that tells <c>+</c> from <c>-</c> must still certify — with that mutant killed,
/// not skipped.</para>
/// </summary>
[Trait("Category", "Certification")]
public sealed class ArithmeticMutantTeethTests
{
    /// <summary>
    /// With armour 0 and base damage 0, <c>base - armor</c> and <c>base + armor</c> agree, so
    /// this witness cannot tell the two bricks apart. It still kills every PRE-FIX mutant:
    /// <c>Math.Max(2, ...)</c> returns 2 instead of 0, a mutated input key throws
    /// <c>KeyNotFoundException</c>, a mutated output key leaves <c>finalDamage</c> unset, and
    /// removing the first statement does not compile. Exactly the reported ADMIT.
    /// </summary>
    private static readonly WitnessSpec ToothlessWitness = new(
        ContradictoryDamageBrickSource.BrickId,
        [
            new WitnessCase(
                new Dictionary<string, object> { ["baseDamage"] = 0, ["armor"] = 0 },
                new Dictionary<string, object> { ["finalDamage"] = 0 })
        ]);

    /// <summary>The toothless witness plus the ONE case that distinguishes <c>-</c> from <c>+</c>.</summary>
    private static readonly WitnessSpec StrengthenedWitness = new(
        ContradictoryDamageBrickSource.BrickId,
        [
            ToothlessWitness.Cases[0],
            new WitnessCase(
                new Dictionary<string, object> { ["baseDamage"] = 50, ["armor"] = 10 },
                new Dictionary<string, object> { ["finalDamage"] = 40 })
        ]);

    [Theory]
    [InlineData("-")]
    [InlineData("+")]
    public async Task ToothlessWitness_IsRejected_WithTheArithmeticMutantNamedAsSurvivor(string op)
    {
        var source = op == "-" ? ContradictoryDamageBrickSource.Subtracting : ContradictoryDamageBrickSource.Adding;

        var decision = await CreateGate().CertifyAsync(Request(source, ToothlessWitness));

        decision.Admitted.Should().BeFalse(
            "a witness that never exercises the arithmetic has not shown it would notice '{0}' being wrong; record: {1}",
            op, decision.Record.Reason);
        decision.FailureCheck.Should().Be("mutation");
        decision.Record.EscapeRate.Should().BeGreaterThan(0);
        decision.Record.SurvivingMutantIds.Should().NotBeEmpty();
        decision.Record.SurvivingMutantIds.Should().Contain(
            id => id.StartsWith("swap-arithmetic-op", StringComparison.Ordinal),
            "the survivor that proves the witness toothless is the arithmetic operator swap; survivors were [{0}]",
            string.Join(", ", decision.Record.SurvivingMutantIds));
        decision.Record.Reason.Should().Contain(op == "-" ? "- -> +" : "+ -> -",
            "the rejection must show the edit the witness failed to notice, so the proposer can add the case that would");
    }

    [Fact]
    public async Task ContradictoryBricks_CannotBothCertify_AgainstOneWitness()
    {
        // The headline defect, stated as the invariant it violated: two programs that contradict
        // each other must not both earn escape_rate=0 from the same witness.
        var gate = CreateGate();
        var subtracting = await gate.CertifyAsync(Request(ContradictoryDamageBrickSource.Subtracting, ToothlessWitness));
        var adding = await gate.CertifyAsync(Request(ContradictoryDamageBrickSource.Adding, ToothlessWitness));

        var bothPerfect = subtracting.Record.EscapeRate == 0 && adding.Record.EscapeRate == 0;
        bothPerfect.Should().BeFalse(
            "'base - armor' and 'base + armor' cannot both be right, so a witness that gives both escape_rate=0 has no teeth "
            + "(subtracting: admitted={0} escape={1}; adding: admitted={2} escape={3})",
            subtracting.Admitted, subtracting.Record.EscapeRate, adding.Admitted, adding.Record.EscapeRate);
    }

    [Fact]
    public async Task StrengthenedWitness_Certifies_WithTheArithmeticMutantKilled()
    {
        // Positive control: the fix must make the leg WORK, not merely refuse. One case where
        // armour is non-zero is enough to kill the operator swap, and the brick certifies.
        var decision = await CreateGate().CertifyAsync(Request(ContradictoryDamageBrickSource.Subtracting, StrengthenedWitness));

        decision.Admitted.Should().BeTrue(decision.Record.Reason);
        decision.Record.EscapeRate.Should().Be(0);
        decision.Record.KilledMutants.Should().Contain(
            id => id.StartsWith("swap-arithmetic-op", StringComparison.Ordinal),
            "escape_rate=0 must be EARNED against the arithmetic mutant, not scored in its absence");
    }

    [Fact]
    public async Task StrengthenedWitness_RejectsTheContradictoryBrick_AtCorrectness()
    {
        // The same strengthened witness must refuse the '+' brick outright — it is the wrong
        // program, and the correctness leg (not mutation) is where a wrong program is caught.
        var decision = await CreateGate().CertifyAsync(Request(ContradictoryDamageBrickSource.Adding, StrengthenedWitness));

        decision.Admitted.Should().BeFalse();
        decision.FailureCheck.Should().Be("correctness");
    }

    private static CertificationGate CreateGate() => new(new CertificationRecordSigner());

    /// <summary>
    /// The correctness leg runs a compiled instance; the mutation leg compiles SourceCode. Both
    /// come from the SAME text here, so nothing but the catalog decides what the leg can see.
    /// </summary>
    private static CertificationRequest Request(string source, WitnessSpec witness) => new()
    {
        Brick = CertifiedBrickCompiler.InstantiateBrick(source, ContradictoryDamageBrickSource.TypeName),
        Witness = witness,
        SourceCode = source,
        ProjectPath = CreateCleanProjectFile(),
        CompilationReferences =
        [
            typeof(DomainBrick).Assembly.Location,
            typeof(BrickInput).Assembly.Location,
        ],
        BrickTypeName = ContradictoryDamageBrickSource.TypeName,
    };

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
