namespace Nexo.GameDomain.Mapping;

/// <summary>
/// Describes how a host should adapt geographic vector data and terrain for the active Forge aesthetic.
/// Returned by <see cref="MapAdaptationPlanner"/> for Unity or other runtimes to branch pipelines.
/// </summary>
public sealed record MapAdaptationPlan(
    string ActiveAestheticId,
    string GeometryStrategy,
    string EffectiveMapRenderingProfile,
    bool ProfileWasInferred,
    IReadOnlyList<string> PipelineStages,
    string? Notes);
