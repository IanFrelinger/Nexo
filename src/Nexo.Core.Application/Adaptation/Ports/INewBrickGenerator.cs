using Nexo.Core.Application.Adaptation.Models;

namespace Nexo.Core.Application.Adaptation.Ports;

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
}
