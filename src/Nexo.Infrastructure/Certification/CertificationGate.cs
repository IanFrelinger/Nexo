using Microsoft.Extensions.Logging;
using Nexo.Certification.Contracts;
using Nexo.Core.Application.Certification.Models;
using Nexo.Core.Application.Certification.Ports;

namespace Nexo.Infrastructure.Certification;

/// <summary>Certification gate: mutation testing, dependency checks, signing, and admit/reject decisions.</summary>
public sealed class CertificationGate : ICertificationGate
{
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

        CertificationRecord Fail(string check, string reason, MutationTestResult? mutation = null) =>
            BuildRecord(
                admitted: false,
                signed: false,
                status: "FAIL",
                brickId,
                timestamp,
                contentHash,
                reason,
                mutation);

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
            mutation: mutationResult);
        admittedRecord = admittedRecord with { Signature = _signer.Sign(admittedRecord) };

        _logger?.LogInformation("Certification ADMIT {BrickId} escape_rate=0", brickId);
        return new CertificationDecision
        {
            Admitted = true,
            Record = admittedRecord
        };
    }

    private static CertificationRecord BuildRecord(
        bool admitted,
        bool signed,
        string status,
        string brickId,
        DateTimeOffset timestamp,
        string contentHash,
        string? reason,
        MutationTestResult? mutation)
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
            Gate = "Nexo.Infrastructure.Certification.CertificationGate"
        };
    }
}
