namespace Nexo.Commercial.GameDomain.Mapping;
/// <summary>
/// Describes how a host should adapt map data for the active aesthetic.
/// </summary>
public sealed record MapAdaptationPlan(
    string EffectiveMapRenderingProfile,
    string GeometryStrategy,
    IReadOnlyList<string> PipelineStages,
    IReadOnlyList<string> Notes);
