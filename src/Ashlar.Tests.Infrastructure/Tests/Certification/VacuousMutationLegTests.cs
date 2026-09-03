using FluentAssertions;
using Ashlar.Core.Application.Certification.Models;
using Ashlar.Core.Domain.Bricks;
using Ashlar.Core.Domain.Execution;
using Ashlar.Infrastructure.Certification;
using Ashlar.Tests.Infrastructure.Certification.Fixtures;
using Ashlar.Tests.Infrastructure.Certification.Reuse;
using Ashlar.Tests.Infrastructure.Helpers;
using Xunit;

namespace Ashlar.Tests.Infrastructure.Tests.Certification;

/// <summary>
/// The mutation leg must never report a kill it did not earn.
///
/// <para>A mutant dies for one of two reasons, and they are not interchangeable: the WITNESS
/// caught it (a real kill, and the only kind an escape rate may count), or the HARNESS could not
/// run it (a vacuous kill, which proves nothing about the witness). The candidate wrapper used to
/// inject the deterministic <c>CertAuditContext</c> only when it could find a namespace brace, so
/// a brick with no namespace — the first thing a newcomer writes — produced mutants that every
/// run threw on. Every throw was scored as a kill, <c>escape_rate</c> came out 0.0, and the gate
/// SIGNED a record asserting the witness had teeth when the leg had proved nothing at all.</para>
/// </summary>
[Trait("Category", "Certification")]
public sealed class VacuousMutationLegTests
{
    private static readonly string ProbeLog =
        "2024-01-01 INFO Started\n2024-01-01 ERROR First failure: connection reset\n2024-01-01 WARN Retrying\n2024-01-01 ERROR Second failure: timeout";

    // The type name as it appears in a mutant compiled from source with no namespace.
    private const string GlobalBrickTypeName = "MutationProbeBrick";

    private static readonly WitnessSpec StrongWitness = new(
        "mutation-probe-brick",
        [
            new WitnessCase(
                new Dictionary<string, object> { ["logText"] = ProbeLog },
                new Dictionary<string, object>
                {
                    ["errorCount"] = 2,
                    ["firstErrorMessage"] = "First failure: connection reset",
                }),
            MutationProbeWitnesses.ZeroErrorCase
        ]);

    // Observes errorCount only, so every mutant that can only change firstErrorMessage escapes.
    private static readonly WitnessSpec WeakWitness = new(
        "mutation-probe-brick",
        [
            new WitnessCase(
                new Dictionary<string, object> { ["logText"] = ProbeLog },
                new Dictionary<string, object> { ["errorCount"] = 2 })
        ]);

    [Fact]
    public void TheWrap_InjectsTheAuditContext_ForEveryCandidateShape()
    {
        // The wrapper is the single place the certification path decides what the compiler sees.
        // If it silently omits the audit context, the mutation leg has no harness and every
        // verdict it produces is noise — so the injection is not allowed to be conditional on
        // the candidate's punctuation.
        CandidateSourceWrapper.Wrap(NoNamespaceProbeBrickSource.Code)
            .Should().Contain("class CertAuditContext", "a brick with no namespace still needs a harness");
        CandidateSourceWrapper.Wrap(MutationProbeBrickSource.Code)
            .Should().Contain("class CertAuditContext");
        CandidateSourceWrapper.Wrap("namespace N { public sealed class C { } }")
            .Should().Contain("class CertAuditContext", "a block-scoped namespace still needs a harness");
        CandidateSourceWrapper.Wrap("namespace N;\npublic sealed record R(int A);\n")
            .Should().Contain("class CertAuditContext", "a candidate with no brace at all still needs a harness");
    }

