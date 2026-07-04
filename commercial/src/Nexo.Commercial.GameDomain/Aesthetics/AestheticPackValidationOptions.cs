namespace Nexo.Commercial.GameDomain.Aesthetics;

/// <summary>
/// Options for <see cref="AestheticPackValidation"/>.
/// </summary>
public sealed class AestheticPackValidationOptions
{
    /// <summary>When true, <see cref="AestheticPack.RenderingPipelineKind"/> must be a <see cref="AestheticAdaptationCatalog.KnownRenderingPipelineKinds"/> member.</summary>
    public bool RequireKnownRenderingPipelineKind { get; init; } = true;

    /// <summary>When true, <see cref="AestheticPack.GeometryStrategy"/> must be a <see cref="GeometryStrategies.All"/> member.</summary>
    public bool RequireKnownGeometryStrategy { get; init; } = true;

    /// <summary>When true, each <see cref="EngineRenderingSurfaceBinding.EngineId"/> must be a <see cref="AestheticAdaptationCatalog.KnownEngineIds"/> member.</summary>
    public bool RequireKnownEngineIds { get; init; }

    /// <summary>When true, informational issues for undocumented <see cref="EngineRenderingSurfaceBinding.Role"/> or <see cref="EngineRenderingSurfaceBinding.MaterialSurfaceId"/>.</summary>
    public bool WarnOnUndocumentedRolesAndSurfaces { get; init; } = true;
}
