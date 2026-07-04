using FluentAssertions;
using Nexo.Core.Application.Trust.Models;
using Nexo.Core.Application.Trust.Ports;
using Nexo.Infrastructure.Trust;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.Trust;

/// <summary>Tests for access boundary.</summary>
public class AccessBoundaryTests
{
    [Fact]
    public void IsObservationPaused_ByDefault_IsFalse()
    {
        var boundary = new AccessBoundary();

        boundary.IsObservationPaused.Should().BeFalse();
    }

    [Fact]
    public void SetPause_True_PausesObservation()
    {
        var boundary = new AccessBoundary();

        boundary.SetPause(true);

        boundary.IsObservationPaused.Should().BeTrue();
    }

    [Fact]
    public void SetPause_False_ResumesObservation()
    {
        var boundary = new AccessBoundary();
        boundary.SetPause(true);

        boundary.SetPause(false);

        boundary.IsObservationPaused.Should().BeFalse();
    }

    [Fact]
    public void IsCategoryAllowed_WhenNotSet_ReturnsTrue()
    {
        var boundary = new AccessBoundary();

        boundary.IsCategoryAllowed("file-paths").Should().BeTrue();
    }

    [Fact]
    public void SetCategoryAllowed_Denied_ReturnsFalse()
    {
        var boundary = new AccessBoundary();
        boundary.SetCategoryAllowed("file-paths", false);

        boundary.IsCategoryAllowed("file-paths").Should().BeFalse();
    }

    [Fact]
    public void IsSourceAllowed_WhenNotSet_ReturnsTrue()
    {
        var boundary = new AccessBoundary();

        boundary.IsSourceAllowed("vscode").Should().BeTrue();
    }

    [Fact]
    public void SetSourceAllowed_Denied_ReturnsFalse()
    {
        var boundary = new AccessBoundary();
        boundary.SetSourceAllowed("git", false);

        boundary.IsSourceAllowed("git").Should().BeFalse();
    }

    [Fact]
    public void SetProjectOverride_OverridesSourceForProject()
    {
        var boundary = new AccessBoundary();
        boundary.SetSourceAllowed("knowledge-indexer", true);
        boundary.SetProjectOverride("/secret", new Dictionary<string, bool> { ["knowledge-indexer"] = false });

        boundary.IsSourceAllowedForProject("knowledge-indexer", "/secret").Should().BeFalse();
        boundary.IsSourceAllowedForProject("knowledge-indexer", "/other").Should().BeTrue();
    }

    [Fact]
    public void BoundaryChanged_RaisedOnSetPause()
    {
        var boundary = new AccessBoundary();
        BoundaryChangeEvent? captured = null;
        boundary.BoundaryChanged += evt => captured = evt;

        boundary.SetPause(true);

        captured.Should().NotBeNull();
        captured!.ChangeType.Should().Be("pause");
        captured.NewState.Should().Be("paused");
    }
}
