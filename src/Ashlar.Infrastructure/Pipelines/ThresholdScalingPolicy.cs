using Ashlar.Core.Application.Pipelines.Models;
using Ashlar.Core.Application.Pipelines.Ports;

namespace Ashlar.Infrastructure.Pipelines;

/// <summary>
/// Simple threshold-based scaling policy with bounded worker counts.
/// </summary>
public sealed class ThresholdScalingPolicy : IPipelineScalingPolicy
{
    private readonly int _deterministicMin;
    private readonly int _deterministicMax;
    private readonly int _agenticMin;
    private readonly int _agenticMax;
    private readonly int _queueScaleUpThreshold;
    private readonly double _errorRateThrottleThreshold;

    /// <summary>Initializes a new threshold scaling policy.</summary>
    public ThresholdScalingPolicy(
        int deterministicMin = 1,
        int deterministicMax = 8,
        int agenticMin = 1,
        int agenticMax = 4,
        int queueScaleUpThreshold = 10,
        double errorRateThrottleThreshold = 0.5)
    {
        _deterministicMin = deterministicMin;
        _deterministicMax = deterministicMax;
        _agenticMin = agenticMin;
        _agenticMax = agenticMax;
        _queueScaleUpThreshold = queueScaleUpThreshold;
        _errorRateThrottleThreshold = errorRateThrottleThreshold;
    }

    /// <summary>Evaluate.</summary>
    public PipelineScalingDecision Evaluate(PipelineScalingSnapshot snapshot)
    {
        var deterministic = snapshot.CurrentDeterministicWorkers;
        var agentic = snapshot.CurrentAgenticWorkers;

        if (snapshot.DeterministicQueueDepth >= _queueScaleUpThreshold)
            deterministic = Math.Min(_deterministicMax, deterministic + 1);
        else if (snapshot.DeterministicQueueDepth == 0)
            deterministic = Math.Max(_deterministicMin, deterministic - 1);

        if (snapshot.AgenticErrorRate >= _errorRateThrottleThreshold)
            agentic = Math.Max(_agenticMin, agentic - 1);
        else if (snapshot.AgenticQueueDepth >= _queueScaleUpThreshold)
            agentic = Math.Min(_agenticMax, agentic + 1);
        else if (snapshot.AgenticQueueDepth == 0)
            agentic = Math.Max(_agenticMin, agentic - 1);

        return new PipelineScalingDecision
        {
            DeterministicWorkers = deterministic,
            AgenticWorkers = agentic,
            DeterministicMaxDegreeOfParallelism = deterministic,
            AgenticMaxDegreeOfParallelism = agentic
        };
    }
}
