namespace Nexo.Commercial.GameDomain.Aesthetics;
/// <summary>
/// Central registry of known pipeline kinds and engine ids for <see cref="AestheticPackValidation"/>.
/// </summary>
public static class AestheticAdaptationCatalog
{
    /// <summary>Known rendering pipeline kinds value.</summary>
    public static IReadOnlySet<string> KnownRenderingPipelineKinds { get; } =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            RenderingPipelineKinds.Auto,
            RenderingPipelineKinds.ForwardStylized,
            RenderingPipelineKinds.ForwardPbr,
            RenderingPipelineKinds.DeferredPbr,
            RenderingPipelineKinds.UnlitFlat,
        };

    /// <summary>known engine ids value.</summary>
    public static IReadOnlySet<string> KnownEngineIds { get; } =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            GameEngines.Unity,
            GameEngines.Unreal,
            GameEngines.Godot,
            GameEngines.Custom,
        };

    /// <summary>Is known rendering pipeline kind operation.</summary>
    public static bool IsKnownRenderingPipelineKind(string? value) =>
        !string.IsNullOrWhiteSpace(value) && KnownRenderingPipelineKinds.Contains(value.Trim());

    /// <summary>Is known engine id operation.</summary>
    public static bool IsKnownEngineId(string? value) =>
        !string.IsNullOrWhiteSpace(value) && KnownEngineIds.Contains(value.Trim());

    /// <summary>Is known geometry strategy operation.</summary>
    public static bool IsKnownGeometryStrategy(string? value) =>
        !string.IsNullOrWhiteSpace(value) && GeometryStrategies.All.Contains(value.Trim());
}
