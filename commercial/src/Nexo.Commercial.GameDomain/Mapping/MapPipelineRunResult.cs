namespace Nexo.GameDomain.Mapping;

/// <summary>
/// Aggregate result of a map pipeline run.
/// </summary>
public sealed record MapPipelineRunResult(
    bool Success,
    IReadOnlyList<MapPipelineStageResult> Stages,
    string? Error);
