using System.Text.Json;
using Microsoft.Extensions.Logging;
using Nexo.Certification.Contracts;
using Nexo.Core.Application.Certification.Models;
using Nexo.Core.Application.Certification.Ports;

namespace Nexo.Infrastructure.Certification;

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

    /// <summary>Initializes a new certification gate.</summary>
    public CertificationGate(
        CertificationRecordSigner signer,
        ILogger<CertificationGate>? logger = null,
        AnalyzerFenceGate? analyzerGate = null)
    {
        _signer = signer ?? throw new ArgumentNullException(nameof(signer));
        _logger = logger;
        // Defaulting the gate on (rather than null-meaning-skip) keeps the chain fail-closed:
        // no construction path exists that certifies without the analyzer fence.
        _analyzerGate = analyzerGate ?? new AnalyzerFenceGate();
    }

    /// <summary>Certify asynchronously.</summary>
    public async Task<CertificationDecision> CertifyAsync(
        CertificationRequest request,
        CancellationToken cancellationToken = default)
    {
        var brickId = request.Brick.Id;
        var timestamp = DateTimeOffset.UtcNow;

        var contentHash = BrickContentHasher.ComputeSha256(request.SourceCode);
        var inputs = BuildInputs(request);

        // Spec A1.1/I-A: the analyzer fence runs first — a candidate carrying a defect a
        // deterministic analyzer can name never reaches the expensive witness/mutation gates.
        // Its gates_passed entry is per-run because A1.5 records the evaluated-diagnostic count.
        var analyzerOutcome = await _analyzerGate.EvaluateAsync(
            request.SourceCode,
            request.CompilationReferences,
            request.ConstraintManifest,
            cancellationToken).ConfigureAwait(false);
        var analyzerGatePass = new CertificationGatePass
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
            "mutation" => new[] { analyzerGatePass, CorrectnessGatePass },
            "determinism" => new[] { analyzerGatePass, CorrectnessGatePass, MutationGatePass },
            "dependency" => new[] { analyzerGatePass, CorrectnessGatePass, MutationGatePass, DeterminismGatePass },
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

        var witnessResult = await WitnessRunner.RunAsync(
            request.Brick,
            request.Witness,
            new AuditExecutionContext(),
            cancellationToken).ConfigureAwait(false);

        if (!witnessResult.Passed)
        {
            var reason = $"Correctness check failed: {string.Join("; ", witnessResult.Failures)}";
            _logger?.LogWarning("Certification REJECT {BrickId}: {Reason}", brickId, reason);
            return new CertificationDecision
            {
                Admitted = false,
                FailureCheck = "correctness",
                Record = Fail("correctness", reason)
            };
        }

        var brickTypeName = request.BrickTypeName ?? request.Brick.GetType().FullName ?? request.Brick.GetType().Name;
        var mutationResult = await _mutationEngine.RunAsync(
            request.SourceCode,
            brickTypeName,
            request.Witness,
            request.CompilationReferences,
            cancellationToken).ConfigureAwait(false);

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
                $"Mutation escape check failed: escape_rate={mutationResult.EscapeRate:F2}, survivors=[{string.Join(", ", mutationResult.SurvivingMutantIds)}]";
            _logger?.LogWarning("Certification REJECT {BrickId}: {Reason}", brickId, reason);
            return new CertificationDecision
            {
                Admitted = false,
                FailureCheck = "mutation",
                Record = Fail("mutation", reason, mutationResult)
            };
        }

        var determinism = await WitnessRunner.CheckDeterminismAsync(
            request.Brick,
            request.Witness,
            cancellationToken).ConfigureAwait(false);

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
                analyzerGatePass, CorrectnessGatePass, MutationGatePass, DeterminismGatePass, DependencyGatePass
            },
            inputs: inputs);
        admittedRecord = _signer.SignRecord(admittedRecord);

        _logger?.LogInformation("Certification ADMIT {BrickId} escape_rate=0", brickId);
        return new CertificationDecision
        {
            Admitted = true,
            Record = admittedRecord
        };
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
            inputs.AddRange(request.AdditionalInputs);
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
            Reason = reason,
            Gate = "Nexo.Infrastructure.Certification.CertificationGate",
            SchemaVersion = CertificationRecordData.TrustLoopSchemaVersion,
            GatesPassed = gatesPassed,
            Inputs = inputs
        };
    }
}
