using Nexo.Core.Application.Adaptation.Models;
using Nexo.Core.Application.Certification.Models;

namespace Nexo.Core.Application.Adaptation.Ports;

/// <summary>
/// Generates untrusted source, certifies against an independent witness, admits only on gate PASS.
/// </summary>
public interface IGenerateAndCertifyService
{
    /// <summary>
    /// Generates brick source from intent, certifies against witness, and admits on success.
    /// </summary>
    /// <param name="intent">Human or programmatic intent for brick generation (no witness cases).</param>
    /// <param name="witness">Independent witness for certification.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>Manifest, certification decision, and optional brick type name.</returns>
    Task<GenerateCertifyResult> GenerateCertifyAndAdmitAsync(
        IntentSpec intent,
        WitnessSpec witness,
        CancellationToken cancellationToken = default);
}
