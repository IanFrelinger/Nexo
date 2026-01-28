using FluentAssertions;
using Nexo.BackgroundAgents.DataSensitivity;
using Xunit;

namespace Nexo.Tests.BackgroundAgents.DataSensitivity;

/// <summary>
/// Tests for DataSensitivityMarker.
/// </summary>
public class DataSensitivityMarkerTests
{
    [Fact]
    public void GetSensitivityLevel_WithUnmarkedData_ReturnsPublic()
    {
        // Arrange
        var registry = new DataSensitivityRegistry();
        var marker = new DataSensitivityMarker(registry);
        var data = new { Value = "test" };

        // Act
        var level = marker.GetSensitivityLevel(data);

        // Assert
        level.Should().Be(DataSensitivityLevels.Public);
    }

    [Fact]
    public void MarkSensitivity_WithLevel_MarksData()
    {
        // Arrange
        var registry = new DataSensitivityRegistry();
        var marker = new DataSensitivityMarker(registry);
        var data = new { Value = "test" };

        // Act
        marker.MarkSensitivity(data, DataSensitivityLevels.Confidential);
        var level = marker.GetSensitivityLevel(data);

        // Assert
        level.Should().Be(DataSensitivityLevels.Confidential);
    }

    [Fact]
    public void MarkSensitivity_WithLevelName_MarksData()
    {
        // Arrange
        var registry = new DataSensitivityRegistry();
        var marker = new DataSensitivityMarker(registry);
        var data = new { Value = "test" };

        // Act
        marker.MarkSensitivity(data, "Confidential");
        var level = marker.GetSensitivityLevel(data);

        // Assert
        level.Should().Be(DataSensitivityLevels.Confidential);
    }

    [Fact]
    public void MarkSensitivity_WithInvalidLevelName_ThrowsException()
    {
        // Arrange
        var registry = new DataSensitivityRegistry();
        var marker = new DataSensitivityMarker(registry);
        var data = new { Value = "test" };

        // Act & Assert
        var act = () => marker.MarkSensitivity(data, "InvalidLevel");
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Unknown sensitivity level*");
    }

    [Fact]
    public void CanAccess_WithAccessibleData_ReturnsTrue()
    {
        // Arrange
        var registry = new DataSensitivityRegistry();
        var marker = new DataSensitivityMarker(registry);
        var data = new { Value = "test" };
        marker.MarkSensitivity(data, DataSensitivityLevels.Internal);

        // Act
        var canAccess = marker.CanAccess(DataSensitivityLevels.Confidential, data);

        // Assert
        canAccess.Should().BeTrue();
    }

    [Fact]
    public void CanAccess_WithInaccessibleData_ReturnsFalse()
    {
        // Arrange
        var registry = new DataSensitivityRegistry();
        var marker = new DataSensitivityMarker(registry);
        var data = new { Value = "test" };
        marker.MarkSensitivity(data, DataSensitivityLevels.Secret);

        // Act
        var canAccess = marker.CanAccess(DataSensitivityLevels.Internal, data);

        // Assert
        canAccess.Should().BeFalse();
    }

    [Fact]
    public void CanAccess_WithUnmarkedData_ReturnsTrue()
    {
        // Arrange
        var registry = new DataSensitivityRegistry();
        var marker = new DataSensitivityMarker(registry);
        var data = new { Value = "test" };

        // Act
        var canAccess = marker.CanAccess(DataSensitivityLevels.Internal, data);

        // Assert
        canAccess.Should().BeTrue(); // Unmarked data defaults to Public, which is accessible
    }
}