    [Fact]
    public async Task NoNamespaceBrick_WeakWitness_IsRejected_NotVacuouslyAdmitted()
    {
        // The same weak witness rejects the namespaced brick (CertificationGateTeethTests). If
        // dropping the namespace flips that to ADMIT, the mutation leg is scoring harness
        // failures as kills.
        var decision = await CreateGate().CertifyAsync(Request(WeakWitness));

        decision.Admitted.Should().BeFalse(
            "a witness that cannot observe firstErrorMessage lets mutants escape, namespace or not");
        decision.FailureCheck.Should().Be("mutation");
        decision.Record.EscapeRate.Should().BeGreaterThan(0);
        decision.Record.SurvivingMutantIds.Should().NotBeEmpty();
    }

    [Fact]
    public async Task NoNamespaceBrick_StrongWitness_StillAdmits()
    {
        // The fix must make the leg WORK, not merely refuse: a newcomer's first brick with a
        // witness that does have teeth is a legitimate candidate and must still certify.
        var decision = await CreateGate().CertifyAsync(Request(StrongWitness));

        decision.Admitted.Should().BeTrue(decision.Record.Reason);
        decision.Record.EscapeRate.Should().Be(0);
        decision.Record.TotalMutants.Should().BeGreaterThan(0);
    }

    // ── kills must be owed to the witness's expectations ─────────────────────────────────────

    /// <summary>
    /// The helper-contradiction brick's strengthened witness: two cases with armour 0, one with
    /// armour 10 that tells <c>-</c> from <c>+</c>, and one at the floor.
    /// </summary>
    private static readonly WitnessSpec HelperWitness = new(
        HelperContradictionBrickSource.BrickId,
        [
            new WitnessCase(
                new Dictionary<string, object> { ["baseDamage"] = 5, ["armor"] = 0 },
                new Dictionary<string, object> { ["finalDamage"] = 5 }),
            new WitnessCase(
                new Dictionary<string, object> { ["baseDamage"] = 12, ["armor"] = 0 },
                new Dictionary<string, object> { ["finalDamage"] = 12 }),
            new WitnessCase(
                new Dictionary<string, object> { ["baseDamage"] = 50, ["armor"] = 10 },
                new Dictionary<string, object> { ["finalDamage"] = 40 }),
            new WitnessCase(
                new Dictionary<string, object> { ["baseDamage"] = 3, ["armor"] = 10 },
                new Dictionary<string, object> { ["finalDamage"] = 0 }),
        ]);

    /// <summary>The same inputs with every expectation removed: a witness that observes nothing.</summary>
    private static WitnessSpec Vacuous(WitnessSpec witness) => witness with
    {
        Cases = witness.Cases.Select(c => new WitnessCase(c.Input, new Dictionary<string, object>())).ToArray(),
    };

    [Fact(Timeout = TestTimeouts.Integration)]
    public async Task AVacuousWitness_KillsNothing_SoEveryKillIsOwedToAnExpectation()
    {
        // The mutation leg reports how many mutants the WITNESS caught. A mutant that dies under
        // a witness with no expectations at all died for some other reason — a mutated lookup key
        // that throws on every input, a statement removal that never compiled — and counting it
        // says the witness has teeth it was never shown to have. Such mutants are not emitted.
        var source = HelperContradictionBrickSource.Subtracting("private static");
        var engine = new BrickMutationEngine();

        var vacuous = await engine.RunAsync(
            source, HelperContradictionBrickSource.TypeName, Vacuous(HelperWitness), References(), CancellationToken.None);
        var real = await engine.RunAsync(
            source, HelperContradictionBrickSource.TypeName, HelperWitness, References(), CancellationToken.None);

        vacuous.TotalMutants.Should().BeGreaterThan(0);
        vacuous.KilledMutantIds.Should().BeEmpty(
            "a witness with no expectations can observe nothing, so any mutant it 'kills' owes its death to something other than "
            + "the witness; killed: [{0}]", string.Join(", ", vacuous.KilledMutantIds));

        real.TotalMutants.Should().Be(vacuous.TotalMutants, "the mutant set is a property of the source, not the witness");
        real.KilledMutantIds.Should().Contain(id => id.StartsWith("swap-arithmetic-op", StringComparison.Ordinal));
        real.SurvivingMutantIds.Should().BeEmpty(real.SurvivingMutantIds.Count == 0 ? string.Empty
            : "the strengthened witness must kill every mutant of the honest brick; survivors: "
            + string.Join(", ", real.Survivors.Select(s => s.Describe())));
    }

