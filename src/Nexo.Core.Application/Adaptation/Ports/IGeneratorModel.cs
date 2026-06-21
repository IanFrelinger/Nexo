using Nexo.Core.Application.Adaptation.Models;

namespace Nexo.Core.Application.Adaptation.Ports;

/// <summary>
/// Sealed model seam for generating brick implementation source from intent + I/O contract.
/// Witness cases are NOT provided — only the signature (names/types).
/// </summary>
public interface IGeneratorModel
{
    Task<GeneratedBrickSource> GenerateAsync(
        IntentSpec intent,
        WitnessSignature witnessSignature,
        CancellationToken cancellationToken = default);
}
