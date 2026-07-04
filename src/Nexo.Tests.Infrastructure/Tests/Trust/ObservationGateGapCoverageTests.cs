using FluentAssertions;
using Nexo.Infrastructure.Trust;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.Trust;

/// <summary>Tests for observation gate gap coverage.</summary>
public sealed class ObservationGateGapCoverageTests
{
    [Fact]
    public void Constructor_throws_for_null_boundary()
    {
        var act = () => new ObservationGate(null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("boundary");
    }

    [Fact]
    public void ShouldObserve_returns_false_when_observation_is_paused()
    {
        var boundary = new AccessBoundary();
        boundary.SetPause(true);
        var gate = new ObservationGate(boundary);

        gate.ShouldObserve("shell", "git").Should().BeFalse();
    }

    [Fact]
    public void ShouldObserve_returns_false_when_category_is_denied()
    {
        var boundary = new AccessBoundary();
        boundary.SetCategoryAllowed("shell", false);
        var gate = new ObservationGate(boundary);

        gate.ShouldObserve("shell", "git").Should().BeFalse();
    }

    [Fact]
    public void ShouldObserve_returns_false_when_source_is_denied_for_project()
    {
        var boundary = new AccessBoundary();
        boundary.SetSourceAllowed("git", false);
        var gate = new ObservationGate(boundary);

        gate.ShouldObserve("files", "git", "/repo").Should().BeFalse();
    }

    [Fact]
    public void ShouldObserve_returns_true_when_boundary_allows()
    {
        var boundary = new AccessBoundary();
        var gate = new ObservationGate(boundary);

        gate.ShouldObserve("files", "git", "/repo").Should().BeTrue();
    }

    [Fact]
    public void ShouldObserve_treats_null_category_and_source_as_blank()
    {
        var boundary = new AccessBoundary();
        var gate = new ObservationGate(boundary);

        gate.ShouldObserve(null!, null!).Should().BeFalse();
    }

    [Fact]
    public void ShouldObserve_allows_source_via_project_override_when_global_denies()
    {
        var boundary = new AccessBoundary();
        boundary.SetSourceAllowed("git", false);
        boundary.SetProjectOverride("/repo", new Dictionary<string, bool> { ["git"] = true });
        var gate = new ObservationGate(boundary);

        gate.ShouldObserve("files", "git", "/repo").Should().BeTrue();
    }
}
