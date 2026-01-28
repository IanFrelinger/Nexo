using FluentAssertions;
using Nexo.BackgroundAgents.DataSensitivity;
using Xunit;

namespace Nexo.Tests.BackgroundAgents.DataSensitivity;

/// <summary>
/// Tests for CustomSensitivityLevelFactory.
/// </summary>
public class CustomSensitivityLevelFactoryTests
{
    [Fact]
    public void RegisterFromConfig_WithValidConfig_RegistersLevels()
    {
        // Arrange
        var registry = new DataSensitivityRegistry();
        var factory = new CustomSensitivityLevelFactory(registry);
        var config = new Dictionary<string, CustomSensitivityLevel>
        {
            ["CustomerData"] = new CustomSensitivityLevel
            {
                Name = "CustomerData",
                SensitivityValue = 2,
                AllowsExternalLLM = false,
                AllowsWebSearch = false,
                RequiresLocalOnly = false,
                AllowsNetworkExports = false,
                Description = "Customer data"
            }
        };

        // Act
        factory.RegisterFromConfig(config);

        // Assert
        var level = registry.GetByName("CustomerData");
        level.Should().NotBeNull();
        level!.Value.Should().Be("CustomerData");
        level.SensitivityValue.Should().Be(2);
    }

    [Fact]
    public void RegisterFromConfig_WithMultipleLevels_RegistersAll()
    {
        // Arrange
        var registry = new DataSensitivityRegistry();
        var factory = new CustomSensitivityLevelFactory(registry);
        var config = new Dictionary<string, CustomSensitivityLevel>
        {
            ["Level1"] = new CustomSensitivityLevel
            {
                Name = "Level1",
                SensitivityValue = 1,
                AllowsExternalLLM = true,
                AllowsWebSearch = true,
                RequiresLocalOnly = false,
                AllowsNetworkExports = true,
                Description = "Level 1"
            },
            ["Level2"] = new CustomSensitivityLevel
            {
                Name = "Level2",
                SensitivityValue = 2,
                AllowsExternalLLM = false,
                AllowsWebSearch = true,
                RequiresLocalOnly = false,
                AllowsNetworkExports = false,
                Description = "Level 2"
            }
        };

        // Act
        factory.RegisterFromConfig(config);

        // Assert
        registry.GetByName("Level1").Should().NotBeNull();
        registry.GetByName("Level2").Should().NotBeNull();
    }

    [Fact]
    public void RegisterFromConfig_WithNullConfig_DoesNothing()
    {
        // Arrange
        var registry = new DataSensitivityRegistry();
        var factory = new CustomSensitivityLevelFactory(registry);

        // Act
        factory.RegisterFromConfig(null);

        // Assert - should not throw
        registry.GetAll().Should().HaveCount(5); // Only primitives
    }

    [Fact]
    public void RegisterFromConfig_WithEmptyConfig_DoesNothing()
    {
        // Arrange
        var registry = new DataSensitivityRegistry();
        var factory = new CustomSensitivityLevelFactory(registry);
        var config = new Dictionary<string, CustomSensitivityLevel>();

        // Act
        factory.RegisterFromConfig(config);

        // Assert - should not throw
        registry.GetAll().Should().HaveCount(5); // Only primitives
    }

    [Fact]
    public void RegisterFromConfig_WithNameMismatch_ThrowsException()
    {
        // Arrange
        var registry = new DataSensitivityRegistry();
        var factory = new CustomSensitivityLevelFactory(registry);
        var config = new Dictionary<string, CustomSensitivityLevel>
        {
            ["KeyName"] = new CustomSensitivityLevel
            {
                Name = "DifferentName", // Mismatch
                SensitivityValue = 1,
                AllowsExternalLLM = true,
                AllowsWebSearch = true,
                RequiresLocalOnly = false,
                AllowsNetworkExports = true,
                Description = "Test"
            }
        };

        // Act & Assert
        var act = () => factory.RegisterFromConfig(config);
        act.Should().Throw<ArgumentException>()
            .WithMessage("*does not match key*");
    }

    [Fact]
    public void RegisterFromConfig_WithEmptyName_ThrowsException()
    {
        // Arrange
        var registry = new DataSensitivityRegistry();
        var factory = new CustomSensitivityLevelFactory(registry);
        var config = new Dictionary<string, CustomSensitivityLevel>
        {
            ["TestLevel"] = new CustomSensitivityLevel
            {
                Name = "", // Empty name
                SensitivityValue = 1,
                AllowsExternalLLM = true,
                AllowsWebSearch = true,
                RequiresLocalOnly = false,
                AllowsNetworkExports = true,
                Description = "Test"
            }
        };

        // Act & Assert
        var act = () => factory.RegisterFromConfig(config);
        act.Should().Throw<ArgumentException>()
            .WithMessage("*has no name*");
    }
}
