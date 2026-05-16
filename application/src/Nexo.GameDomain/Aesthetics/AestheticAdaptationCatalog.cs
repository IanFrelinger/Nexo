namespace Nexo.GameDomain.Aesthetics;

/// <summary>
/// Central registry of known pipeline kinds and engine ids for <see cref="AestheticPackValidation"/>.
/// </summary>
public static class AestheticAdaptationCatalog
{
    public static IReadOnlySet<string> KnownRenderingPipelineKinds { get; } =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            RenderingPipelineKinds.Auto,
            RenderingPipelineKinds.ForwardStylized,
            RenderingPipelineKinds.ForwardPbr,
            RenderingPipelineKinds.DeferredPbr,
            RenderingPipelineKinds.UnlitFlat,
        };

    public static IReadOnlySet<string> KnownEngineIds { get; } =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            GameEngines.Unity,
            GameEngines.Unreal,
            GameEngines.Godot,
            GameEngines.Custom,
        };

    public static bool IsKnownRenderingPipelineKind(string? value) =>
        !string.IsNullOrWhiteSpace(value) && KnownRenderingPipelineKinds.Contains(value.Trim());

    public static bool IsKnownEngineId(string? value) =>
        !string.IsNullOrWhiteSpace(value) && KnownEngineIds.Contains(value.Trim());

    public static bool IsKnownGeometryStrategy(string? value) =>
        !string.IsNullOrWhiteSpace(value) && GeometryStrategies.All.Contains(value.Trim());
}
