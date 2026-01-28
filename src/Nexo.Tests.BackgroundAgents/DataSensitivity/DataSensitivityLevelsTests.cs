using FluentAssertions;
using Nexo.BackgroundAgents.DataSensitivity;
using Xunit;

namespace Nexo.Tests.BackgroundAgents.DataSensitivity;

/// <summary>
/// Tests for primitive data sensitivity levels.
/// </summary>
public class DataSensitivityLevelsTests
{
    [Fact]
    public void FromName_WithValidName_ReturnsLevel()
    {
        // Act
        var publicLevel = DataSensitivityLevels.FromName("Public");
        var internalLevel = DataSensitivityLevels.FromName("Internal");
        var confidentialLevel = DataSensitivityLevels.FromName("Confidential");
        var secretLevel = DataSensitivityLevels.FromName("Secret");
        var topSecretLevel = DataSensitivityLevels.FromName("TopSecret");

        // Assert
        publicLevel.Should().NotBeNull();
        publicLevel!.Value.Should().Be("Public");
        internalLevel.Should().NotBeNull();
        internalLevel!.Value.Should().Be("Internal");
        confidentialLevel.Should().NotBeNull();
        confidentialLevel!.Value.Should().Be("Confidential");
        secretLevel.Should().NotBeNull();
        secretLevel!.Value.Should().Be("Secret");
        topSecretLevel.Should().NotBeNull();
        topSecretLevel!.Value.Should().Be("TopSecret");
    }

    [Fact]
    public void FromName_WithCaseInsensitiveName_ReturnsLevel()
    {
        // Act
        var level1 = DataSensitivityLevels.FromName("public");
        var level2 = DataSensitivityLevels.FromName("PUBLIC");
        var level3 = DataSensitivityLevels.FromName("top-secret");

        // Assert
        level1.Should().Be(DataSensitivityLevels.Public);
        level2.Should().Be(DataSensitivityLevels.Public);
        level3.Should().Be(DataSensitivityLevels.TopSecret);
    }

    [Fact]
    public void FromName_WithInvalidName_ReturnsNull()
    {
        // Act
        var level = DataSensitivityLevels.FromName("InvalidLevel");

        // Assert
        level.Should().BeNull();
    }

    [Fact]
    public void FromName_WithNull_ReturnsNull()
    {
        // Act
        var level = DataSensitivityLevels.FromName(null);

        // Assert
        level.Should().BeNull();
    }

    [Fact]
    public void All_ContainsAllPrimitiveLevels()
    {
        // Act
        var all = DataSensitivityLevels.All;

        // Assert
        all.Should().HaveCount(5);
        all.Should().Contain(DataSensitivityLevels.Public);
        all.Should().Contain(DataSensitivityLevels.Internal);
        all.Should().Contain(DataSensitivityLevels.Confidential);
        all.Should().Contain(DataSensitivityLevels.Secret);
        all.Should().Contain(DataSensitivityLevels.TopSecret);
    }

    [Fact]
    public void All_IsOrderedBySensitivityValue()
    {
        // Act
        var all = DataSensitivityLevels.All;

        // Assert
        all[0].SensitivityValue.Should().Be(0); // Public
        all[1].SensitivityValue.Should().Be(1); // Internal
        all[2].SensitivityValue.Should().Be(2); // Confidential
        all[3].SensitivityValue.Should().Be(3); // Secret
        all[4].SensitivityValue.Should().Be(4); // TopSecret
    }

    [Fact]
    public void Public_AllowsAllOperations()
    {
        // Assert
        DataSensitivityLevels.Public.AllowsExternalLLM.Should().BeTrue();
        DataSensitivityLevels.Public.AllowsWebSearch.Should().BeTrue();
        DataSensitivityLevels.Public.RequiresLocalOnly.Should().BeFalse();
        DataSensitivityLevels.Public.AllowsNetworkExports.Should().BeTrue();
    }

    [Fact]
    public void Confidential_BlocksExternalLLM()
    {
        // Assert
        DataSensitivityLevels.Confidential.AllowsExternalLLM.Should().BeFalse();
        DataSensitivityLevels.Confidential.AllowsWebSearch.Should().BeTrue();
        DataSensitivityLevels.Confidential.RequiresLocalOnly.Should().BeFalse();
        DataSensitivityLevels.Confidential.AllowsNetworkExports.Should().BeFalse();
    }

    [Fact]
    public void Secret_BlocksExternalLLMAndWebSearch()
    {
        // Assert
        DataSensitivityLevels.Secret.AllowsExternalLLM.Should().BeFalse();
        DataSensitivityLevels.Secret.AllowsWebSearch.Should().BeFalse();
        DataSensitivityLevels.Secret.RequiresLocalOnly.Should().BeFalse();
        DataSensitivityLevels.Secret.AllowsNetworkExports.Should().BeFalse();
    }

    [Fact]
    public void TopSecret_RequiresLocalOnly()
    {
        // Assert
        DataSensitivityLevels.TopSecret.AllowsExternalLLM.Should().BeFalse();
        DataSensitivityLevels.TopSecret.AllowsWebSearch.Should().BeFalse();
        DataSensitivityLevels.TopSecret.RequiresLocalOnly.Should().BeTrue();
        DataSensitivityLevels.TopSecret.AllowsNetworkExports.Should().BeFalse();
    }
}
