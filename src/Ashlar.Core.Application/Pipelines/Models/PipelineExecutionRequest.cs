namespace Ashlar.Core.Application.Pipelines.Models;

/// <summary>
/// Input request for executing or resuming a pipeline run.
/// </summary>
public sealed record PipelineExecutionRequest
{
    /// <summary>Optional run identifier for a new run; generated if omitted.</summary>
    public string? RunId { get; init; }

    /// <summary>Existing run identifier to resume from persisted state.</summary>
    public string? ResumeRunId { get; init; }

    /// <summary>When resuming, whether to retry stages that previously failed.</summary>
    public bool ResumeFailedStages { get; init; }

    /// <summary>Run-level named inputs passed to entry stages.</summary>
    public IReadOnlyDictionary<string, string> Inputs { get; init; } = new Dictionary<string, string>();

    /// <summary>Pipeline template to execute or resume.</summary>
    public required PipelineTemplate Template { get; init; }
}
