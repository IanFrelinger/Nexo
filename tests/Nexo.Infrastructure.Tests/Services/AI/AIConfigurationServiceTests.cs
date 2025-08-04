using System.Text.Json;
using Microsoft.Extensions.Logging;
using Moq;
using Nexo.Feature.AI.Enums;
using Nexo.Feature.AI.Models;
using Nexo.Infrastructure.Services.AI;
using Xunit;

namespace Nexo.Infrastructure.Tests.Services.AI;

/// <summary>
/// Unit tests for AIConfigurationService.
/// </summary>
public class AIConfigurationServiceTests
{
    private readonly Mock<ILogger<AIConfigurationService>> _mockLogger;
    private readonly AIConfigurationService _service;

    public AIConfigurationServiceTests()
    {
        _mockLogger = new Mock<ILogger<AIConfigurationService>>();
        _service = new AIConfigurationService(_mockLogger.Object);
    }

    [Fact]
    public async Task GetConfigurationAsync_WhenConfigurationExists_ReturnsConfiguration()
    {
        // Act
        var result = await _service.GetConfigurationAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(AIMode.Development, result.Mode);
        Assert.Equal("gpt-3.5-turbo", result.Model.Name);
    }

    [Fact]
    public async Task SaveConfigurationAsync_WhenValidConfiguration_SavesSuccessfully()
    {
        // Arrange
        var config = new AIConfiguration
        {
            Mode = AIMode.AIHeavy,
            Model = new AIModelConfiguration { Name = "gpt-4-turbo" }
        };

        // Act
        await _service.SaveConfigurationAsync(config);

        // Assert - Verify the configuration was saved by getting it back
        var savedConfig = await _service.GetConfigurationAsync();
        Assert.Equal(AIMode.AIHeavy, savedConfig.Mode);
        Assert.Equal("gpt-4-turbo", savedConfig.Model.Name);
    }

    [Fact]
    public async Task LoadForModeAsync_WhenModeIsDifferent_LoadsModeSpecificDefaults()
    {
        // Act
        var result = await _service.LoadForModeAsync(AIMode.Production);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(AIMode.Production, result.Mode);
        Assert.Equal("gpt-4", result.Model.Name);
        Assert.Equal(8192, result.Model.MaxInputTokens);
        Assert.Equal(0.5, result.Model.Temperature);
    }

    [Theory]
    [InlineData(AIMode.Development, "gpt-3.5-turbo", 4096, 0.3)]
    [InlineData(AIMode.Production, "gpt-4", 8192, 0.5)]
    [InlineData(AIMode.AIHeavy, "gpt-4-turbo", 16384, 0.7)]
    public void GetDefaultConfiguration_ForEachMode_ReturnsCorrectDefaults(AIMode mode, string expectedModel, int expectedInputTokens, double expectedTemperature)
    {
        // Act
        var result = _service.GetDefaultConfiguration(mode);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(mode, result.Mode);
        Assert.Equal(expectedModel, result.Model.Name);
        Assert.Equal(expectedInputTokens, result.Model.MaxInputTokens);
        Assert.Equal(expectedTemperature, result.Model.Temperature);
    }

