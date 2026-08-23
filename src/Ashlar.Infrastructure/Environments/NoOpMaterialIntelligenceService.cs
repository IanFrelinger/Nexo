using Ashlar.Core.Application.Environments;
using Ashlar.Core.Application.Environments.Ports;

namespace Ashlar.Infrastructure.Environments;

/// <summary>No-op material suggestions (hosts supply diffusion / catalog implementations).</summary>
public sealed class NoOpMaterialIntelligenceService : IMaterialIntelligenceService
{
    /// <summary>Suggest materials asynchronously.</summary>
    public Task<MaterialIntelligenceResult> SuggestMaterialsAsync(
        MaterialIntelligenceRequest request,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(new MaterialIntelligenceResult(Array.Empty<SuggestedMaterialSpec>(), Summary: null));
}
