using Nexo.Core.Application.Certification.Models;

namespace Nexo.Core.Application.Certification.Ports;

/// <summary>
/// Operational brick certification gate (S0–S2 v0).
/// </summary>
public interface ICertificationGate
{
    /// <summary>Runs certification checks against a brick request.</summary>
    /// <param name="request">Brick, witness, source code, and compilation context.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>Admission decision with signed certification record.</returns>
    Task<CertificationDecision> CertifyAsync(
        CertificationRequest request,
        CancellationToken cancellationToken = default);
}
