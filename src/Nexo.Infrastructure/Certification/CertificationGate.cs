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

    private static readonly IReadOnlyList<CertificationGatePass> AllGatePasses =
        new[] { CorrectnessGatePass, MutationGatePass, DeterminismGatePass, DependencyGatePass };

    private static readonly JsonSerializerOptions WitnessHashOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    private readonly BrickMutationEngine _mutationEngine = new();
    private readonly CertificationRecordSigner _signer;
    private readonly ILogger<CertificationGate>? _logger;

    /// <summary>Initializes a new certification gate.</summary>
    public CertificationGate(
        CertificationRecordSigner signer,
        ILogger<CertificationGate>? logger = null)
    {
        _signer = signer ?? throw new ArgumentNullException(nameof(signer));
        _logger = logger;
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
            gatesPassed: AllGatePasses,
            inputs: inputs);
        admittedRecord = _signer.SignRecord(admittedRecord);

        _logger?.LogInformation("Certification ADMIT {BrickId} escape_rate=0", brickId);
        return new CertificationDecision
        {
            Admitted = true,
            Record = admittedRecord
        };
    }

    /// <summary>Gates that had already passed when the named check failed (R2.4 "furthest gate reached").</summary>
    private static IReadOnlyList<CertificationGatePass> GatesPassedBefore(string failedCheck) => failedCheck switch
    {
        "mutation" => new[] { CorrectnessGatePass },
        "determinism" => new[] { CorrectnessGatePass, MutationGatePass },
        "dependency" => new[] { CorrectnessGatePass, MutationGatePass, DeterminismGatePass },
        _ => Array.Empty<CertificationGatePass>()
    };

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
