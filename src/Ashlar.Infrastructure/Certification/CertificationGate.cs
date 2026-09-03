#pragma warning disable ASHLAREXP001 // The gate enforces the experimental autonomy contract (touch-set, lineage) by design; see docs/SdkCompatibilityPolicy.md.
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Ashlar.Certification.Contracts;
using Ashlar.Core.Application.Certification.Models;
using Ashlar.Core.Application.Certification.Ports;

namespace Ashlar.Infrastructure.Certification;

/// <summary>Certification gate: mutation testing, dependency checks, signing, and admit/reject decisions.</summary>
public sealed class CertificationGate : ICertificationGate
{
    private const string GateVersion = "1";

    private static readonly CertificationGatePass CorrectnessGatePass = new() { Name = "correctness-witness", Version = GateVersion };
    private static readonly CertificationGatePass MutationGatePass = new() { Name = "mutation-gate", Version = GateVersion, Configuration = "escapeRateThreshold=0" };
    private static readonly CertificationGatePass DeterminismGatePass = new() { Name = "determinism", Version = GateVersion };
    private static readonly CertificationGatePass DependencyGatePass = new() { Name = "dependency-graph", Version = GateVersion };

    private static readonly JsonSerializerOptions WitnessHashOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    private readonly BrickMutationEngine _mutationEngine = new();
    private readonly CertificationRecordSigner _signer;
    private readonly AnalyzerFenceGate _analyzerGate;
    private readonly ILogger<CertificationGate>? _logger;
    private readonly CandidateExecutionLimits _executionLimits;

    /// <summary>Initializes a new certification gate.</summary>
    /// <param name="signer">Signs admitted records.</param>
    /// <param name="logger">Optional logger.</param>
    /// <param name="analyzerGate">The analyzer fence; defaults on, never off.</param>
    /// <param name="probes">Diagnostic probes attached to rejections.</param>
    /// <param name="executionLimits">
    /// The wall-clock and memory bounds on every execution of candidate or mutant code when the
    /// request names no execution backend (see <see cref="CandidateExecutionLimits"/>). Recorded
    /// on the certificate's gate passes. Null uses <see cref="CandidateExecutionLimits.Default"/>.
    /// </param>
    public CertificationGate(
        CertificationRecordSigner signer,
        ILogger<CertificationGate>? logger = null,
        AnalyzerFenceGate? analyzerGate = null,
        IEnumerable<IDiagnosticProbe>? probes = null,
        CandidateExecutionLimits? executionLimits = null)
    {
        _signer = signer ?? throw new ArgumentNullException(nameof(signer));
        _logger = logger;
        // Defaulting the gate on (rather than null-meaning-skip) keeps the chain fail-closed:
        // no construction path exists that certifies without the analyzer fence.
        _analyzerGate = analyzerGate ?? new AnalyzerFenceGate();
        _probes = probes?.ToArray() ?? Probes.CertificationProbeCatalog.Default;
        _executionLimits = executionLimits ?? CandidateExecutionLimits.Default;
        _executionLimits.Validate();
    }

    private readonly IReadOnlyList<IDiagnosticProbe> _probes;

    /// <summary>Certify asynchronously; on rejection, diagnostic probes attach structured findings (G4).</summary>
    public async Task<CertificationDecision> CertifyAsync(
        CertificationRequest request,
        CancellationToken cancellationToken = default)
    {
        var decision = await CertifyCoreAsync(request, cancellationToken).ConfigureAwait(false);
        return decision.Admitted ? decision : decision with { ProbeFindings = RunProbes(request, decision) };
    }

    private IReadOnlyList<DiagnosticProbeFinding> RunProbes(
        CertificationRequest request, CertificationDecision decision)
    {
        var findings = new List<DiagnosticProbeFinding>();
        foreach (var probe in _probes.Where(p => p.FailureCheck == decision.FailureCheck))
        {
            try
            {
                if (probe.Probe(request, decision) is { } finding)
                    findings.Add(finding);
            }
            catch (Exception ex)
            {
                // A probe holds no authority: it can neither change a verdict nor break
                // the decision path. Skipped and logged.
                _logger?.LogWarning(ex, "Diagnostic probe {Probe} threw; verdict unaffected", probe.GetType().Name);
            }
        }

        return findings;
    }

