namespace Nexo.Commercial.GameDomain.Mapping;
/// <summary>
/// Outcome for a single pipeline stage.
/// </summary>
public sealed record MapPipelineStageResult(
    string Stage,
    string Status,
    string? Detail);
