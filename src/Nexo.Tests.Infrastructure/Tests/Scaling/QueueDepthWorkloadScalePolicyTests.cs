using FluentAssertions;
using Nexo.Core.Application.Scaling.Models;
using Nexo.Infrastructure.Scaling;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.Scaling;

public sealed class QueueDepthWorkloadScalePolicyTests
{
    [Fact]
    public void Evaluate_ScalesUpWhenQueueDepthHigh()
    {
        var sut = new QueueDepthWorkloadScalePolicy(scaleUpQueueDepth: 5, scaleDownQueueDepth: 0);
        var decision = sut.Evaluate(Context(desired: 2, queue: 8, min: 1, max: 10));

        decision.ShouldScale.Should().BeTrue();
        decision.DesiredReplicas.Should().Be(3);
    }

    [Fact]
    public void Evaluate_ScalesDownWhenQueueEmpty()
    {
        var sut = new QueueDepthWorkloadScalePolicy(scaleUpQueueDepth: 5, scaleDownQueueDepth: 0);
        var decision = sut.Evaluate(Context(desired: 4, queue: 0, min: 1, max: 10));

        decision.ShouldScale.Should().BeTrue();
        decision.DesiredReplicas.Should().Be(3);
    }

    [Fact]
    public void Evaluate_RespectsMaxReplicas()
    {
        var sut = new QueueDepthWorkloadScalePolicy(scaleUpQueueDepth: 1, scaleDownQueueDepth: 0, scaleUpStep: 5);
        var decision = sut.Evaluate(Context(desired: 8, queue: 100, min: 1, max: 10));

        decision.DesiredReplicas.Should().Be(10);
    }

    private static WorkloadScaleContext Context(int desired, int queue, int min, int max) =>
        new(
            new WorkloadDescriptor("mesh-worker", "Mesh Worker", min, max),
            new WorkloadReplicaSnapshot("mesh-worker", desired, desired, desired, DateTimeOffset.UtcNow),
            new WorkloadDemandSnapshot("mesh-worker", queue, PendingTasks: 0, DateTimeOffset.UtcNow));
}
