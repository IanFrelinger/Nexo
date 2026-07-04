using Nexo.Core.Domain;

namespace Nexo.Infrastructure.Pipelines.Sdk.Options;

/// <summary>
/// Policy that controls how stage-level failures map to the overall pipeline
/// run state.  See <see cref="PipelineExecutionOptions.CompletionPolicy"/>
/// for usage context.
/// </summary>
public enum PipelineCompletionPolicy
{
    /// <summary>Any stage failure causes the entire run to be marked <c>Failed</c>.
    /// Recommended for production where every stage is considered critical.</summary>
    FailOnAnyStageFailure = 0,

    /// <summary>Only critical-stage failures cause a run-level failure.
    /// Non-critical stages (flagged in the pipeline definition) may fail
    /// without affecting the overall run outcome.</summary>
    AllowNonCriticalStageFailures = 1
}
