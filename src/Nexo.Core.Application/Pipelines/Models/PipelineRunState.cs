namespace Nexo.Core.Application.Pipelines.Models;

/// <summary>
/// Runtime state of a pipeline run.
/// </summary>
public enum PipelineRunState
{
    /// <summary>Run has been created but not yet started.</summary>
    Pending,

    /// <summary>Run is actively executing stages.</summary>
    Running,

    /// <summary>All stages completed successfully.</summary>
    Completed,

    /// <summary>Run terminated due to one or more stage failures.</summary>
    Failed
}
