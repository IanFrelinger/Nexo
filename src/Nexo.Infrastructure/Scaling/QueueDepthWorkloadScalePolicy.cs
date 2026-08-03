using Microsoft.Extensions.Options;
using Nexo.Core.Application.Scaling.Models;
using Nexo.Core.Application.Scaling.Ports;

namespace Nexo.Infrastructure.Scaling;

/// <summary>
/// Simple queue-depth policy: step replicas up/down within workload min/max.
/// Provider-agnostic — works with Kubernetes, Compose, or a future ECS adapter.
/// </summary>
public sealed class QueueDepthWorkloadScalePolicy : IWorkloadScalePolicy
{
    private readonly WorkloadAutoscaleOptions _options;

    /// <summary>Creates a policy using autoscale thresholds from options.</summary>
    public QueueDepthWorkloadScalePolicy(IOptions<WorkloadScalerOptions> options)
    {
        _options = options.Value.Autoscale;
    }

    /// <summary>Creates a policy with explicit thresholds (tests).</summary>
    public QueueDepthWorkloadScalePolicy(
        int scaleUpQueueDepth,
        int scaleDownQueueDepth,
        int scaleUpStep = 1,
        int scaleDownStep = 1)
    {
        _options = new WorkloadAutoscaleOptions
        {
            ScaleUpQueueDepth = scaleUpQueueDepth,
            ScaleDownQueueDepth = scaleDownQueueDepth,
            ScaleUpStep = scaleUpStep,
            ScaleDownStep = scaleDownStep,
        };
    }

    /// <inheritdoc />
    public WorkloadScaleDecision Evaluate(WorkloadScaleContext context)
    {
        var current = context.Replicas.DesiredReplicas;
        var min = context.Workload.MinReplicas;
        var max = context.Workload.MaxReplicas;
        var depth = Math.Max(context.Demand.QueueDepth, context.Demand.PendingTasks);

        if (depth >= _options.ScaleUpQueueDepth && current < max)
        {
            var desired = Math.Min(max, current + Math.Max(1, _options.ScaleUpStep));
            return new WorkloadScaleDecision(
                desired,
                ShouldScale: desired != current,
                Reason: $"queueDepth={depth} >= scaleUp={_options.ScaleUpQueueDepth}");
        }

        if (depth <= _options.ScaleDownQueueDepth && current > min)
        {
            var desired = Math.Max(min, current - Math.Max(1, _options.ScaleDownStep));
            return new WorkloadScaleDecision(
                desired,
                ShouldScale: desired != current,
                Reason: $"queueDepth={depth} <= scaleDown={_options.ScaleDownQueueDepth}");
        }

        return new WorkloadScaleDecision(current, ShouldScale: false, Reason: "within thresholds");
    }
}
