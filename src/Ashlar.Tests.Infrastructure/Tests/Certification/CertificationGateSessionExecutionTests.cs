using FluentAssertions;
using Ashlar.Core.Application.Certification.Models;
using Ashlar.Core.Application.Certification.Ports;
using Ashlar.Core.Domain.Bricks;
using Ashlar.Core.Domain.Execution;
using Ashlar.Infrastructure.Certification;
using Ashlar.Tests.Infrastructure.Certification.Fixtures;
using Xunit;

namespace Ashlar.Tests.Infrastructure.Tests.Certification;

/// <summary>
/// The execution-backend seam: when a <see cref="CertificationRequest.ExecutionBackend"/>
/// is present, the witness, determinism, and mutation legs execute through it and the GATE
/// judges the raw observations. The backend runs; the gate decides. These facts prove the
/// gate actually judges backend data (tampered observations flip verdicts), that verdicts
/// stay identical to the in-process path for honest observations, and that backend
/// infrastructure failures throw rather than minting a verdict in either direction.
/// </summary>
[Trait("Category", "Certification")]
[Collection("hot-swap-host")]
public sealed class CertificationGateSessionExecutionTests
{
    private static readonly WitnessSpec StrongWitness = new(
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

    [Fact]
    public async Task HonestObservations_AdmitWithEscapeRateZero_AndRecordTheBackend()
    {
        var backend = new ScriptedExecutionBackend(ScriptedExecutionBackend.Mode.Honest);

        var decision = await Gate().CertifyAsync(Request(backend));

        decision.Admitted.Should().BeTrue(decision.Record.Reason);
        decision.Record.EscapeRate.Should().Be(0);
        backend.CandidateJobs.Should().Be(1, "one batched candidate execution serves witness AND determinism");
        backend.MutantUnitsSeen.Should().BeGreaterThan(0, "mutants execute through the backend, not in-process");
        decision.Record.Inputs.Should().Contain(i => i.Kind == "session-execution" && i.Id == backend.Describe(),
            "where execution happened is certificate evidence");
        decision.Record.GatesPassed.Should().Contain(g =>
            g.Name == "correctness-witness" && g.Configuration!.Contains("execution=scripted"),
            "the gate passes record where execution actually happened");
    }

    [Fact]
    public async Task TamperedCandidateObservations_RejectAtCorrectness()
    {
        var backend = new ScriptedExecutionBackend(ScriptedExecutionBackend.Mode.TamperedCandidate);

        var decision = await Gate().CertifyAsync(Request(backend));

        decision.Admitted.Should().BeFalse();
        decision.FailureCheck.Should().Be("correctness",
            "the gate judges backend observations — wrong outputs must flip the verdict");
        decision.Record.Signed.Should().BeFalse();
    }

    [Fact]
    public async Task NondeterministicRepeats_RejectAtDeterminism()
    {
        var backend = new ScriptedExecutionBackend(ScriptedExecutionBackend.Mode.NondeterministicRepeat);

        var decision = await Gate().CertifyAsync(Request(backend));

        decision.Admitted.Should().BeFalse();
        decision.FailureCheck.Should().Be("determinism",
            "repeat 0 satisfied the witness, so only the determinism comparison can catch the drift");
    }

    [Fact]
    public async Task AllMutantsSurviving_RejectAtMutation_WithHonestEscapeRate()
    {
        var backend = new ScriptedExecutionBackend(ScriptedExecutionBackend.Mode.MutantsSurvive);

        var decision = await Gate().CertifyAsync(Request(backend));

        decision.Admitted.Should().BeFalse();
        decision.FailureCheck.Should().Be("mutation");
        decision.Record.EscapeRate.Should().BeGreaterThan(0,
            "backend observations that satisfy the witness for every mutant are survivors, honestly reported");
    }

    [Fact]
    public async Task BackendInfrastructureFailure_Throws_AndMintsNoVerdict()
    {
        var backend = new ScriptedExecutionBackend(ScriptedExecutionBackend.Mode.Throw);

        var act = () => Gate().CertifyAsync(Request(backend));

        await act.Should().ThrowAsync<InvalidOperationException>(
            "an execution-backend failure is not evidence about the candidate — it must never "
            + "become a signed verdict in either direction");
    }

    // --- helpers -------------------------------------------------------------------------

    private static CertificationGate Gate() => new(new CertificationRecordSigner());

    private static CertificationRequest Request(ICandidateExecutionBackend backend) => new()
    {
        Brick = new MutationProbeBrick(),
        Witness = StrongWitness,
        SourceCode = MutationProbeBrickSource.Code,
        ProjectPath = CleanProjectFile(),
        CompilationReferences =
        [
            typeof(DomainBrick).Assembly.Location,
            typeof(BrickInput).Assembly.Location,
            typeof(MutationProbeBrick).Assembly.Location
        ],
        BrickTypeName = typeof(MutationProbeBrick).FullName,
        ExecutionBackend = backend,
    };

    private static string CleanProjectFile()
    {
        var path = Path.Combine(Path.GetTempPath(), $"ashlar-exec-gate-{Guid.NewGuid():N}.csproj");
        File.WriteAllText(path, """
<Project Sdk="Microsoft.NET.Sdk">
  <ItemGroup>
    <PackageReference Include="Ashlar.Brick.Contracts" Version="0.1.0" />
  </ItemGroup>
</Project>
""");
        return path;
    }

    /// <summary>
    /// A backend double that OBSERVES like a remote executor would: for the candidate it
    /// runs the real probe brick in-process and reports raw outputs; for mutants it
    /// reports whatever its mode scripts. It never judges anything.
    /// </summary>
    private sealed class ScriptedExecutionBackend : ICandidateExecutionBackend
    {
        public enum Mode
        {
            Honest,
            TamperedCandidate,
            NondeterministicRepeat,
            MutantsSurvive,
            Throw,
        }

        private readonly Mode _mode;

        public ScriptedExecutionBackend(Mode mode) => _mode = mode;

        public int CandidateJobs { get; private set; }

        public int MutantUnitsSeen { get; private set; }

        public string Describe() => "scripted";

        public async Task<CandidateExecutionReport> ExecuteAsync(
            CandidateExecutionJob job, CancellationToken cancellationToken = default)
        {
            if (_mode == Mode.Throw)
                throw new InvalidOperationException("scripted backend infrastructure failure");

            var observations = new List<CandidateCaseObservation>();
            foreach (var unit in job.Units)
            {
                var isCandidate = unit.Assembly is null;
                if (isCandidate)
                    CandidateJobs++;
                else
                    MutantUnitsSeen++;

                for (var caseIndex = 0; caseIndex < job.Witness.Cases.Count; caseIndex++)
                {
                    for (var repeat = 0; repeat < job.Repeats; repeat++)
                    {
                        observations.Add(await ObserveAsync(
                            unit.UnitId, isCandidate, job.Witness.Cases[caseIndex], caseIndex, repeat,
                            cancellationToken).ConfigureAwait(false));
                    }
                }
            }

            return new CandidateExecutionReport(observations);
        }

        private async Task<CandidateCaseObservation> ObserveAsync(
            string unitId, bool isCandidate, WitnessCase witnessCase, int caseIndex, int repeat,
            CancellationToken cancellationToken)
        {
            if (!isCandidate && _mode != Mode.MutantsSurvive)
            {
                // A remote mutant execution whose outputs satisfy nothing: killed by the judge.
                return new CandidateCaseObservation(
                    unitId, caseIndex, repeat, Threw: false, Error: null,
                    Summary: "mutant", Outputs: new Dictionary<string, object?>());
            }

            // Execute the REAL brick to produce honest raw observations.
            var brick = new MutationProbeBrick();
            var output = await brick.ExecuteAsync(
                new BrickInput(witnessCase.Input),
                ImplementationType.Deterministic,
                new ObservationContext(),
                cancellationToken).ConfigureAwait(false);
            var outputs = output.ToDictionary().ToDictionary(kv => kv.Key, kv => (object?)kv.Value);

            if (_mode == Mode.TamperedCandidate && isCandidate)
                outputs["errorCount"] = 999;
            var summary = _mode == Mode.NondeterministicRepeat && isCandidate && repeat > 0
                ? $"drifted-{repeat}"
                : output.Summary;

            return new CandidateCaseObservation(
                unitId, caseIndex, repeat, Threw: false, Error: null, Summary: summary, Outputs: outputs);
        }

        private sealed class ObservationContext : IExecutionContext
        {
            public string AgentId => "scripted-backend";
            public string BehaviorId => "scripted-backend";
            public bool IsAirGapped => true;
            public bool AuditMode => true;
            public string Provider => "deterministic";
            public IReadOnlyDictionary<string, object> Variables { get; } = new Dictionary<string, object>();
        }
    }
}
