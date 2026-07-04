using Nexo.Core.Application.Certification.Models;

namespace Nexo.Core.Application.Certification.Ports;

/// <summary>
/// Combines certification with admission into the certified composition catalog.
/// </summary>
public interface ICertifiedCompositionAdmission
{
    /// <summary>Certifies and, on success, admits the composition for runtime use.</summary>
    /// <param name="request">Composition spec, witness, and optional wiring metadata.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>Admission decision with signed certification record.</returns>
    Task<CompositionCertificationDecision> CertifyAndAdmitAsync(
        CompositionCertificationRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>Returns true when a composition id is currently admitted.</summary>
    /// <param name="compositionId">Composition identifier to check.</param>
    bool IsAdmitted(string compositionId);
}
