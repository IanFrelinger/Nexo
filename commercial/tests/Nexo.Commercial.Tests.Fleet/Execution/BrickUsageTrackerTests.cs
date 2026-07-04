using FluentAssertions;
using Microsoft.Extensions.Options;
using Nexo.Commercial.Fleet.Contracts.Networking.Models;
using Nexo.Core.Domain;
using Nexo.Commercial.Fleet.Infrastructure.Execution;
using Xunit;

namespace Nexo.Commercial.Tests.Fleet.Execution;

/// <summary>
/// Tests for BrickUsageTracker: usage-weighted brick routing (synaptic plasticity).
/// </summary>
public class BrickUsageTrackerTests
{
    [Fact]
    public void BrickUsageTrackerOptions_UsesNexoDefaults()
    {
        var opts = new BrickUsageTrackerOptions();
        opts.MaxEntries.Should().Be(NexoDefaults.BrickUsageTrackerMaxEntries);
        opts.RollingHourWindowSeconds.Should().Be(NexoDefaults.BrickUsageTrackerRollingHourWindowSeconds);
    }

    private static BrickUsageTracker CreateTracker(int maxEntries = 1000, int rollingHourSeconds = 3600)
    {
        var options = new BrickUsageTrackerOptions { MaxEntries = maxEntries, RollingHourWindowSeconds = rollingHourSeconds };
        return new BrickUsageTracker(Options.Create(options));
    }

    private static BrickUsageRecord Record(string brickId, string nodeId = "node1", bool success = true, long durationMs = 10, DateTimeOffset? ts = null)
    {
        return new BrickUsageRecord
        {
            BrickId = brickId,
            NodeId = nodeId,
            Timestamp = ts ?? DateTimeOffset.UtcNow,
            DurationMs = durationMs,
            Success = success,
            Implementation = "Deterministic",
            IsRemote = false
        };
    }

    [Fact]
    public void RecordExecution_AddsRecord_GetUsageStats_ReturnsAggregated()
    {
        var tracker = CreateTracker();
        tracker.RecordExecution(Record("brick-a", durationMs: 50));
        tracker.RecordExecution(Record("brick-a", durationMs: 100));
        tracker.RecordExecution(Record("brick-a", success: false, durationMs: 200));

        var stats = tracker.GetUsageStats("brick-a");

        stats.Should().NotBeNull();
        stats!.TotalExecutions.Should().Be(3);
        stats.SuccessRate.Should().BeApproximately(2.0 / 3.0, 0.01);
        stats.AvgDurationMs.Should().BeApproximately(116.67, 0.1);
        stats.LastUsed.Should().NotBeNull();
    }

    [Fact]
    public void GetUsageStats_UnknownBrick_ReturnsNull()
    {
        var tracker = CreateTracker();
        tracker.RecordExecution(Record("brick-x"));

        tracker.GetUsageStats("other").Should().BeNull();
    }

    [Fact]
    public void GetHotBricks_OrdersByUsageInLastHour()
    {
        var tracker = CreateTracker();
        tracker.RecordExecution(Record("low"));
        tracker.RecordExecution(Record("high"));
        tracker.RecordExecution(Record("high"));
        tracker.RecordExecution(Record("high"));
        tracker.RecordExecution(Record("mid"));
        tracker.RecordExecution(Record("mid"));

        var hot = tracker.GetHotBricks(3);

        hot.Should().HaveCount(3);
        hot[0].Should().Be("high");
        hot[1].Should().Be("mid");
        hot[2].Should().Be("low");
    }

    [Fact]
    public void GetColdBricks_ReturnsBricksNotUsedWithinTimespan()
    {
        var tracker = CreateTracker();
        var old = DateTimeOffset.UtcNow.AddDays(-2);
        tracker.RecordExecution(Record("cold1", ts: old));
        tracker.RecordExecution(Record("cold2", ts: old));
        tracker.RecordExecution(Record("warm")); // recent

        var cold = tracker.GetColdBricks(TimeSpan.FromDays(1));

        cold.Should().Contain("cold1").And.Contain("cold2");
        cold.Should().NotContain("warm");
    }

    [Fact]
    public void RecordExecution_TrimsToMaxEntries_WhenOverLimit()
    {
        var tracker = CreateTracker(maxEntries: 5);
        for (var i = 0; i < 10; i++)
            tracker.RecordExecution(Record($"brick-{i}"));

        var hot = tracker.GetHotBricks(20);
        hot.Should().HaveCountLessThanOrEqualTo(5);
    }

    [Fact]
    public async Task RecordExecution_ConcurrentCalls_DoesNotCorruptState()
    {
        var tracker = CreateTracker(maxEntries: 5000);
        var tasks = Enumerable.Range(0, 100).Select(i =>
            Task.Run(() => tracker.RecordExecution(Record($"brick-{i % 10}")))
        ).ToArray();
        await Task.WhenAll(tasks);

        var stats = tracker.GetUsageStats("brick-0");
        stats.Should().NotBeNull();
        stats!.TotalExecutions.Should().Be(10);
    }
}
