using Nexo.Core.Application.Adaptation.Models;

namespace Nexo.Core.Application.Adaptation.Ports;

/// <summary>
/// Sealed model seam for generating brick implementation source from intent + I/O contract.
/// Witness cases are NOT provided — only the signature (names/types).
/// </summary>
public interface IGeneratorModel
{
    /// <summary>
    /// Generates untrusted brick source from intent and I/O signature.
    /// </summary>
    /// <param name="intent">Brick generation intent (no witness cases).</param>
    /// <param name="witnessSignature">I/O field names and types only.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>Generated source with provenance metadata.</returns>
    Task<GeneratedBrickSource> GenerateAsync(
        IntentSpec intent,
        WitnessSignature witnessSignature,
        CancellationToken cancellationToken = default);
}
