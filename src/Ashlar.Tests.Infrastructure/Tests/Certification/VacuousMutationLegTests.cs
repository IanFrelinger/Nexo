using FluentAssertions;
using Ashlar.Core.Application.Certification.Models;
using Ashlar.Core.Domain.Bricks;
using Ashlar.Core.Domain.Execution;
using Ashlar.Infrastructure.Certification;
using Ashlar.Tests.Infrastructure.Certification.Fixtures;
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
                })
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
