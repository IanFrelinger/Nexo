using Nexo.Core.Application.Adaptation.Models;
using Nexo.Core.Application.Certification.Models;

namespace Nexo.Core.Application.Adaptation.Ports;

/// <summary>
/// Result of a single generate→certify→admit attempt (v0: no silent retry loop).
/// </summary>
public sealed record GenerateCertifyResult(
    BrickManifest? Manifest,
    CertificationDecision Decision,
    string? BrickTypeName);

/// <summary>
/// Generates untrusted source, certifies against an independent witness, admits only on gate PASS.
/// </summary>
public interface IGenerateAndCertifyService
{
    Task<GenerateCertifyResult> GenerateCertifyAndAdmitAsync(
        IntentSpec intent,
        WitnessSpec witness,
        CancellationToken cancellationToken = default);
}
