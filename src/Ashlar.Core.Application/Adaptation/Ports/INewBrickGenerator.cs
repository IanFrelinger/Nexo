using Ashlar.Core.Application.Adaptation.Models;

namespace Ashlar.Core.Application.Adaptation.Ports;

/// <summary>
/// Generates new bricks from scratch given an observed pattern.
/// </summary>
public interface INewBrickGenerator
{
    /// <summary>
    /// Generate a new brick manifest for the given pattern.
    /// </summary>
    /// <param name="patternType">Observed pattern type (e.g. repeated-edits, edit-then-build).</param>
    /// <param name="patternMetadata">Pattern metadata (paths, intervals, etc.).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<BrickManifest> GenerateAsync(
        string patternType,
        IReadOnlyDictionary<string, object>? patternMetadata = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generate a brick manifest from arbitrary intent using the sealed model seam.
    /// Witness signature provides I/O contract only — not witness cases.
    /// </summary>
    Task<BrickManifest> GenerateFromIntentAsync(
        IntentSpec intent,
        WitnessSignature witnessSignature,
        CancellationToken cancellationToken = default);
}
