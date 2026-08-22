namespace Ashlar.Core.Application.Pipelines.Models;

/// <summary>
/// Runtime state of a stage execution.
/// </summary>
public enum PipelineStageRunState
{
    /// <summary>Stage is waiting for predecessors or dispatch.</summary>
    Pending,

    /// <summary>Stage is currently executing on a worker.</summary>
    Running,

    /// <summary>Stage finished successfully.</summary>
    Completed,

    /// <summary>Stage failed and may be retried depending on policy.</summary>
    Failed
}
