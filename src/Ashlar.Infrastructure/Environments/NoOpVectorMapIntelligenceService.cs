using Ashlar.Core.Application.Environments;
using Ashlar.Core.Application.Environments.Ports;

namespace Ashlar.Infrastructure.Environments;

/// <summary>
/// Pass-through refinement (no AI). Swap <see cref="IVectorMapIntelligenceService"/> for an AI-backed implementation.
/// </summary>
public sealed class NoOpVectorMapIntelligenceService : IVectorMapIntelligenceService
{
    /// <summary>Refine asynchronously.</summary>
    public Task<VectorMapIntelligenceResult> RefineAsync(
        VectorMapIntelligenceRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = new VectorMapIntelligenceResult(
            request.RawPayload,
            request.FormatHint,
            Notes: null,
            WasModified: false);
        return Task.FromResult(result);
    }
}
