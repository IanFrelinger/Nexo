namespace Ashlar.Core.Application.Pipelines.Models;

/// <summary>
/// Constraint set for an individual stage.
/// </summary>
public sealed record PipelineStageConstraints
{
    /// <summary>Maximum concurrent executions allowed for this stage.</summary>
    public int? MaxParallelism { get; init; }

    /// <summary>When true, only one instance of this stage may run at a time.</summary>
    public bool ForceSerialized { get; init; }

    /// <summary>When true, failure of this stage fails the entire pipeline run.</summary>
    public bool Critical { get; init; }

    /// <summary>When true, output must pass post-validation before the stage is marked complete.</summary>
    public bool RequiresPostValidation { get; init; }
}
