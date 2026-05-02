using Nexo.Core.Application.Environments;
using Nexo.Core.Application.Environments.Ports;

namespace Nexo.Infrastructure.Environments;

/// <summary>No-op material suggestions (hosts supply diffusion / catalog implementations).</summary>
public sealed class NoOpMaterialIntelligenceService : IMaterialIntelligenceService
{
    public Task<MaterialIntelligenceResult> SuggestMaterialsAsync(
        MaterialIntelligenceRequest request,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(new MaterialIntelligenceResult(Array.Empty<SuggestedMaterialSpec>(), Summary: null));
}
