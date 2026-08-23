using Ashlar.Core.Application.Certification.Models;
using Ashlar.Core.Domain.Bricks;

namespace Ashlar.Core.Application.Certification.Ports;

/// <summary>
/// Sole admission path for bricks into the certified registry.
/// </summary>
public interface ICertifiedBrickAdmission
{
    /// <summary>Certifies a brick and admits it on success.</summary>
    /// <param name="request">Brick, witness, source code, and compilation context.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>Admission decision with signed certification record.</returns>
    Task<CertificationDecision> CertifyAndAdmitAsync(
        CertificationRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>Returns true when a brick id is currently admitted.</summary>
    /// <param name="brickId">Brick identifier to check.</param>
    bool IsAdmitted(string brickId);
}
