namespace Nexo.Infrastructure.Pipelines;

/// <summary>
/// Runtime controls for pipeline execution behavior.
/// </summary>
public sealed class PipelineExecutionOptions
{
    /// <summary>
    /// Configuration section name.
    /// </summary>
    public const string SectionName = "Nexo:Pipelines:Execution";

    /// <summary>
    /// Maximum retry attempts for failed stages.
    /// </summary>
    public int MaxRetryAttempts { get; set; } = 2;

    /// <summary>
    /// Delay in milliseconds before retrying a failed stage.
    /// </summary>
    public int RetryDelayMs { get; set; } = 100;

    /// <summary>
    /// Whether failed stages should be resumed from a prior run.
    /// </summary>
    public bool ResumeFailedStages { get; set; } = true;

    /// <summary>
    /// Controls how final run state is derived when at least one stage failed.
    /// </summary>
    public PipelineCompletionPolicy CompletionPolicy { get; set; } = PipelineCompletionPolicy.FailOnAnyStageFailure;

    /// <summary>
    /// If true, missing resume source run id does not fail execution and a fresh run starts.
    /// </summary>
    public bool AllowMissingResumeSource { get; set; }

    /// <summary>
    /// Enables test-only behavior hooks in stage executors.
    /// Keep false in production.
    /// </summary>
    public bool EnableTestHooks { get; set; }
}

/// <summary>
/// Policy for determining terminal run state with stage failures.
/// </summary>
public enum PipelineCompletionPolicy
{
    FailOnAnyStageFailure = 0,
    AllowNonCriticalStageFailures = 1
}
