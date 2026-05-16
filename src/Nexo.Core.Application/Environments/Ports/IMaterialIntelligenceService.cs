using Nexo.Core.Application.Environments;

namespace Nexo.Core.Application.Environments.Ports;

/// <summary>
/// Produces material specs and texture-generation prompts for the engine to bake assets
/// (material descriptors in the game domain). Optional diffusion happens outside Nexo;
/// this port returns structured hints only unless an implementation attaches URIs in prompts.
/// </summary>
public interface IMaterialIntelligenceService
{
    Task<MaterialIntelligenceResult> SuggestMaterialsAsync(
        MaterialIntelligenceRequest request,
        CancellationToken cancellationToken = default);
}