    private async Task<CertificationDecision> CertifyCoreAsync(
        CertificationRequest request,
        CancellationToken cancellationToken)
    {
        var brickId = request.Brick.Id;
        var timestamp = DateTimeOffset.UtcNow;

        var contentHash = BrickContentHasher.ComputeSha256(request.SourceCode);
        var inputs = BuildInputs(request);

        // Declared ahead of the recursion refusal because the shared Fail/GatesPassedBefore
        // closures reference it; it is re-assigned with the real per-run configuration once
        // the analyzer fence actually evaluates. A recursion refusal never reads it
        // (GatesPassedBefore("recursion") is the empty prefix).
        var analyzerGatePass = new CertificationGatePass { Name = "analyzer-gate", Version = GateVersion };

        // Execution routing: every EXECUTION of candidate/mutant code goes through an
        // execution backend and the gate judges raw observations. A request may name its own
        // (the attested session); otherwise the gate replays in a bounded child process on this
        // machine. Nothing the author wrote ever runs on the certifier's own threads. The per-run
        // gate passes record where execution happened and under what budget. Declared here for
        // the same closure reason as the analyzer pass above.
        var executionConfiguration = request.ExecutionBackend is { } namedBackend
            ? $"execution={namedBackend.Describe()}"
            : $"execution={LocalProcessExecutionBackend.Identity};{_executionLimits.Describe()}";
        var correctnessPass = CorrectnessGatePass with { Configuration = executionConfiguration };
        var mutationPass = MutationGatePass with { Configuration = $"escapeRateThreshold=0;{executionConfiguration}" };
        var determinismPass = DeterminismGatePass with { Configuration = executionConfiguration };

        // Autonomy spec R4.1/R4.2: the recursion check runs before EVERYTHING — an
        // incoherent depth claim (laundering) or a candidate past the ceiling must not
        // even be analyzed. Null lineage = human-authored context (depth 0), always
        // coherent and under the ceiling.
        var lineage = request.Lineage ?? Ashlar.Core.Application.Autonomy.GenerationLineage.HumanAuthored;
        var recursionViolations = Ashlar.Core.Application.Autonomy.RecursionDiscipline.FindViolations(lineage);
        if (recursionViolations.Count > 0)
        {
            var reason = "Recursion discipline failed: " + string.Join(" | ", recursionViolations);
            _logger?.LogWarning("Certification REJECT {BrickId}: {Reason}", brickId, reason);
            return new CertificationDecision
            {
                Admitted = false,
                FailureCheck = "recursion",
                Record = Fail("recursion", reason)
            };
        }

        // Spec A1.1/I-A: the analyzer fence runs first — a candidate carrying a defect a
        // deterministic analyzer can name never reaches the expensive witness/mutation gates.
        // Its gates_passed entry is per-run because A1.5 records the evaluated-diagnostic count.
        var analyzerOutcome = await _analyzerGate.EvaluateAsync(
            request.SourceCode,
            request.CompilationReferences,
            request.ConstraintManifest,
            request.TouchSet,
            request.CompileOptions,
            cancellationToken).ConfigureAwait(false);
        analyzerGatePass = new CertificationGatePass
        {
            Name = "analyzer-gate",
            Version = GateVersion,
            Configuration = analyzerOutcome.GatePassConfiguration,
        };

        // R2.4 "furthest gate reached": the ordered prefix of gates already passed when the
        // named check failed. Local because the analyzer entry is per-run.
        IReadOnlyList<CertificationGatePass> GatesPassedBefore(string failedCheck) => failedCheck switch
        {
            "correctness" => new[] { analyzerGatePass },
            "mutation" => new[] { analyzerGatePass, correctnessPass },
            "determinism" => new[] { analyzerGatePass, correctnessPass, mutationPass },
            "dependency" => new[] { analyzerGatePass, correctnessPass, mutationPass, determinismPass },
            _ => Array.Empty<CertificationGatePass>()
        };

        CertificationRecord Fail(string check, string reason, MutationTestResult? mutation = null) =>
            BuildRecord(
                admitted: false,
                signed: false,
                status: "FAIL",
                brickId,
                timestamp,
                contentHash,
                reason,
                mutation,
                GatesPassedBefore(check),
                inputs);

        if (!analyzerOutcome.Passed)
        {
            var reason = analyzerOutcome.FormatProposerFeedback();
            _logger?.LogWarning("Certification REJECT {BrickId}: {Reason}", brickId, reason);
            return new CertificationDecision
            {
                Admitted = false,
                FailureCheck = "analyzer",
                Record = Fail("analyzer", reason)
            };
        }

        var brickTypeName = request.BrickTypeName ?? request.Brick.GetType().FullName ?? request.Brick.GetType().Name;

        // The local backend owns a scratch directory (runner, mutant images) for exactly one
        // certification; nothing in it outlives the verdict.
        using var localBackend = request.ExecutionBackend is null
            ? await LocalProcessExecutionBackend.CreateAsync(request, brickTypeName, _executionLimits, cancellationToken)
                .ConfigureAwait(false)
            : null;
        ICandidateExecutionBackend backend = request.ExecutionBackend ?? localBackend!;

        // One batched candidate execution (repeats=2) serves BOTH the witness leg (repeat 0)
        // and the determinism leg (repeat 0 vs 1 of case 0) — observed before mutation so a
        // mutant run can never poison the candidate's own evidence. Backend infrastructure
        // failures THROW (they are not candidate evidence and must never become a signed
        // verdict either way).
        var report = await backend.ExecuteAsync(
            new CandidateExecutionJob(
                new[] { new CandidateExecutionUnit("candidate", null, brickTypeName) },
                request.Witness,
                Repeats: 2),
            cancellationToken).ConfigureAwait(false);
        var candidateObservations = report.Observations.Where(o => o.UnitId == "candidate").ToArray();
        ThrowIfCandidateNeverRan(brickId, candidateObservations);

        var witnessResult = WitnessRunner.JudgeObservations(request.Witness, candidateObservations);

        if (!witnessResult.Passed)
        {
            var reason = $"Correctness check failed: {string.Join("; ", witnessResult.Failures)}";
            _logger?.LogWarning("Certification REJECT {BrickId}: {Reason}", brickId, reason);
            return new CertificationDecision
            {
                Admitted = false,
                FailureCheck = "correctness",
                Record = Fail("correctness", reason),
                WitnessFindings = witnessResult.Findings,
            };
        }

        var mutationResult = await _mutationEngine.RunAsync(
            request.SourceCode,
            brickTypeName,
            request.Witness,
            request.CompilationReferences,
            cancellationToken,
            backend,
            _analyzerGate,
            request.CompileOptions).ConfigureAwait(false);

        if (mutationResult.TotalMutants == 0)
        {
            var reason = "Mutation escape check failed: no mutants were generated from catalog";
            return new CertificationDecision
            {
                Admitted = false,
                FailureCheck = "mutation",
                Record = Fail("mutation", reason, mutationResult)
            };
        }

        if (mutationResult.EscapeRate > 0)
        {
            var reason =
                $"Mutation escape check failed: escape_rate={mutationResult.EscapeRate:F2}, survivors=[{string.Join(", ", mutationResult.SurvivingMutantIds)}]"
                + DescribeSurvivors(mutationResult)
                + DescribeWallClockKills(mutationResult);
            _logger?.LogWarning("Certification REJECT {BrickId}: {Reason}", brickId, reason);
            return new CertificationDecision
            {
                Admitted = false,
                FailureCheck = "mutation",
                Record = Fail("mutation", reason, mutationResult)
            };
        }

        var determinism = JudgeObservationDeterminism(candidateObservations);

        if (!determinism.Identical)
        {
            var reason = "Determinism check failed: outputs differ under AuditMode";
            _logger?.LogWarning("Certification REJECT {BrickId}: {Reason}", brickId, reason);
            return new CertificationDecision
            {
                Admitted = false,
                FailureCheck = "determinism",
                Record = Fail("determinism", reason, mutationResult)
            };
        }

        var dependency = BrickDependencyChecker.Check(request.ProjectPath, request.SourceCode);
        if (!dependency.Passed)
        {
            var reason = $"Dependency-cleanliness failed: {string.Join("; ", dependency.Violations)}";
            _logger?.LogWarning("Certification REJECT {BrickId}: {Reason}", brickId, reason);
            return new CertificationDecision
            {
                Admitted = false,
                FailureCheck = "dependency",
                Record = Fail("dependency", reason, mutationResult)
            };
        }

        var admittedRecord = BuildRecord(
            admitted: true,
            signed: true,
            status: "PASS",
            brickId,
            timestamp,
            contentHash,
            reason: null,
            mutation: mutationResult,
            gatesPassed: new[]
            {
                analyzerGatePass, correctnessPass, mutationPass, determinismPass, DependencyGatePass
            },
            inputs: inputs);
        admittedRecord = _signer.SignRecord(admittedRecord);

        _logger?.LogInformation(
            "Certification ADMIT {BrickId} escape_rate=0 mutants={Total} mutants_killed={Killed} killed_by_timeout={TimedOut} killed_by_crash={Crashed}",
            brickId, mutationResult.TotalMutants, mutationResult.KilledMutantIds.Count,
            mutationResult.TimedOutMutantIds.Count, mutationResult.CrashedMutantIds.Count);
        return new CertificationDecision
        {
            Admitted = true,
            Record = admittedRecord
        };
    }

