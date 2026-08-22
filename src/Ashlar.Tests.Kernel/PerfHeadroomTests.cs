using System.Text.Json;
using FluentAssertions;
using Ashlar.Abstractions;
using Ashlar.Policies;
using Xunit;

namespace Ashlar.Tests.Kernel;

/// <summary>Tests for perf headroom.</summary>
public class PerfHeadroomTests
{
    /// <summary>Call.</summary>
    /// <param name="id">Id.</param>
    private static ToolCall Call(string id) =>
        new(id, JsonDocument.Parse("{}").RootElement);

    [Fact]
    public void Approve_returns_OK_when_no_elapsed_data()
    {
        var p = new PerfHeadroom(TimeSpan.FromSeconds(1));
        p.Approve(Call("t1"), new WorldSnapshot(0, new Dictionary<string, object?>()), out var reason).Should().BeTrue();
        reason.Should().Be("OK");
    }

    [Fact]
    public void Approve_ignores_non_TimeSpan_elapsed_data()
    {
        var p = new PerfHeadroom(TimeSpan.FromSeconds(1));
        var snap = new WorldSnapshot(0, new Dictionary<string, object?>
        {
            ["ToolElapsed:t1"] = "not a TimeSpan",
        });
        p.Approve(Call("t1"), snap, out var reason).Should().BeTrue();
        reason.Should().Be("OK");
    }

    [Fact]
    public void Approve_accumulates_elapsed_and_rejects_after_budget_exceeded()
    {
        var p = new PerfHeadroom(TimeSpan.FromMilliseconds(100));

        var snap = new WorldSnapshot(0, new Dictionary<string, object?>
        {
            ["ToolElapsed:t1"] = TimeSpan.FromMilliseconds(60),
        });

        p.Approve(Call("t1"), snap, out var first).Should().BeTrue();
        first.Should().Be("OK");

        p.Approve(Call("t1"), snap, out var second).Should().BeFalse();
        second.Should().Contain("exceeded time budget").And.Contain("t1");
    }

    [Fact]
    public void Approve_tracks_each_tool_independently_and_is_case_insensitive()
    {
        var p = new PerfHeadroom(TimeSpan.FromMilliseconds(50));

        var snapA = new WorldSnapshot(0, new Dictionary<string, object?>
        {
            ["ToolElapsed:Alpha"] = TimeSpan.FromMilliseconds(40),
        });
        var snapB = new WorldSnapshot(0, new Dictionary<string, object?>
        {
            ["ToolElapsed:beta"] = TimeSpan.FromMilliseconds(40),
        });

        p.Approve(new ToolCall("Alpha", JsonDocument.Parse("{}").RootElement), snapA, out _).Should().BeTrue();
        p.Approve(new ToolCall("beta", JsonDocument.Parse("{}").RootElement), snapB, out _).Should().BeTrue();

        var snapAOver = new WorldSnapshot(0, new Dictionary<string, object?>
        {
            ["ToolElapsed:alpha"] = TimeSpan.FromMilliseconds(40),
        });
        p.Approve(new ToolCall("alpha", JsonDocument.Parse("{}").RootElement), snapAOver, out _).Should().BeFalse();
    }

    [Fact]
    public void Reset_clears_accumulated_state()
    {
        var p = new PerfHeadroom(TimeSpan.FromMilliseconds(100));
        var snap = new WorldSnapshot(0, new Dictionary<string, object?>
        {
            ["ToolElapsed:t1"] = TimeSpan.FromMilliseconds(60),
        });
        p.Approve(Call("t1"), snap, out _);
        p.Approve(Call("t1"), snap, out _).Should().BeFalse();

        p.Reset();
        p.Approve(Call("t1"), snap, out var reason).Should().BeTrue();
        reason.Should().Be("OK");
    }
}
