namespace Nexo.Core.Application.Environments.Ports;

using Nexo.Core.Application.Environments;

/// <summary>
/// Embedded AI or heuristic refinement for messy open vector inputs (OSM tags, broken geometries).
/// Default registration can pass-through; hosts swap for an implementation backed by the Nexo language model (<c>Nexo.Abstractions.IModel</c>).
/// </summary>
public interface IVectorMapIntelligenceService
{
    Task<VectorMapIntelligenceResult> RefineAsync(
        VectorMapIntelligenceRequest request,
        CancellationToken cancellationToken = default);
}
