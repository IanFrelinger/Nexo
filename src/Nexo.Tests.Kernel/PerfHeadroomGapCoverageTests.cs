using System.Text.Json;
using FluentAssertions;
using Nexo.Abstractions;
using Nexo.Policies;
using Xunit;

namespace Nexo.Tests.Kernel;

public sealed class PerfHeadroomGapCoverageTests
{
    private static ToolCall Call(string id) =>
        new(id, JsonDocument.Parse("{}").RootElement);

    [Fact]
    public void Approve_allows_call_exactly_at_limit()
    {
        var policy = new PerfHeadroom(TimeSpan.FromMilliseconds(100));
        var snap = Snapshot("t1", TimeSpan.FromMilliseconds(100));

        policy.Approve(Call("t1"), snap, out var reason).Should().BeTrue();
        reason.Should().Be("OK");
    }

    [Fact]
    public void Approve_allows_call_below_limit()
    {
        var policy = new PerfHeadroom(TimeSpan.FromMilliseconds(100));
        var snap = Snapshot("t1", TimeSpan.FromMilliseconds(1));

        policy.Approve(Call("t1"), snap, out var reason).Should().BeTrue();
        reason.Should().Be("OK");
    }

    [Fact]
    public void Approve_allows_zero_elapsed()
    {
        var policy = new PerfHeadroom(TimeSpan.FromMilliseconds(100));
        var snap = Snapshot("t1", TimeSpan.Zero);

        policy.Approve(Call("t1"), snap, out var reason).Should().BeTrue();
        reason.Should().Be("OK");
    }

    [Fact]
    public void Approve_rejects_one_tick_past_limit()
    {
        var policy = new PerfHeadroom(TimeSpan.FromMilliseconds(100));
        var snap = Snapshot("t1", TimeSpan.FromMilliseconds(60));

        policy.Approve(Call("t1"), snap, out _).Should().BeTrue();
        policy.Approve(Call("t1"), snap, out var reason).Should().BeFalse();
        reason.Should().Contain("exceeded time budget");
    }

    [Fact]
    public void Approve_rejects_significantly_over_limit_in_single_observation()
    {
        var policy = new PerfHeadroom(TimeSpan.FromMilliseconds(50));
        var snap = Snapshot("heavy", TimeSpan.FromMilliseconds(200));

        policy.Approve(Call("heavy"), snap, out var reason).Should().BeFalse();
        reason.Should().Contain("heavy");
    }

    [Fact]
    public void Approve_missing_elapsed_key_returns_ok()
    {
        var policy = new PerfHeadroom(TimeSpan.FromMilliseconds(100));
        var snap = new WorldSnapshot(0, new Dictionary<string, object?>());

        policy.Approve(Call("t1"), snap, out var reason).Should().BeTrue();
        reason.Should().Be("OK");
    }

    private static WorldSnapshot Snapshot(string toolId, TimeSpan elapsed)
        => new(0, new Dictionary<string, object?>
        {
            [$"ToolElapsed:{toolId}"] = elapsed,
        });
}