    [Fact]
    public async Task ValidateAsync_WhenValidConfiguration_ReturnsValidResult()
    {
        // Arrange
        var config = new AIConfiguration
        {
            Model = new AIModelConfiguration
            {
                Name = "gpt-4",
                MaxInputTokens = 8192,
                MaxOutputTokens = 4096,
                Temperature = 0.5
            },
            Resources = new AIResourceConfiguration
            {
                MaxConcurrentRequests = 20,
                MaxMemoryUsageBytes = 2L * 1024L * 1024L * 1024L
            }
        };

        // Act
        var result = await _service.ValidateAsync(config);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public async Task ValidateAsync_WhenModelIsNull_ReturnsInvalidResult()
    {
        // Arrange
        var config = new AIConfiguration { Model = null! };

        // Act
        var result = await _service.ValidateAsync(config);

        // Assert
        Assert.False(result.IsValid);
        Assert.NotEmpty(result.Errors);
    }

    [Fact]
    public async Task ValidateAsync_WhenModelNameIsEmpty_ReturnsInvalidResult()
    {
        // Arrange
        var config = new AIConfiguration
        {
            Model = new AIModelConfiguration { Name = "" }
        };

        // Act
        var result = await _service.ValidateAsync(config);

        // Assert
        Assert.False(result.IsValid);
        Assert.NotEmpty(result.Errors);
        Assert.Contains(result.Errors, e => e.Code == "MODEL_NAME_EMPTY");
    }

    [Fact]
    public async Task ValidateAsync_WhenMaxInputTokensIsInvalid_ReturnsInvalidResult()
    {
        // Arrange
        var config = new AIConfiguration
        {
            Model = new AIModelConfiguration
            {
                Name = "gpt-4",
                MaxInputTokens = 0
            }
        };

        // Act
        var result = await _service.ValidateAsync(config);

        // Assert
        Assert.False(result.IsValid);
        Assert.NotEmpty(result.Errors);
        Assert.Contains(result.Errors, e => e.Code == "MAX_INPUT_TOKENS_INVALID");
    }

    [Fact]
    public async Task ValidateAsync_WhenMaxOutputTokensIsInvalid_ReturnsInvalidResult()
    {
        // Arrange
        var config = new AIConfiguration
        {
            Model = new AIModelConfiguration
            {
                Name = "gpt-4",
                MaxInputTokens = 8192,
                MaxOutputTokens = 0
            }
        };

        // Act
        var result = await _service.ValidateAsync(config);

        // Assert
        Assert.False(result.IsValid);
        Assert.NotEmpty(result.Errors);
        Assert.Contains(result.Errors, e => e.Code == "MAX_OUTPUT_TOKENS_INVALID");
    }

    [Fact]
    public async Task ValidateAsync_WhenMaxConcurrentRequestsIsInvalid_ReturnsInvalidResult()
    {
        // Arrange
        var config = new AIConfiguration
        {
            Model = new AIModelConfiguration
            {
                Name = "gpt-4",
                MaxInputTokens = 8192,
                MaxOutputTokens = 4096
            },
            Resources = new AIResourceConfiguration
            {
                MaxConcurrentRequests = 0
            }
        };

        // Act
        var result = await _service.ValidateAsync(config);

        // Assert
        Assert.False(result.IsValid);
        Assert.NotEmpty(result.Errors);
        Assert.Contains(result.Errors, e => e.Code == "MAX_CONCURRENT_REQUESTS_INVALID");
    }

    [Fact]
    public async Task ValidateAsync_WhenMaxMemoryUsageIsInvalid_ReturnsInvalidResult()
    {
        // Arrange
        var config = new AIConfiguration
        {
            Model = new AIModelConfiguration
            {
                Name = "gpt-4",
                MaxInputTokens = 8192,
                MaxOutputTokens = 4096
            },
            Resources = new AIResourceConfiguration
            {
                MaxConcurrentRequests = 20,
                MaxMemoryUsageBytes = 0
            }
        };

        // Act
        var result = await _service.ValidateAsync(config);

        // Assert
        Assert.False(result.IsValid);
        Assert.NotEmpty(result.Errors);
        Assert.Contains(result.Errors, e => e.Code == "MAX_MEMORY_USAGE_INVALID");
    }

    [Fact]
    public async Task MergeAsync_WhenMultipleConfigurations_ReturnsMergedConfiguration()
    {
        // Arrange
        var config1 = new AIConfiguration
        {
            Mode = AIMode.Development,
            Model = new AIModelConfiguration { Name = "gpt-3.5-turbo" }
        };

        var config2 = new AIConfiguration
        {
            Mode = AIMode.Production,
            Model = new AIModelConfiguration { Name = "gpt-4" }
        };

        var configs = new[] { config1, config2 };

        // Act
        var result = await _service.MergeAsync(configs);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(AIMode.Production, result.Mode); // Last non-default mode
        Assert.Equal("gpt-4", result.Model.Name); // Last model
    }

    [Fact]
    public async Task MergeAsync_WhenEmptyList_ReturnsDefaultConfiguration()
    {
        // Arrange
        var configs = Enumerable.Empty<AIConfiguration>();

        // Act
        var result = await _service.MergeAsync(configs);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(AIMode.Development, result.Mode);
    }

    [Fact]
    public void GetConfigurationPath_ReturnsExpectedPath()
    {
        // Act
        var result = _service.GetConfigurationPath();

        // Assert
        Assert.Equal("ai-config.json", result);
    }

    [Fact]
    public async Task ExistsAsync_ReturnsTrue()
    {
        // Act
        var result = await _service.ExistsAsync();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task ReloadAsync_WhenSuccessful_ReturnsReloadedConfiguration()
    {
        // Act
        var result = await _service.ReloadAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(AIMode.Development, result.Mode);
    }

    [Fact]
    public void Constructor_WhenLoggerIsNull_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new AIConfigurationService(null!));
    }

    [Fact]
    public async Task SaveConfigurationAsync_WhenConfigurationIsNull_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => _service.SaveConfigurationAsync(null!));
    }

    [Fact]
    public async Task ValidateAsync_WhenConfigurationIsNull_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => _service.ValidateAsync(null!));
    }

    [Fact]
    public async Task MergeAsync_WhenConfigurationsIsNull_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => _service.MergeAsync(null!));
    }
} 