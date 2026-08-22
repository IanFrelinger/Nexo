using FluentAssertions;
using Ashlar.BackgroundAgents.DataSensitivity;
using Xunit;

namespace Ashlar.Tests.BackgroundAgents.DataSensitivity;

/// <summary>
/// Tests for DataSensitivityRegistry.
/// </summary>
public class DataSensitivityRegistryTests
{
    [Fact]
    public void GetByName_WithPrimitiveName_ReturnsPrimitiveLevel()
    {
        // Arrange
        var registry = new DataSensitivityRegistry();

        // Act
        var level = registry.GetByName("Public");

        // Assert
        level.Should().Be(DataSensitivityLevels.Public);
    }

    [Fact]
    public void GetByName_WithCustomLevel_ReturnsCustomLevel()
    {
        // Arrange
        var registry = new DataSensitivityRegistry();
        var customLevel = new ConfigurableSensitivityLevel(
            "CustomLevel",
            "Custom Level",
            5,
            AllowsExternalLLM: false,
            AllowsWebSearch: false,
            RequiresLocalOnly: true,
            AllowsNetworkExports: false,
            "Custom level for testing");
        registry.Register(customLevel);

        // Act
        var level = registry.GetByName("CustomLevel");

        // Assert
        level.Should().Be(customLevel);
    }

    [Fact]
    public void GetByName_WithUnknownName_ReturnsNull()
    {
        // Arrange
        var registry = new DataSensitivityRegistry();

        // Act
        var level = registry.GetByName("UnknownLevel");

        // Assert
        level.Should().BeNull();
    }

    [Fact]
    public void Register_WithCustomLevel_AddsToRegistry()
    {
        // Arrange
        var registry = new DataSensitivityRegistry();
        var customLevel = new ConfigurableSensitivityLevel(
            "TestLevel",
            "Test Level",
            2,
            AllowsExternalLLM: true,
            AllowsWebSearch: true,
            RequiresLocalOnly: false,
            AllowsNetworkExports: true,
            "Test level");

        // Act
        registry.Register(customLevel);

        // Assert
        var retrieved = registry.GetByName("TestLevel");
        retrieved.Should().Be(customLevel);
    }

    [Fact]
    public void Register_WithDuplicateName_ThrowsException()
    {
        // Arrange
        var registry = new DataSensitivityRegistry();
        var level1 = new ConfigurableSensitivityLevel(
            "Duplicate",
            "Duplicate",
            1,
            AllowsExternalLLM: true,
            AllowsWebSearch: true,
            RequiresLocalOnly: false,
            AllowsNetworkExports: true,
            "First");
        var level2 = new ConfigurableSensitivityLevel(
            "Duplicate",
            "Duplicate",
            2,
            AllowsExternalLLM: false,
            AllowsWebSearch: false,
            RequiresLocalOnly: false,
            AllowsNetworkExports: false,
            "Second");
        registry.Register(level1);

        // Act & Assert
        var act = () => registry.Register(level2);
        act.Should().Throw<ArgumentException>()
            .WithMessage("*already registered*");
    }

    [Fact]
    public void Register_WithPrimitiveName_ThrowsException()
    {
        // Arrange
        var registry = new DataSensitivityRegistry();
        var customLevel = new ConfigurableSensitivityLevel(
            "Public", // Conflicts with primitive
            "Public",
            1,
            AllowsExternalLLM: true,
            AllowsWebSearch: true,
            RequiresLocalOnly: false,
            AllowsNetworkExports: true,
            "Custom public");

        // Act & Assert
        var act = () => registry.Register(customLevel);
        act.Should().Throw<ArgumentException>()
            .WithMessage("*conflicts with primitive*");
    }

    [Fact]
    public void GetAll_ReturnsPrimitivesAndCustomLevels()
    {
        // Arrange
        var registry = new DataSensitivityRegistry();
        var customLevel = new ConfigurableSensitivityLevel(
            "CustomLevel",
            "Custom Level",
            2,
            AllowsExternalLLM: false,
            AllowsWebSearch: true,
            RequiresLocalOnly: false,
            AllowsNetworkExports: false,
            "Custom");
        registry.Register(customLevel);

        // Act
        var all = registry.GetAll();

        // Assert
        all.Should().HaveCount(6); // 5 primitives + 1 custom
        all.Should().Contain(DataSensitivityLevels.Public);
        all.Should().Contain(DataSensitivityLevels.Internal);
        all.Should().Contain(DataSensitivityLevels.Confidential);
        all.Should().Contain(DataSensitivityLevels.Secret);
        all.Should().Contain(DataSensitivityLevels.TopSecret);
        all.Should().Contain(customLevel);
    }

    [Fact]
    public void GetAll_IsOrderedBySensitivityValue()
    {
        // Arrange
        var registry = new DataSensitivityRegistry();
        var customLevel = new ConfigurableSensitivityLevel(
            "CustomLevel",
            "Custom Level",
            2,
            AllowsExternalLLM: false,
            AllowsWebSearch: true,
            RequiresLocalOnly: false,
            AllowsNetworkExports: false,
            "Custom");
        registry.Register(customLevel);

        // Act
        var all = registry.GetAll();

        // Assert
        all.Should().BeInAscendingOrder(l => l.SensitivityValue);
    }

    [Fact]
    public void CanAccess_WithLowerSensitivity_ReturnsTrue()
    {
        // Arrange
        var registry = new DataSensitivityRegistry();

        // Act & Assert
        registry.CanAccess(DataSensitivityLevels.Internal, DataSensitivityLevels.Public).Should().BeTrue();
        registry.CanAccess(DataSensitivityLevels.Internal, DataSensitivityLevels.Internal).Should().BeTrue();
    }

    [Fact]
    public void CanAccess_WithHigherSensitivity_ReturnsFalse()
    {
        // Arrange
        var registry = new DataSensitivityRegistry();

        // Act & Assert
        registry.CanAccess(DataSensitivityLevels.Internal, DataSensitivityLevels.Confidential).Should().BeFalse();
        registry.CanAccess(DataSensitivityLevels.Internal, DataSensitivityLevels.Secret).Should().BeFalse();
    }

    [Fact]
    public void CanAccess_WithSameSensitivity_ReturnsTrue()
    {
        // Arrange
        var registry = new DataSensitivityRegistry();

        // Act & Assert
        registry.CanAccess(DataSensitivityLevels.Confidential, DataSensitivityLevels.Confidential).Should().BeTrue();
    }
}
