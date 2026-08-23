using FluentAssertions;
using Moq;
using Ashlar.Abstractions.Barriers;
using Ashlar.Orchestration.Barriers;
using Xunit;

namespace Ashlar.Tests.Orchestration.Barriers;

/// <summary>Tests for barrier guard.</summary>
public sealed class BarrierGuardTests
{
    [Fact]
    public async Task AssertValidForInvocation_NoContext_RequireExplicit_ThrowsMissing()
    {
        var accessor = new Mock<IBarrierContextAccessor>();
        accessor.SetupGet(x => x.Current).Returns((BarrierContext?)null);
        var audit = new Mock<IBarrierAuditLog>();
        var guard = CreateGuard(accessor.Object, audit.Object, new BarrierOptions
        {
            Levels = ["low", "high"],
            RequireExplicitBarrier = true
        });

        var act = async () => await guard.AssertValidForInvocationAsync("agent-1", "corr-1", "span-1", CancellationToken.None);

        await act.Should().ThrowAsync<BarrierContextMissingException>();
    }

    [Fact]
    public async Task AssertValidForInvocation_NoContext_DefaultsToFloor_EmitsDefaultApplied()
    {
        var accessor = new Mock<IBarrierContextAccessor>();
        accessor.SetupGet(x => x.Current).Returns((BarrierContext?)null);
        var audit = new Mock<IBarrierAuditLog>();
        audit.Setup(x => x.RecordAsync(It.IsAny<BarrierAuditEvent>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);

        var guard = CreateGuard(accessor.Object, audit.Object, new BarrierOptions
        {
            Levels = ["free", "pro"],
            RequireExplicitBarrier = false
        });

        await guard.AssertValidForInvocationAsync("agent-1", "corr-1", "span-1", CancellationToken.None);

        audit.Verify(
            x => x.RecordAsync(
                It.Is<BarrierAuditEvent>(e =>
                    e.EventType == BarrierAuditEventType.DefaultApplied &&
                    e.BarrierLevel == "free"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task AssertValidForInvocation_ValidContext_EmitsAgentInvoked()
    {
        var hierarchy = new BarrierHierarchy([
            new BarrierLevel("public", 0),
            new BarrierLevel("private", 1)
        ]);
        var context = BarrierContext.Create("private", BarrierAuthoritySource.Cli, "*", "corr-1", hierarchy);

        var accessor = new Mock<IBarrierContextAccessor>();
        accessor.SetupGet(x => x.Current).Returns(context);
        var audit = new Mock<IBarrierAuditLog>();
        audit.Setup(x => x.RecordAsync(It.IsAny<BarrierAuditEvent>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);
        var guard = CreateGuard(accessor.Object, audit.Object, new BarrierOptions
        {
            Levels = ["public", "private"]
        });

        await guard.AssertValidForInvocationAsync("agent-1", "corr-1", "span-1", CancellationToken.None);

        audit.Verify(
            x => x.RecordAsync(
                It.Is<BarrierAuditEvent>(e =>
                    e.EventType == BarrierAuditEventType.AgentInvoked &&
                    e.BarrierLevel == "private"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public void AssertNoElevation_ElevationAttempt_ThrowsAndAudits()
    {
        var accessor = new Mock<IBarrierContextAccessor>();
        var audit = new Mock<IBarrierAuditLog>();
        audit.Setup(x => x.RecordAsync(It.IsAny<BarrierAuditEvent>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);
        var guard = CreateGuard(accessor.Object, audit.Object, new BarrierOptions
        {
            Levels = ["low", "high"]
        });

        var act = () => guard.AssertNoElevation("high", "low", "agent-1", "corr-1");

        act.Should().Throw<BarrierElevationException>();
        audit.Verify(
            x => x.RecordAsync(
                It.Is<BarrierAuditEvent>(e => e.EventType == BarrierAuditEventType.ElevationDenied),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public void AssertNoElevation_SameLevel_NoException()
    {
        var guard = CreateGuard(
            new Mock<IBarrierContextAccessor>().Object,
            new Mock<IBarrierAuditLog>().Object,
            new BarrierOptions { Levels = ["low", "high"] });

        var act = () => guard.AssertNoElevation("low", "low", "agent-1", "corr-1");

        act.Should().NotThrow();
    }

    private static BarrierGuard CreateGuard(
        IBarrierContextAccessor accessor,
        IBarrierAuditLog audit,
        BarrierOptions options)
    {
        var hierarchy = new BarrierHierarchy(options.Levels.Select((name, rank) => new BarrierLevel(name, rank)));
        /// <summary>Barrier guard.</summary>
        return new BarrierGuard(accessor, audit, hierarchy, options);
    }
}
