using Nexo.Core.Application.Certification.Models;

namespace Nexo.Core.Application.Certification.Ports;

/// <summary>
/// Certification gate that evaluates a composition and returns an admit/reject decision.
/// </summary>
public interface ICompositionCertificationGate
{
    /// <summary>Runs certification checks against a composition request.</summary>
    /// <param name="request">Composition spec, witness, and optional wiring metadata.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>Admission decision with signed certification record.</returns>
    Task<CompositionCertificationDecision> CertifyAsync(
        CompositionCertificationRequest request,
        CancellationToken cancellationToken = default);
}
