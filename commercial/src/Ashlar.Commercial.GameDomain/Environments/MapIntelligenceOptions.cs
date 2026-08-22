namespace Ashlar.Commercial.GameDomain.Environments;
/// <summary>
/// Optional toggles for embedded AI assistance on map ingestion (resolved by host; defaults off).
/// </summary>
public sealed record MapIntelligenceOptions
{
    /// <summary>Use <see cref="Ashlar.Core.Application.Environments.Ports.IVectorMapIntelligenceService"/> after fetch.</summary>
    public bool RefineVectorWithEmbeddedAi { get; init; }

    /// <summary>Use <see cref="Ashlar.Core.Application.Environments.Ports.IMaterialIntelligenceService"/> for facade/road materials.</summary>
    public bool SuggestMaterialsWithEmbeddedAi { get; init; }

    /// <summary>Run <see cref="Ashlar.Core.Application.Environments.Ports.IMapVerificationService"/> per LOD tier.</summary>
    public bool VerifyPerLodTier { get; init; }

    /// <summary>When true, finer tiers run consistency checks against parent tier reports.</summary>
    public bool TilingDownVerification { get; init; }
}
