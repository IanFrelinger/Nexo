namespace Ashlar.Core.Application.Pipelines.Models;

/// <summary>
/// Fan-in strategy for joining predecessor stage results.
/// </summary>
public enum PipelineJoinStrategy
{
    /// <summary>Wait for all predecessors to complete before proceeding.</summary>
    All,

    /// <summary>Proceed when the first predecessor succeeds.</summary>
    FirstSuccess,

    /// <summary>Proceed when a configured quorum of predecessors succeed.</summary>
    Quorum
}
