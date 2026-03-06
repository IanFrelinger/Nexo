using FluentAssertions;
using Nexo.Core.Application.Trust.Ports;
using Xunit;

namespace Nexo.Tests.Contracts;

/// <summary>
/// Contract tests for IAccessBoundary implementations.
/// Every implementation must pass these tests to ensure consistent behavior.
/// </summary>
public abstract class AccessBoundaryContractTests
{
    protected abstract IAccessBoundary CreateInstance();

    [Fact]
    public void SetCategoryAllowed_IsCategoryAllowed_ReturnsSameValue()
    {
        var boundary = CreateInstance();
        boundary.SetCategoryAllowed("file-paths", true);
        boundary.IsCategoryAllowed("file-paths").Should().BeTrue();

        boundary.SetCategoryAllowed("terminal-output", false);
        boundary.IsCategoryAllowed("terminal-output").Should().BeFalse();
    }

    [Fact]
    public void SetSourceAllowed_IsSourceAllowed_ReturnsSameValue()
    {
        var boundary = CreateInstance();
        boundary.SetSourceAllowed("vscode", true);
        boundary.IsSourceAllowed("vscode").Should().BeTrue();

        boundary.SetSourceAllowed("git", false);
        boundary.IsSourceAllowed("git").Should().BeFalse();
    }

    [Fact]
    public void SetPause_IsObservationPaused_ReturnsSameValue()
    {
        var boundary = CreateInstance();
        boundary.SetPause(true);
        boundary.IsObservationPaused.Should().BeTrue();

        boundary.SetPause(false);
        boundary.IsObservationPaused.Should().BeFalse();
    }
}
