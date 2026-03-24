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
}
