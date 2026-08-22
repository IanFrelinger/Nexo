using Ashlar.Core.Domain;

namespace Ashlar.Infrastructure.Pipelines.Sdk.Options;
/// <summary>
/// Runtime controls for pipeline execution behavior.
/// Bound from the <c>Ashlar:Pipelines:Execution</c> configuration section.
/// <para>
/// These options govern how the pipeline executor handles stage failures,
/// retries, and resumption.  They interact with
/// <see cref="PipelineCompletionPolicy"/> to determine the terminal run
/// state visible to callers and the observation pipeline.
/// </para>
/// <para>
/// <b>Related:</b>
/// <see cref="AshlarDefaults.PipelineMaxRetryAttempts"/> and
/// <see cref="AshlarDefaults.PipelineRetryDelayMs"/> supply the default
/// values used here.
/// </para>
/// </summary>
public sealed class PipelineExecutionOptions
{
    /// <summary>
    /// Configuration section path for <c>IOptions&lt;PipelineExecutionOptions&gt;</c> binding.
    /// </summary>
    public const string SectionName = "Ashlar:Pipelines:Execution";

    /// <summary>
    /// Maximum number of retry attempts for a failed stage before the stage
    /// is marked as permanently failed.  The retry loop uses a linear back-off
    /// seeded by <see cref="RetryDelayMs"/>.
    /// Override: <c>Ashlar:Pipelines:Execution:MaxRetryAttempts</c>.
    /// </summary>
    public int MaxRetryAttempts { get; set; } = AshlarDefaults.PipelineMaxRetryAttempts;

    /// <summary>
    /// Base delay (milliseconds) between stage-retry attempts.
    /// Actual delay is <c>RetryDelayMs * attemptNumber</c> (linear back-off).
    /// Override: <c>Ashlar:Pipelines:Execution:RetryDelayMs</c>.
    /// </summary>
    public int RetryDelayMs { get; set; } = AshlarDefaults.PipelineRetryDelayMs;

    /// <summary>
    /// When <c>true</c>, a new pipeline run looks for the most recent prior
    /// run of the same pipeline and skips stages that already succeeded,
    /// re-executing only those that failed or were never reached.
    /// <para>
    /// <b>Operationally</b> this means you can fix a broken brick / config,
    /// re-trigger the pipeline, and only the failed stages will execute —
    /// saving time and compute on long multi-stage pipelines.
    /// </para>
    /// <para>
    /// If the prior run ID cannot be found and <see cref="AllowMissingResumeSource"/>
    /// is <c>false</c>, execution fails immediately.
    /// </para>
    /// Override: <c>Ashlar:Pipelines:Execution:ResumeFailedStages</c>.
    /// </summary>
    public bool ResumeFailedStages { get; set; } = true;

    /// <summary>
    /// Determines how the terminal run state is derived when at least one
    /// stage has failed after exhausting retries.
    /// <list type="bullet">
    ///   <item><see cref="PipelineCompletionPolicy.FailOnAnyStageFailure"/>
    ///         (default) — the entire run is marked <c>Failed</c> if any
    ///         stage fails.  This is the safest policy for production.</item>
    ///   <item><see cref="PipelineCompletionPolicy.AllowNonCriticalStageFailures"/>
    ///         — stages tagged as non-critical may fail without failing the
    ///         overall run.  Useful for pipelines with optional enrichment
    ///         stages (e.g. telemetry, summary generation).</item>
    /// </list>
    /// Override: <c>Ashlar:Pipelines:Execution:CompletionPolicy</c>.
    /// </summary>
    public PipelineCompletionPolicy CompletionPolicy { get; set; } = PipelineCompletionPolicy.FailOnAnyStageFailure;

    /// <summary>
    /// When <c>true</c>, a missing resume-source run ID is treated as a
    /// no-op rather than an error — the pipeline starts a fresh run from
    /// stage 0.  Useful in CI / first-run scenarios where there is no
    /// prior run to resume from but <see cref="ResumeFailedStages"/> is
    /// left on globally.
    /// Override: <c>Ashlar:Pipelines:Execution:AllowMissingResumeSource</c>.
    /// </summary>
    public bool AllowMissingResumeSource { get; set; }

    /// <summary>
    /// Enables test-only behaviour hooks injected into stage executors
    /// (e.g. deterministic random seeds, artificial faults).
    /// <para>
    /// <b>Must stay <c>false</c> in production.</b>  When enabled, stage
    /// executors may bypass trust sanitization and skip retries to make
    /// integration tests faster and more deterministic.  A misconfigured
    /// production deployment with this flag on would silently weaken the
    /// trust boundary.
    /// </para>
    /// Override: <c>Ashlar:Pipelines:Execution:EnableTestHooks</c>.
    /// </summary>
    public bool EnableTestHooks { get; set; }
}
