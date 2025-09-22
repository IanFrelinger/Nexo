using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Nexo.Observability.Helpers;

/// <summary>
/// Helper class for observability operations.
/// </summary>
public static class ObservabilityHelpers
{
    /// <summary>
    /// Creates an activity with common tags for pipeline operations.
    /// </summary>
    /// <param name="activitySource">The activity source.</param>
    /// <param name="operationName">The operation name.</param>
    /// <param name="tags">Additional tags.</param>
    /// <returns>The created activity.</returns>
    public static Activity? CreatePipelineActivity(
        ActivitySource activitySource,
        string operationName,
        Dictionary<string, object?>? tags = null)
    {
        var activity = activitySource.StartActivity(operationName);
        if (activity == null) return null;

        // Add common tags
        activity.SetTag("service.name", "Nexo");
        activity.SetTag("operation.name", operationName);
        activity.SetTag("timestamp", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());

        // Add custom tags
        if (tags != null)
        {
            foreach (var tag in tags)
            {
                activity.SetTag(tag.Key, tag.Value);
            }
        }

        return activity;
    }

    /// <summary>
    /// Records a counter metric.
    /// </summary>
    /// <param name="counter">The counter.</param>
    /// <param name="value">The value to record.</param>
    /// <param name="tags">Additional tags.</param>
    public static void RecordCounter(Counter<long> counter, long value = 1, Dictionary<string, object?>? tags = null)
    {
        var tagList = tags?.Select(kvp => new KeyValuePair<string, object?>(kvp.Key, kvp.Value)).ToArray() ?? Array.Empty<KeyValuePair<string, object?>>();
        counter.Add(value, tagList);
    }

    /// <summary>
    /// Records a histogram metric.
    /// </summary>
    /// <param name="histogram">The histogram.</param>
    /// <param name="value">The value to record.</param>
    /// <param name="tags">Additional tags.</param>
    public static void RecordHistogram(Histogram<double> histogram, double value, Dictionary<string, object?>? tags = null)
    {
        var tagList = tags?.Select(kvp => new KeyValuePair<string, object?>(kvp.Key, kvp.Value)).ToArray() ?? Array.Empty<KeyValuePair<string, object?>>();
        histogram.Record(value, tagList);
    }

    /// <summary>
    /// Records a gauge metric.
    /// </summary>
    /// <param name="gauge">The gauge.</param>
    /// <param name="value">The value to record.</param>
    /// <param name="tags">Additional tags.</param>
    public static void RecordGauge(ObservableGauge<double> gauge, double value, Dictionary<string, object?>? tags = null)
    {
        // Gauges are typically recorded through the measurement callback
        // This is a placeholder for consistency
    }
}
