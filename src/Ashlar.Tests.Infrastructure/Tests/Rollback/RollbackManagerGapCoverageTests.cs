using FluentAssertions;
using Moq;
using Ashlar.Core.Application.Adaptation.Models;
using Ashlar.Core.Application.Adaptation.Ports;
using Ashlar.Core.Application.Rollback.Models;
using Ashlar.Core.Application.Rollback.Ports;
using Ashlar.Infrastructure.Rollback;
using Xunit;

namespace Ashlar.Tests.Infrastructure.Tests.Rollback;

/// <summary>Tests for rollback manager gap coverage.</summary>
public sealed class RollbackManagerGapCoverageTests
{
    [Fact]
    public void Constructor_throws_for_null_dependencies()
    {
        var snapshot = Mock.Of<ISnapshotStore>();
        var graph = Mock.Of<IDependencyGraph>();
        var audit = Mock.Of<IAdaptationAuditLog>();

        var act = () => new RollbackManager(null!, graph, audit);
        act.Should().Throw<ArgumentNullException>().WithParameterName("snapshotStore");

        act = () => new RollbackManager(snapshot, null!, audit);
        act.Should().Throw<ArgumentNullException>().WithParameterName("dependencyGraph");

        act = () => new RollbackManager(snapshot, graph, null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("auditLog");
    }

    [Fact]
    public void PrepareForInherit_rejects_blank_adaptation_id()
    {
        var sut = CreateManager();

        var act = () => sut.PrepareForInherit("  ", ["path"]);

        act.Should().Throw<ArgumentNullException>().WithParameterName("adaptationId");
    }

    [Fact]
    public async Task BeforeInheritAsync_throws_when_adaptation_not_prepared()
    {
        var sut = CreateManager();

        var act = async () => await sut.BeforeInheritAsync("missing-adaptation");

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*PrepareForInherit*");
    }

    [Fact]
    public async Task RollbackAsync_throws_when_snapshot_missing()
    {
        var sut = CreateManager();
        sut.PrepareForInherit("adapt-1", ["path/a.cs"]);

        var act = async () => await sut.RollbackAsync("adapt-1");

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*No snapshot found*");
    }

    [Fact]
    public async Task BeforeInheritAsync_takes_snapshot_and_records_audit()
    {
        var snapshotStore = new Mock<ISnapshotStore>();
        snapshotStore
            .Setup(s => s.TakeSnapshotAsync("before-inherit-adapt-1", It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("snap-123");

        var audit = new Mock<IAdaptationAuditLog>();
        var sut = CreateManager(snapshotStore.Object, audit.Object);
        sut.PrepareForInherit("adapt-1", ["path/a.cs"]);

        var snapshotId = await sut.BeforeInheritAsync("adapt-1");

        snapshotId.Should().Be("snap-123");
        audit.Verify(
            l => l.LogAsync(It.Is<AdaptationAuditEntry>(e => e.Outcome == "SnapshotCreated"), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task RollbackAsync_restores_snapshot_and_clears_state()
    {
        var snapshotStore = new Mock<ISnapshotStore>();
        snapshotStore
            .Setup(s => s.TakeSnapshotAsync(It.IsAny<string>(), It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("snap-rollback");
        snapshotStore
            .Setup(s => s.RestoreSnapshotAsync("snap-rollback", It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var audit = new Mock<IAdaptationAuditLog>();
        var sut = CreateManager(snapshotStore.Object, audit.Object);
        sut.PrepareForInherit("adapt-rollback", ["path/a.cs"]);
        await sut.BeforeInheritAsync("adapt-rollback");

        await sut.RollbackAsync("adapt-rollback");

        snapshotStore.Verify(s => s.RestoreSnapshotAsync("snap-rollback", It.IsAny<CancellationToken>()), Times.Once);
        audit.Verify(
            l => l.LogAsync(It.Is<AdaptationAuditEntry>(e => e.Outcome == "Rollback"), It.IsAny<CancellationToken>()),
            Times.Once);

        var act = async () => await sut.RollbackAsync("adapt-rollback");
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task RollbackToSnapshotAsync_restores_without_prepare()
    {
        var snapshotStore = new Mock<ISnapshotStore>();
        snapshotStore
            .Setup(s => s.RestoreSnapshotAsync("direct-snap", It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var audit = new Mock<IAdaptationAuditLog>();
        var sut = CreateManager(snapshotStore.Object, audit.Object);

        await sut.RollbackToSnapshotAsync("direct-snap");

        audit.Verify(
            l => l.LogAsync(It.Is<AdaptationAuditEntry>(e => e.Outcome == "RollbackToSnapshot"), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task PreviewRollbackAsync_reports_dependents_and_data_loss()
    {
        var graph = new Mock<IDependencyGraph>();
        graph.Setup(g => g.GetTransitiveDependents("adapt-preview"))
            .Returns(["adapt-preview", "dep-1", "dep-2"]);

        var sut = CreateManager(dependencyGraph: graph.Object);

        var impact = await sut.PreviewRollbackAsync("adapt-preview");

        impact.TargetAdaptationId.Should().Be("adapt-preview");
        impact.AdditionalComponentsAffected.Should().Equal("dep-1", "dep-2");
        impact.WillCauseDataLoss.Should().BeTrue();
        impact.Summary.Should().Contain("dependent component");
    }

    [Fact]
    public async Task PreviewRollbackAsync_without_dependents_reports_safe_summary()
    {
        var graph = new Mock<IDependencyGraph>();
        graph.Setup(g => g.GetTransitiveDependents("solo"))
            .Returns(["solo"]);

        var sut = CreateManager(dependencyGraph: graph.Object);

        var impact = await sut.PreviewRollbackAsync("solo");

        impact.WillCauseDataLoss.Should().BeFalse();
        impact.Summary.Should().Contain("pre-inheritance state");
    }

    private static RollbackManager CreateManager(
        ISnapshotStore? snapshotStore = null,
        IAdaptationAuditLog? auditLog = null,
        IDependencyGraph? dependencyGraph = null)
        => new(
            snapshotStore ?? Mock.Of<ISnapshotStore>(),
            dependencyGraph ?? Mock.Of<IDependencyGraph>(),
            auditLog ?? Mock.Of<IAdaptationAuditLog>());
}