    /// <summary>
    /// Refuses a candidate the backend could not LOAD. Its cases come back as throws, which the
    /// judge would score as a correctness FAIL — a signed verdict blaming the brick for the
    /// harness's own failure to run it. The runner marks the difference
    /// (<see cref="HotSwap.ExecutionRunnerMarkers.UnitLoadFailurePrefix"/>); honour it.
    /// </summary>
    private static void ThrowIfCandidateNeverRan(string brickId, IReadOnlyList<CandidateCaseObservation> observations)
    {
        var loadFailure = observations.FirstOrDefault(o =>
            o.Threw
            && o.Error is not null
            && o.Error.StartsWith(HotSwap.ExecutionRunnerMarkers.UnitLoadFailurePrefix, StringComparison.Ordinal));
        if (loadFailure is null)
            return;

        throw new CertificationHarnessException(
            $"Execution harness: the candidate '{brickId}' never ran — the execution backend could not load it "
            + $"({loadFailure.Error}). Its cases threw because the harness broke, not because the witness caught "
            + "anything, so a correctness verdict here would blame the brick for the certifier's own fault. Fix: the "
            + "candidate assembly and the request's CompilationReferences must load in the replay runner. Refusing "
            + "rather than signing a FAIL over code that was never executed.");
    }

    /// <summary>
    /// Spells out each surviving mutant: location, the edit, and the line it landed on.
    /// </summary>
    /// <remarks>
    /// A mutant id names an operator and a line, which is enough to locate a survivor and not
    /// enough to judge it. The two cases behind <c>escape_rate &gt; 0</c> need opposite
    /// responses — a weak witness wants more cases, an EQUIVALENT MUTANT (a rewrite that cannot
    /// change behaviour on any input) wants none, because no case can kill it and the candidate
    /// may be perfectly correct. Ledger S5 hit the second twice on <c>semver-parse</c>, and
    /// telling them apart meant decoding the recorded candidate by hand. The verdict is
    /// deliberately unchanged: equivalence is undecidable, so the gate still rejects and a human
    /// adjudicates — this only makes that adjudication a glance instead of an investigation.
    /// </remarks>
    private static string DescribeSurvivors(MutationTestResult mutation)
    {
        if (mutation.Survivors.Count == 0)
            return string.Empty;

        var sb = new StringBuilder();
        foreach (var survivor in mutation.Survivors)
            sb.Append("; ").Append(survivor.Describe());

        return sb.ToString();
    }

