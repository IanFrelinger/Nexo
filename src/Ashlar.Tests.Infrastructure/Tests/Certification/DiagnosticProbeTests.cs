using FluentAssertions;
using Ashlar.Core.Application.Certification.Models;
using Ashlar.Core.Application.Certification.Ports;
using Ashlar.Core.Domain.Bricks;
using Ashlar.Core.Domain.Execution;
using Ashlar.Infrastructure.Certification;
using Ashlar.Infrastructure.Certification.Probes;
using Ashlar.Tests.Infrastructure.Certification.Fixtures;
using Xunit;

namespace Ashlar.Tests.Infrastructure.Tests.Certification;

/// <summary>
/// Diagnostic probes (trust-loop G4, the PR-D half left open): on gate failure, probes
/// attach cause + minimal machine-usable facts WITHOUT touching the verbatim feedback or
/// the verdict — a probe holds no authority, and a probe that throws is skipped while the
/// decision stands.
/// </summary>
[Trait("Category", "Certification")]
public sealed class DiagnosticProbeTests
{
    private static readonly WitnessSpec WeakWitness = new(
        "mutation-probe-brick",
        [
            new WitnessCase(
                new Dictionary<string, object>
                {
                    ["logText"] = "2024-01-01 ERROR First failure: connection reset\n2024-01-01 ERROR Second failure: timeout"
                },
                new Dictionary<string, object> { ["errorCount"] = 2 })
        ]);

    [Fact]
    public async Task MutationFailure_CarriesTheSurvivorProbeFinding()
    {
        var gate = new CertificationGate(new CertificationRecordSigner());
        var decision = await gate.CertifyAsync(ProbeRequest(WeakWitness));

        decision.Admitted.Should().BeFalse();
        decision.FailureCheck.Should().Be("mutation");
        var finding = decision.ProbeFindings.Should()
            .ContainSingle(f => f.Probe == nameof(MutationSurvivorProbe)).Subject;
        finding.Cause.Should().Contain("escaped the witness");
        finding.Facts.Should().ContainKey("survivors").WhoseValue.Should().NotBeNullOrWhiteSpace();
        finding.Facts["survivors"].Should().Be(
            string.Join(",", decision.Record.SurvivingMutantIds),
            "probe facts must be the record's own data, never re-derived");
    }

    [Fact]
    public async Task AnalyzerFailure_GroupsFindingsByRule_AndNonCompilingNamesTheFirstError()
    {
        var gate = new CertificationGate(new CertificationRecordSigner());

        var analyzerFail = await gate.CertifyAsync(ProbeRequest(WeakWitness) with
        {
            SourceCode = MutationProbeBrickSource.Code.Replace(
                "var errorCount = errorLines.Count;",
                "var errorCount = errorLines.Count; var t = DateTime.Now; if (t.Ticks < 0) errorLines.Clear();")
        });
        analyzerFail.FailureCheck.Should().Be("analyzer");
        var grouped = analyzerFail.ProbeFindings.Should()
            .ContainSingle(f => f.Probe == nameof(AnalyzerFindingGroupProbe)).Subject;
        grouped.Facts.Should().ContainKey("ASHLAR0006");

        var nonCompiling = await gate.CertifyAsync(ProbeRequest(WeakWitness) with
        {
            SourceCode = "public sealed class Broken {"
        });
        nonCompiling.FailureCheck.Should().Be("analyzer");
        nonCompiling.ProbeFindings.Should()
            .Contain(f => f.Probe == nameof(NonCompilingCandidateProbe))
            .Which.Facts.Should().ContainKey("firstError");
    }

    [Fact]
    public async Task AThrowingProbe_IsSkipped_AndTheVerdictStands()
    {
        var gate = new CertificationGate(
            new CertificationRecordSigner(),
            probes: new IDiagnosticProbe[] { new SabotageProbe(), new MutationSurvivorProbe() });

        var decision = await gate.CertifyAsync(ProbeRequest(WeakWitness));

        decision.Admitted.Should().BeFalse("a probe can never change a verdict");
        decision.FailureCheck.Should().Be("mutation");
        decision.ProbeFindings.Should().ContainSingle(f => f.Probe == nameof(MutationSurvivorProbe),
            "the healthy probe still ran after the sabotaged one was skipped");
    }

    [Fact]
    public async Task AdmittedDecisions_CarryNoProbeFindings()
    {
        var strongWitness = new WitnessSpec(
            "mutation-probe-brick",
            [
                new WitnessCase(
                    new Dictionary<string, object>
                    {
                        ["logText"] = "2024-01-01 ERROR First failure: connection reset\n2024-01-01 ERROR Second failure: timeout"
                    },
                    new Dictionary<string, object>
                    {
                        ["errorCount"] = 2,
                        ["firstErrorMessage"] = "First failure: connection reset"
                    })
            ]);
        var gate = new CertificationGate(new CertificationRecordSigner());

        var decision = await gate.CertifyAsync(ProbeRequest(strongWitness));

        decision.Admitted.Should().BeTrue(decision.Record.Reason);
        decision.ProbeFindings.Should().BeEmpty("probes run on failure only");
    }

    // --- helpers -------------------------------------------------------------------------

    private static CertificationRequest ProbeRequest(WitnessSpec witness) => new()
    {
        Brick = new MutationProbeBrick(),
        Witness = witness,
        SourceCode = MutationProbeBrickSource.Code,
        ProjectPath = CleanProjectFile(),
        CompilationReferences =
        [
            typeof(DomainBrick).Assembly.Location,
            typeof(BrickInput).Assembly.Location,
            typeof(MutationProbeBrick).Assembly.Location
        ],
        BrickTypeName = typeof(MutationProbeBrick).FullName,
    };

    private static string CleanProjectFile()
    {
        var path = Path.Combine(Path.GetTempPath(), $"ashlar-probe-{Guid.NewGuid():N}.csproj");
        File.WriteAllText(path, """
<Project Sdk="Microsoft.NET.Sdk">
  <ItemGroup>
    <PackageReference Include="Ashlar.Brick.Contracts" Version="0.1.0" />
  </ItemGroup>
</Project>
""");
        return path;
    }

    private sealed class SabotageProbe : IDiagnosticProbe
    {
        public string FailureCheck => "mutation";
        public DiagnosticProbeFinding? Probe(CertificationRequest request, CertificationDecision decision) =>
            throw new InvalidOperationException("probe sabotage");
    }
}
