using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Nexo.Infrastructure.Ide;
using Xunit;

namespace Nexo.Tests.Application.Ide;

public sealed class IdeRunTrackerTests
{
    [Fact]
    public void StartComplete_ListsRunAsCompleted()
    {
        var tracker = new IdeRunTracker();
        var (runId, _) = tracker.Start("chat", "hello world", "agent-1", "model-1", CancellationToken.None);
        tracker.Complete(runId, "done", "proposal-1");

        var run = tracker.Get(runId);
        run.Should().NotBeNull();
        run!.Status.Should().Be("completed");
        run.Kind.Should().Be("chat");
        run.Message.Should().Be("done");
        run.ProposalId.Should().Be("proposal-1");
        run.CompletedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public void Fail_SetsFailedStatus()
    {
        var tracker = new IdeRunTracker();
        var (runId, _) = tracker.Start("edit", "x", "a", "m", CancellationToken.None);
        tracker.Fail(runId, "boom");

        var rec = tracker.Get(runId);
        rec.Should().NotBeNull();
        rec!.Status.Should().Be("failed");
        rec.Error.Should().Be("boom");
    }

    [Fact]
    public async Task TryCancel_RunningRun_SetsCancelled()
    {
        var tracker = new IdeRunTracker();
        var (runId, token) = tracker.Start("edit", "change file", null, null, CancellationToken.None);
        token.IsCancellationRequested.Should().BeFalse();

        tracker.TryCancel(runId).Should().BeTrue();
        await Task.Delay(20);
        tracker.Get(runId)!.Status.Should().Be("cancelled");
        token.IsCancellationRequested.Should().BeTrue();
        tracker.TryCancel(runId).Should().BeFalse();
    }

    [Fact]
    public void List_ReturnsNewestFirst()
    {
        var tracker = new IdeRunTracker();
        var (a, _) = tracker.Start("chat", "one", "a", "m", CancellationToken.None);
        Thread.Sleep(5);
        var (b, _) = tracker.Start("edit", "two", "a", "m", CancellationToken.None);
        tracker.Complete(a, "done-a");
        tracker.Complete(b, "done-b");

        var recent = tracker.List(10);
        recent.Count.Should().BeGreaterThanOrEqualTo(2);
        recent[0].RunId.Should().Be(b);
        recent[1].RunId.Should().Be(a);
    }

    [Fact]
    public void Get_Unknown_ReturnsNull()
    {
        var tracker = new IdeRunTracker();
        tracker.Get(Guid.NewGuid().ToString("N")).Should().BeNull();
    }
}
