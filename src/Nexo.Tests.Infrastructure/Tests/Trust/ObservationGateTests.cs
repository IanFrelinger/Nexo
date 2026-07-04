using FluentAssertions;
using Nexo.Core.Application.Trust.Ports;
using Nexo.Infrastructure.Trust;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.Trust;

/// <summary>Tests for observation gate.</summary>
public class ObservationGateTests
{
    [Fact]
    public void ShouldObserve_WhenPaused_ReturnsFalse()
    {
        var boundary = new AccessBoundary();
        boundary.SetPause(true);
        var gate = new ObservationGate(boundary);

        gate.ShouldObserve("file-contents", "knowledge-indexer", "/path").Should().BeFalse();
    }

    [Fact]
    public void ShouldObserve_WhenCategoryDenied_ReturnsFalse()
    {
        var boundary = new AccessBoundary();
        boundary.SetCategoryAllowed("file-contents", false);
        var gate = new ObservationGate(boundary);

        gate.ShouldObserve("file-contents", "knowledge-indexer").Should().BeFalse();
    }

    [Fact]
    public void ShouldObserve_WhenSourceDenied_ReturnsFalse()
    {
        var boundary = new AccessBoundary();
        boundary.SetSourceAllowed("knowledge-indexer", false);
        var gate = new ObservationGate(boundary);

        gate.ShouldObserve("file-contents", "knowledge-indexer").Should().BeFalse();
    }

    [Fact]
    public void ShouldObserve_WhenProjectOverrideDenies_ReturnsFalse()
    {
        var boundary = new AccessBoundary();
        boundary.SetProjectOverride("/secret", new Dictionary<string, bool> { ["knowledge-indexer"] = false });
        var gate = new ObservationGate(boundary);

        gate.ShouldObserve("file-contents", "knowledge-indexer", "/secret").Should().BeFalse();
    }

    [Fact]
    public void ShouldObserve_WhenAllAllowed_ReturnsTrue()
    {
        var boundary = new AccessBoundary();
        var gate = new ObservationGate(boundary);

        gate.ShouldObserve("file-contents", "knowledge-indexer", "/any").Should().BeTrue();
    }
}
