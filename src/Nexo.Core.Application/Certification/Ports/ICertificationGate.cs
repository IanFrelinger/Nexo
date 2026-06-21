using Nexo.Core.Application.Certification.Models;

namespace Nexo.Core.Application.Certification.Ports;

/// <summary>
/// Operational brick certification gate (S0–S2 v0).
/// </summary>
public interface ICertificationGate
{
    Task<CertificationDecision> CertifyAsync(
        CertificationRequest request,
        CancellationToken cancellationToken = default);
}