    [Fact(Timeout = TestTimeouts.Integration)]
    public async Task AWitnessThatKillsNoMoreThanTheVacuousOne_IsToothless_AndIsRejected()
    {
        // The flag for a toothless witness, stated operationally: strip every expectation, re-run,
        // and whatever the kill set did NOT gain is logic the expectations never reached. armor=0
        // on every case is exactly that witness for a damage brick: the only kill its expectations
        // add over the vacuous run is the output KEY (it asserts finalDamage exists), not one
        // mutant of the arithmetic — and the gate must refuse it, honest brick or not, with the
        // arithmetic mutant named as the survivor.
        var source = HelperContradictionBrickSource.Subtracting("private static");
        var toothless = HelperWitness with { Cases = HelperWitness.Cases.Take(2).ToArray() };
        var engine = new BrickMutationEngine();

        var vacuous = await engine.RunAsync(
            source, HelperContradictionBrickSource.TypeName, Vacuous(toothless), References(), CancellationToken.None);
        var judged = await engine.RunAsync(
            source, HelperContradictionBrickSource.TypeName, toothless, References(), CancellationToken.None);

        var owedToExpectations = judged.KilledMutantIds.Except(vacuous.KilledMutantIds, StringComparer.Ordinal).ToArray();
        owedToExpectations.Should().OnlyContain(id => id.StartsWith("mutate-string-literal", StringComparison.Ordinal),
            "with armor=0 everywhere the expectations kill the output-key mutant and nothing else — not one mutant of the logic; "
            + "owed: [{0}]", string.Join(", ", owedToExpectations));
        judged.SurvivingMutantIds.Should().Contain(id => id.StartsWith("swap-arithmetic-op", StringComparison.Ordinal));

        var decision = await CreateGate().CertifyAsync(new CertificationRequest
        {
            Brick = CertifiedBrickCompiler.InstantiateBrick(source, HelperContradictionBrickSource.TypeName),
            Witness = toothless,
            SourceCode = source,
            ProjectPath = CreateCleanProjectFile(),
            CompilationReferences = References(),
            BrickTypeName = HelperContradictionBrickSource.TypeName,
        });

        decision.Admitted.Should().BeFalse("a witness whose kills are all owed to something other than its expectations must not certify");
        decision.FailureCheck.Should().Be("mutation");
        decision.Record.SurvivingMutantIds.Should().Contain(id => id.StartsWith("swap-arithmetic-op", StringComparison.Ordinal));
    }

    private static List<string> References() =>
    [
        typeof(DomainBrick).Assembly.Location,
        typeof(BrickInput).Assembly.Location,
    ];

    private static CertificationGate CreateGate() => new(new CertificationRecordSigner());

    private static CertificationRequest Request(WitnessSpec witness) => new()
    {
        // The correctness leg runs the compiled probe brick; the mutation and analyzer legs
        // compile SourceCode, which is the same brick with its namespace removed. The defect
        // under test lives entirely in how SourceCode is wrapped and mutated.
        Brick = new MutationProbeBrick(),
        Witness = witness,
        SourceCode = NoNamespaceProbeBrickSource.Code,
        ProjectPath = CreateCleanProjectFile(),
        CompilationReferences =
        [
            typeof(DomainBrick).Assembly.Location,
            typeof(BrickInput).Assembly.Location,
            typeof(MutationProbeBrick).Assembly.Location,
        ],
        BrickTypeName = GlobalBrickTypeName,
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
