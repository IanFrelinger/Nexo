using FluentAssertions;
using Ashlar.Core.Application.Pipelines.Models;
using Ashlar.Infrastructure.Pipelines;
using Xunit;

namespace Ashlar.Tests.Infrastructure.Tests.Pipelines;

/// <summary>Tests for threshold scaling policy.</summary>
public sealed class ThresholdScalingPolicyTests
{
    [Fact]
    public void Evaluate_WhenDeterministicQueueDepthHigh_ScalesDeterministicUpWithinMax()
    {
        var sut = new ThresholdScalingPolicy(
            deterministicMin: 1,
            deterministicMax: 3,
            agenticMin: 1,
            agenticMax: 3,
            queueScaleUpThreshold: 10,
            errorRateThrottleThreshold: 0.5);

        var decision = sut.Evaluate(new PipelineScalingSnapshot
        {
            DeterministicQueueDepth = 25,
            AgenticQueueDepth = 0,
            DeterministicErrorRate = 0.0,
            AgenticErrorRate = 0.0,
            CurrentDeterministicWorkers = 2,
            CurrentAgenticWorkers = 2
        });

        Assert.Equal(3, decision.DeterministicWorkers);
        Assert.Equal(3, decision.DeterministicMaxDegreeOfParallelism);
    }

    [Fact]
    public void Evaluate_WhenDeterministicQueueEmpty_ScalesDeterministicDownToMin()
    {
        var sut = new ThresholdScalingPolicy(
            deterministicMin: 2,
            deterministicMax: 6,
            agenticMin: 1,
            agenticMax: 4,
            queueScaleUpThreshold: 10,
            errorRateThrottleThreshold: 0.5);

        var decision = sut.Evaluate(new PipelineScalingSnapshot
        {
            DeterministicQueueDepth = 0,
            AgenticQueueDepth = 5,
            DeterministicErrorRate = 0.0,
            AgenticErrorRate = 0.0,
            CurrentDeterministicWorkers = 3,
            CurrentAgenticWorkers = 2
        });

        Assert.Equal(2, decision.DeterministicWorkers);
    }

    [Fact]
    public void Evaluate_WhenAgenticErrorRateHigh_ThrottlesAgenticWorkers()
    {
        var sut = new ThresholdScalingPolicy(
            deterministicMin: 1,
            deterministicMax: 8,
            agenticMin: 1,
            agenticMax: 4,
            queueScaleUpThreshold: 10,
            errorRateThrottleThreshold: 0.25);

        var decision = sut.Evaluate(new PipelineScalingSnapshot
        {
            DeterministicQueueDepth = 1,
            AgenticQueueDepth = 100,
            DeterministicErrorRate = 0.0,
            AgenticErrorRate = 0.30,
            CurrentDeterministicWorkers = 2,
            CurrentAgenticWorkers = 3
        });

        Assert.Equal(2, decision.AgenticWorkers);
        Assert.Equal(2, decision.AgenticMaxDegreeOfParallelism);
    }

    [Fact]
    public void Evaluate_WhenAgenticQueueHighAndHealthy_ScalesAgenticUpWithinMax()
    {
        var sut = new ThresholdScalingPolicy(
            deterministicMin: 1,
            deterministicMax: 8,
            agenticMin: 1,
            agenticMax: 3,
            queueScaleUpThreshold: 10,
            errorRateThrottleThreshold: 0.5);

        var decision = sut.Evaluate(new PipelineScalingSnapshot
        {
            DeterministicQueueDepth = 1,
            AgenticQueueDepth = 10,
            DeterministicErrorRate = 0.0,
            AgenticErrorRate = 0.0,
            CurrentDeterministicWorkers = 2,
            CurrentAgenticWorkers = 2
        });

        Assert.Equal(3, decision.AgenticWorkers);
    }
}