    /// <summary>
    /// Names the mutants the wall clock or a process death stopped, so a rejection's reason
    /// carries the same three-way split as the record: what the witness killed, what timed out,
    /// what crashed. Empty when there were none — most rejections.
    /// </summary>
    private static string DescribeWallClockKills(MutationTestResult mutation)
    {
        var sb = new StringBuilder();
        if (mutation.TimedOutMutantIds.Count > 0)
            sb.Append("; killed_by_timeout=[").Append(string.Join(", ", mutation.TimedOutMutantIds)).Append(']');
        if (mutation.CrashedMutantIds.Count > 0)
            sb.Append("; killed_by_crash=[").Append(string.Join(", ", mutation.CrashedMutantIds)).Append(']');
        return sb.ToString();
    }

    /// <summary>
    /// Determinism verdict over backend observations: case 0's two repeats must canonicalize
    /// identically. A missing repeat is nondeterminism-by-absence — fail-closed, never
    /// vacuously identical.
    /// </summary>
    private static (bool Identical, string? First, string? Second) JudgeObservationDeterminism(
        IReadOnlyList<CandidateCaseObservation> observations)
    {
        var first = observations.FirstOrDefault(o => o.CaseIndex == 0 && o.Repeat == 0);
        var second = observations.FirstOrDefault(o => o.CaseIndex == 0 && o.Repeat == 1);
        if (first is null && second is null)
            return (true, null, null); // No cases — same as the in-proc empty-witness path.
        if (first is null || second is null)
            return (false, first is null ? "<missing>" : "present", second is null ? "<missing>" : "present");

        var firstJson = WitnessRunner.CanonicalizeObservation(first);
        var secondJson = WitnessRunner.CanonicalizeObservation(second);
        return (firstJson == secondJson, firstJson, secondJson);
    }

