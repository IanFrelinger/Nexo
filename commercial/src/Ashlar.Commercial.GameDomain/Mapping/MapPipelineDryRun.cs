namespace Ashlar.Commercial.GameDomain.Mapping;
/// <summary>
/// Simulates map pipeline stages without performing network I/O or heavy geometry work.
/// Hosts replace this with real orchestration.
/// </summary>
public static class MapPipelineDryRun
{
    /// <summary>Execute operation.</summary>
    public static MapPipelineRunResult Execute(MapAdaptationPlan plan, MapPipelineRunRequest request)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(request);

        if (request.TimeoutMs <= 0)
            return new MapPipelineRunResult(false, [],
                "TimeoutMs must be positive.");

        var stages = new List<MapPipelineStageResult>();
        foreach (var stage in plan.PipelineStages)
        {
            var detail = request.DryRun
                ? "Simulated (dry run); no data fetched."
                : "Non-dry execution is not implemented in the reference host.";
            stages.Add(new MapPipelineStageResult(stage, request.DryRun ? "simulated_ok" : "not_implemented", detail));
        }

        var success = request.DryRun || stages.All(s => s.Status == "ok");
        return new MapPipelineRunResult(success, stages,
            request.DryRun ? null : "Reference pipeline only supports dryRun=true.");
    }
}
