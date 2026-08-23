namespace Ashlar.Core.Application.Environments.Ports;

using Ashlar.Core.Application.Environments;

/// <summary>
/// Embedded AI or heuristic refinement for messy open vector inputs (OSM tags, broken geometries).
/// Default registration can pass-through; hosts swap for an implementation backed by the Ashlar language model (<c>Ashlar.Abstractions.IModel</c>).
/// </summary>
public interface IVectorMapIntelligenceService
{
    /// <summary>
    /// Refines raw vector map data by repairing tags, geometries, and format inconsistencies.
    /// </summary>
    /// <param name="request">Raw payload, format hint, and output size cap.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>Refined payload with modification notes.</returns>
    Task<VectorMapIntelligenceResult> RefineAsync(
        VectorMapIntelligenceRequest request,
        CancellationToken cancellationToken = default);
}
