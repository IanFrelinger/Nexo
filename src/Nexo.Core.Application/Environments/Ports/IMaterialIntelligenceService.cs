using Nexo.Core.Application.Environments;

namespace Nexo.Core.Application.Environments.Ports;

/// <summary>
/// Produces material specs and texture-generation prompts for the engine to bake assets
/// (material descriptors in the game domain). Optional diffusion happens outside Nexo;
/// this port returns structured hints only unless an implementation attaches URIs in prompts.
/// </summary>
public interface IMaterialIntelligenceService
{
    /// <summary>
    /// Suggests materials and texture-generation hints for the given style and tags.
    /// </summary>
    /// <param name="request">Style preset, OSM tags, and output cap.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>Batch of suggested material specifications.</returns>
    Task<MaterialIntelligenceResult> SuggestMaterialsAsync(
        MaterialIntelligenceRequest request,
        CancellationToken cancellationToken = default);
}
