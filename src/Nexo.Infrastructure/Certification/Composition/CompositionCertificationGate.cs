using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Certification.Models;
using Nexo.Core.Application.Certification.Ports;
using Nexo.Core.Domain.Execution;

namespace Nexo.Infrastructure.Certification.Composition;

public sealed class CompositionCertificationGate : ICompositionCertificationGate
{
    private readonly ICertificationRecordStore _brickCertificationStore;
    private readonly CertificationRecordSigner _brickSigner;
    private readonly IBrickRegistry _brickRegistry;
    private readonly CompositionCertificationRecordSigner _compositionSigner;
    private readonly CompositionGraphMutationEngine _mutationEngine = new();
    private readonly CompositionExecutor _executor = new();
    private readonly ILogger<CompositionCertificationGate>? _logger;

    public CompositionCertificationGate(
        ICertificationRecordStore brickCertificationStore,
        CertificationRecordSigner brickSigner,
        IBrickRegistry brickRegistry,
        CompositionCertificationRecordSigner compositionSigner,
        ILogger<CompositionCertificationGate>? logger = null)
    {
        _brickCertificationStore = brickCertificationStore ?? throw new ArgumentNullException(nameof(brickCertificationStore));
        _brickSigner = brickSigner ?? throw new ArgumentNullException(nameof(brickSigner));
        _brickRegistry = brickRegistry ?? throw new ArgumentNullException(nameof(brickRegistry));
        _compositionSigner = compositionSigner ?? throw new ArgumentNullException(nameof(compositionSigner));
        _logger = logger;
    }

    public async Task<CompositionCertificationDecision> CertifyAsync(
        CompositionCertificationRequest request,
        CancellationToken cancellationToken = default)
    {
        var compositionId = request.Spec.CompositionId;
        var timestamp = DateTimeOffset.UtcNow;

        CompositionCertificationRecord Fail(
            string check,
            string reason,
            CompositionMutationTestResult? mutation = null) =>
            BuildRecord(false, false, "FAIL", compositionId, timestamp, reason, mutation);

        var constituents = CompositionConstituentChecker.Check(
            request.Spec,
            _brickCertificationStore,
            _brickSigner);
        if (!constituents.Passed)
        {
            var reason = $"Constituent integrity failed: {string.Join("; ", constituents.Violations)}";
            return Reject("constituents", reason, Fail("constituents", reason));
        }

        var seam = CompositionSeamChecker.Check(request.Spec, _brickRegistry);
        if (!seam.Passed)
        {
            var reason = $"Seam contract failed: {string.Join("; ", seam.Violations)}";
            return Reject("seam", reason, Fail("seam", reason));
        }

        var witnessResult = await _executor.RunWitnessAsync(
            request.Spec,
            request.Witness,
            _brickRegistry,
            new AuditExecutionContext(),
            cancellationToken).ConfigureAwait(false);

        if (!witnessResult.Passed)
        {
            var reason = $"Correctness check failed: {string.Join("; ", witnessResult.Failures)}";
            return Reject("correctness", reason, Fail("correctness", reason));
        }

        var mutationResult = await _mutationEngine.RunAsync(
            request.Spec,
            request.Witness,
            _brickRegistry,
            cancellationToken).ConfigureAwait(false);

        if (mutationResult.TotalMutants == 0)
        {
            var reason =
                "Graph-mutation escape check failed: no structural mutants — composition too trivial or witness shape inadequate";
            return Reject("mutation", reason, Fail("mutation", reason, mutationResult));
        }

        if (mutationResult.EscapeRate > 0)
        {
            var reason =
                $"Graph-mutation escape check failed: composition_escape_rate={mutationResult.EscapeRate:F2}, survivors=[{string.Join(", ", mutationResult.SurvivingMutantIds)}]";
            return Reject("mutation", reason, Fail("mutation", reason, mutationResult));
        }

        var determinism = await _executor.CheckDeterminismAsync(
            request.Spec,
            request.Witness,
            _brickRegistry,
            cancellationToken).ConfigureAwait(false);

        if (!determinism.Identical)
        {
            var reason = "Determinism check failed: composition outputs differ under AuditMode";
            return Reject("determinism", reason, Fail("determinism", reason, mutationResult));
        }

        var dependency = CompositionDependencyChecker.Check(request.WiringMetadata);
        if (!dependency.Passed)
        {
            var reason = $"Dependency-cleanliness failed: {string.Join("; ", dependency.Violations)}";
            return Reject("dependency", reason, Fail("dependency", reason, mutationResult));
        }

        var admittedRecord = BuildRecord(true, true, "PASS", compositionId, timestamp, null, mutationResult);
        admittedRecord = admittedRecord with { Signature = _compositionSigner.Sign(admittedRecord) };

        _logger?.LogInformation("Composition certification ADMIT {CompositionId} composition_escape_rate=0", compositionId);
        return new CompositionCertificationDecision
        {
            Admitted = true,
            Record = admittedRecord
        };
    }

    private CompositionCertificationDecision Reject(
        string check,
        string reason,
        CompositionCertificationRecord record)
    {
        _logger?.LogWarning("Composition certification REJECT: {Reason}", reason);
        return new CompositionCertificationDecision
        {
            Admitted = false,
            FailureCheck = check,
            Record = record
        };
    }

    private static CompositionCertificationRecord BuildRecord(
        bool admitted,
        bool signed,
        string status,
        string compositionId,
        DateTimeOffset timestamp,
        string? reason,
        CompositionMutationTestResult? mutation)
    {
        return new CompositionCertificationRecord
        {
            Status = status,
            Stage = "S0-S2-composition",
            Admitted = admitted,
            Signed = signed,
            Timestamp = timestamp,
            CompositionId = compositionId,
            CompositionEscapeRate = mutation?.EscapeRate ?? 0,
            TotalStructuralMutants = mutation?.TotalMutants ?? 0,
            SurvivingStructuralMutants = mutation?.SurvivingMutantIds.Count ?? 0,
            KilledStructuralMutantIds = mutation?.KilledMutantIds ?? Array.Empty<string>(),
            SurvivingStructuralMutantIds = mutation?.SurvivingMutantIds ?? Array.Empty<string>(),
            Reason = reason,
            Gate = "Nexo.Infrastructure.Certification.Composition.CompositionCertificationGate"
        };
    }
}
