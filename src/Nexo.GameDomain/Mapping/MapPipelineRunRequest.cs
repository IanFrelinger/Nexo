namespace Nexo.GameDomain.Mapping;

/// <summary>
/// Request to execute (or simulate) the map adaptation pipeline.
/// </summary>
public sealed record MapPipelineRunRequest(
    bool DryRun = true,
    int TimeoutMs = 30_000);
