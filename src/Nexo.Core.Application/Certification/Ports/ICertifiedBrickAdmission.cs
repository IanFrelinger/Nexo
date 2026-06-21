using Nexo.Core.Application.Certification.Models;
using Nexo.Core.Domain.Bricks;

namespace Nexo.Core.Application.Certification.Ports;

/// <summary>
/// Sole admission path for bricks into the certified registry.
/// </summary>
public interface ICertifiedBrickAdmission
{
    Task<CertificationDecision> CertifyAndAdmitAsync(
        CertificationRequest request,
        CancellationToken cancellationToken = default);

    bool IsAdmitted(string brickId);
}
