namespace Nexo.Core.Application.Pipelines.Models;

/// <summary>
/// Pipeline run aggregate.
/// </summary>
public sealed record PipelineRun
{
    /// <summary>Unique identifier for this run instance.</summary>
    public string RunId { get; init; } = string.Empty;

    /// <summary>Template identifier that produced this run.</summary>
    public string TemplateId { get; init; } = string.Empty;

    /// <summary>Overall lifecycle state of the run.</summary>
    public PipelineRunState State { get; init; } = PipelineRunState.Pending;

    /// <summary>UTC timestamp when the run started.</summary>
    public DateTimeOffset StartedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>UTC timestamp when the run finished, if completed or failed.</summary>
    public DateTimeOffset? CompletedAt { get; init; }

    /// <summary>Per-stage execution records for this run.</summary>
    public IReadOnlyList<PipelineStageRun> StageRuns { get; init; } = Array.Empty<PipelineStageRun>();
}