    private IReadOnlyList<CertificationInput> BuildInputs(CertificationRequest request)
    {
        try
        {
            var witnessJson = JsonSerializer.Serialize(request.Witness, WitnessHashOptions);
            var inputs = new List<CertificationInput>
            {
                new CertificationInput
                {
                    Kind = "witness",
                    Id = request.Witness.BrickId,
                    Hash = BrickContentHasher.ComputeSha256(witnessJson)
                }
            };
            // The program the legs judged is the source text PLUS the options it was compiled
            // under; the content hash binds the first and this input binds the second, on PASS
            // and FAIL alike, so a reader can see which program the verdict is about.
            if (request.CompileOptions is { } compileOptions)
                inputs.Add(compileOptions.ToCertificationInput());
            inputs.AddRange(request.AdditionalInputs);
            // Where execution happened is certificate-relevant on PASS and FAIL alike:
            // a verdict minted over backend observations names the backend.
            if (request.ExecutionBackend is { } executionBackend)
            {
                inputs.Add(new CertificationInput
                {
                    Kind = "session-execution",
                    Id = executionBackend.Describe(),
                    Hash = BrickContentHasher.ComputeSha256(executionBackend.Describe()),
                });
            }
            // Autonomy R4.1: an explicitly declared lineage is recorded — depth bound to a
            // hash over the parent certificate chain — on PASS and FAIL records alike, so
            // even a refused laundering attempt leaves its claim in evidence. Requests
            // without a lineage (the human-authored default) record nothing extra.
            if (request.Lineage is { } lineage)
                inputs.Add(GenerationLineageInputs.From(lineage));
            return inputs;
        }
        catch (NotSupportedException ex)
        {
            _logger?.LogWarning(
                ex,
                "Witness spec for {BrickId} could not be serialized for input hashing; omitting the witness input",
                request.Witness.BrickId);
            return request.AdditionalInputs;
        }
    }

    private static CertificationRecord BuildRecord(
        bool admitted,
        bool signed,
        string status,
        string brickId,
        DateTimeOffset timestamp,
        string contentHash,
        string? reason,
        MutationTestResult? mutation,
        IReadOnlyList<CertificationGatePass> gatesPassed,
        IReadOnlyList<CertificationInput> inputs)
    {
        return new CertificationRecord
        {
            Status = status,
            Stage = "S0-S2",
            Admitted = admitted,
            Signed = signed,
            Timestamp = timestamp,
            BrickId = brickId,
            ContentHash = contentHash,
            EscapeRate = mutation?.EscapeRate ?? 0,
            TotalMutants = mutation?.TotalMutants ?? 0,
            SurvivingMutants = mutation?.SurvivingMutantIds.Count ?? 0,
            KilledMutants = mutation?.KilledMutantIds ?? Array.Empty<string>(),
            SurvivingMutantIds = mutation?.SurvivingMutantIds ?? Array.Empty<string>(),
            TimedOutMutants = mutation?.TimedOutMutantIds ?? Array.Empty<string>(),
            CrashedMutants = mutation?.CrashedMutantIds ?? Array.Empty<string>(),
            Reason = reason,
            Gate = "Ashlar.Infrastructure.Certification.CertificationGate",
            SchemaVersion = CertificationRecordData.CurrentSchemaVersion,
            GatesPassed = gatesPassed,
            Inputs = inputs
        };
    }
}
