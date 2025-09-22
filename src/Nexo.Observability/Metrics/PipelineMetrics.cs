using System.Diagnostics.Metrics;

namespace Nexo.Observability.Metrics;

/// <summary>
/// Metrics for pipeline operations.
/// </summary>
public class PipelineMetrics
{
    private readonly Meter _meter;
    private readonly Counter<long> _generationDurationCounter;
    private readonly Counter<long> _validationFailuresCounter;
    private readonly Counter<long> _policyScoreCounter;
    private readonly Counter<long> _pipelineSuccessCounter;
    private readonly Counter<long> _pipelineFailureCounter;
    private readonly Histogram<double> _generationDurationHistogram;
    private readonly Histogram<double> _validationDurationHistogram;
    private readonly Histogram<double> _policyEvaluationDurationHistogram;
    private readonly Histogram<double> _pipelineDurationHistogram;

    /// <summary>
    /// Initializes a new instance of the PipelineMetrics class.
    /// </summary>
    /// <param name="meter">The meter instance.</param>
    public PipelineMetrics(Meter meter)
    {
        _meter = meter ?? throw new ArgumentNullException(nameof(meter));

        // Counters
        _generationDurationCounter = _meter.CreateCounter<long>(
            "generation.duration",
            "ms",
            "Duration of generation operations in milliseconds");

        _validationFailuresCounter = _meter.CreateCounter<long>(
            "validation.failures",
            "count",
            "Number of validation failures");

        _policyScoreCounter = _meter.CreateCounter<long>(
            "policy.score",
            "score",
            "Policy evaluation scores");

        _pipelineSuccessCounter = _meter.CreateCounter<long>(
            "pipeline.success",
            "count",
            "Number of successful pipeline executions");

        _pipelineFailureCounter = _meter.CreateCounter<long>(
            "nexo.pipeline.failure",
            "count",
            "Number of failed pipeline executions");

        // Histograms
        _generationDurationHistogram = _meter.CreateHistogram<double>(
            "generation.duration",
            "ms",
            "Distribution of generation operation durations");

        _validationDurationHistogram = _meter.CreateHistogram<double>(
            "validation.duration",
            "ms",
            "Distribution of validation operation durations");

        _policyEvaluationDurationHistogram = _meter.CreateHistogram<double>(
            "policy.evaluation.duration",
            "ms",
            "Distribution of policy evaluation durations");

        _pipelineDurationHistogram = _meter.CreateHistogram<double>(
            "pipeline.duration",
            "ms",
            "Distribution of pipeline execution durations");
    }

    /// <summary>
    /// Records generation duration.
    /// </summary>
    /// <param name="durationMs">Duration in milliseconds.</param>
    /// <param name="operationType">Type of generation operation.</param>
    /// <param name="success">Whether the operation was successful.</param>
    public void RecordGenerationDuration(double durationMs, string operationType, bool success = true)
    {
        var tags = new Dictionary<string, object?>
        {
            ["operation_type"] = operationType,
            ["success"] = success
        };

        _generationDurationCounter.Add((long)durationMs, tags.Select(kvp => new KeyValuePair<string, object?>(kvp.Key, kvp.Value)).ToArray());
        _generationDurationHistogram.Record(durationMs, tags.Select(kvp => new KeyValuePair<string, object?>(kvp.Key, kvp.Value)).ToArray());
    }

    /// <summary>
    /// Records validation failure.
    /// </summary>
    /// <param name="validationType">Type of validation.</param>
    /// <param name="errorCode">Error code.</param>
    public void RecordValidationFailure(string validationType, string? errorCode = null)
    {
        var tags = new Dictionary<string, object?>
        {
            ["validation_type"] = validationType,
            ["error_code"] = errorCode ?? "unknown"
        };

        _validationFailuresCounter.Add(1, tags.Select(kvp => new KeyValuePair<string, object?>(kvp.Key, kvp.Value)).ToArray());
    }

    /// <summary>
    /// Records validation duration.
    /// </summary>
    /// <param name="durationMs">Duration in milliseconds.</param>
    /// <param name="validationType">Type of validation.</param>
    /// <param name="success">Whether the validation was successful.</param>
    public void RecordValidationDuration(double durationMs, string validationType, bool success = true)
    {
        var tags = new Dictionary<string, object?>
        {
            ["validation_type"] = validationType,
            ["success"] = success
        };

        _validationDurationHistogram.Record(durationMs, tags.Select(kvp => new KeyValuePair<string, object?>(kvp.Key, kvp.Value)).ToArray());
    }

    /// <summary>
    /// Records policy score.
    /// </summary>
    /// <param name="score">Policy score.</param>
    /// <param name="policyType">Type of policy.</param>
    public void RecordPolicyScore(long score, string policyType)
    {
        var tags = new Dictionary<string, object?>
        {
            ["policy_type"] = policyType
        };

        _policyScoreCounter.Add(score, tags.Select(kvp => new KeyValuePair<string, object?>(kvp.Key, kvp.Value)).ToArray());
    }

    /// <summary>
    /// Records policy evaluation duration.
    /// </summary>
    /// <param name="durationMs">Duration in milliseconds.</param>
    /// <param name="policyType">Type of policy.</param>
    public void RecordPolicyEvaluationDuration(double durationMs, string policyType)
    {
        var tags = new Dictionary<string, object?>
        {
            ["policy_type"] = policyType
        };

        _policyEvaluationDurationHistogram.Record(durationMs, tags.Select(kvp => new KeyValuePair<string, object?>(kvp.Key, kvp.Value)).ToArray());
    }

    /// <summary>
    /// Records pipeline success.
    /// </summary>
    /// <param name="pipelineType">Type of pipeline.</param>
    /// <param name="durationMs">Duration in milliseconds.</param>
    public void RecordPipelineSuccess(string pipelineType, double durationMs)
    {
        var tags = new Dictionary<string, object?>
        {
            ["pipeline_type"] = pipelineType
        };

        _pipelineSuccessCounter.Add(1, tags.Select(kvp => new KeyValuePair<string, object?>(kvp.Key, kvp.Value)).ToArray());
        _pipelineDurationHistogram.Record(durationMs, tags.Select(kvp => new KeyValuePair<string, object?>(kvp.Key, kvp.Value)).ToArray());
    }

    /// <summary>
    /// Records pipeline failure.
    /// </summary>
    /// <param name="pipelineType">Type of pipeline.</param>
    /// <param name="durationMs">Duration in milliseconds.</param>
    /// <param name="errorCode">Error code.</param>
    public void RecordPipelineFailure(string pipelineType, double durationMs, string? errorCode = null)
    {
        var tags = new Dictionary<string, object?>
        {
            ["pipeline_type"] = pipelineType,
            ["error_code"] = errorCode ?? "unknown"
        };

        _pipelineFailureCounter.Add(1, tags.Select(kvp => new KeyValuePair<string, object?>(kvp.Key, kvp.Value)).ToArray());
        _pipelineDurationHistogram.Record(durationMs, tags.Select(kvp => new KeyValuePair<string, object?>(kvp.Key, kvp.Value)).ToArray());
    }
}
