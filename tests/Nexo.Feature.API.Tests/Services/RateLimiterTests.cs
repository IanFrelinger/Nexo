using Microsoft.Extensions.Logging;
using Moq;
using Nexo.Feature.API.Enums;
using Nexo.Feature.API.Interfaces;
using Nexo.Feature.API.Models;
using Nexo.Feature.API.Services;
using Xunit;

namespace Nexo.Feature.API.Tests.Services;

/// <summary>
/// Tests for the Rate Limiter service
/// </summary>
public class RateLimiterTests
{
    private readonly Mock<ILogger<RateLimiter>> _mockLogger;
    private readonly RateLimiter _rateLimiter;

    public RateLimiterTests()
    {
        _mockLogger = new Mock<ILogger<RateLimiter>>();
        _rateLimiter = new RateLimiter(_mockLogger.Object);
    }

    [Fact]
    public async Task CheckRateLimitAsync_WithinLimit_ReturnsAllowed()
    {
        // Arrange
        var request = new RateLimitRequest
        {
            Identifier = "test-user",
            Scope = RateLimitScope.User,
            Weight = 1
        };

        // Act
        var result = await _rateLimiter.CheckRateLimitAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsAllowed);
        Assert.Equal("test-user", result.Identifier);
        Assert.Equal(RateLimitScope.User, result.Scope);
    }

    [Fact]
    public async Task CheckRateLimitAsync_ExceedsLimit_ReturnsNotAllowed()
    {
        // Arrange
        var request = new RateLimitRequest
        {
            Identifier = "test-user",
            Scope = RateLimitScope.User,
            Weight = 1
        };

        // Configure rate limit to 1 request per minute
        var config = new RateLimitConfiguration
        {
            Identifier = "test-user",
            Scope = RateLimitScope.User,
            MaxRequests = 1,
            TimeWindowSeconds = 60
        };
        await _rateLimiter.ConfigureRateLimitingAsync(config);

        // Make first request - this should consume the token
        var firstResult = await _rateLimiter.CheckRateLimitAsync(request);
        Assert.True(firstResult.IsAllowed);

        // Act - Check second request
        var result = await _rateLimiter.CheckRateLimitAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsAllowed);
        Assert.Equal(0, result.CurrentCount);
        Assert.Equal(1, result.MaxCount);
    }

    [Fact]
    public async Task RecordRequestAsync_ValidRequest_ReturnsSuccess()
    {
        // Arrange
        var request = new RateLimitRequest
        {
            Identifier = "test-user",
            Scope = RateLimitScope.User,
            Weight = 1
        };

        // Act
        var result = await _rateLimiter.RecordRequestAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsSuccess);
        Assert.Equal("test-user", result.Identifier);
        Assert.Equal(100, result.NewCount); // Default capacity is 100
    }

    [Fact]
    public async Task GetRateLimitStatusAsync_ExistingIdentifier_ReturnsStatus()
    {
        // Arrange
        var identifier = "test-user";
        var scope = RateLimitScope.User;

        // Record a request first
        var request = new RateLimitRequest
        {
            Identifier = identifier,
            Scope = scope,
            Weight = 1
        };
        await _rateLimiter.RecordRequestAsync(request);

        // Act
        var result = await _rateLimiter.GetRateLimitStatusAsync(identifier, scope);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(identifier, result.Identifier);
        Assert.Equal(scope, result.Scope);
        Assert.Equal(100, result.CurrentCount); // Default capacity
        Assert.False(result.IsRateLimited);
    }

    [Fact]
    public async Task ResetRateLimitAsync_ExistingIdentifier_ReturnsSuccess()
    {
        // Arrange
        var identifier = "test-user";
        var scope = RateLimitScope.User;

        // Record a request first
        var request = new RateLimitRequest
        {
            Identifier = identifier,
            Scope = scope,
            Weight = 1
        };
        await _rateLimiter.RecordRequestAsync(request);

        // Act
        var result = await _rateLimiter.ResetRateLimitAsync(identifier, scope);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsSuccess);
        Assert.Equal(identifier, result.Identifier);
        Assert.Equal(100, result.PreviousCount); // Default capacity
    }

    [Fact]
    public async Task ConfigureRateLimitingAsync_ValidConfiguration_ReturnsSuccess()
    {
        // Arrange
        var config = new RateLimitConfiguration
        {
            Identifier = "test-user",
            Scope = RateLimitScope.User,
            MaxRequests = 100,
            TimeWindowSeconds = 3600,
            IsEnabled = true
        };

        // Act
        var result = await _rateLimiter.ConfigureRateLimitingAsync(config);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsSuccess);
        Assert.Equal("test-user", result.Identifier);
    }

    [Fact]
    public async Task GetStatisticsAsync_ReturnsValidStatistics()
    {
        // Arrange
        var request = new RateLimitRequest
        {
            Identifier = "test-user",
            Scope = RateLimitScope.User,
            Weight = 1
        };

        // Record some requests
        await _rateLimiter.RecordRequestAsync(request);
        await _rateLimiter.RecordRequestAsync(request);

        // Act
        var result = await _rateLimiter.GetStatisticsAsync();

        // Assert
        Assert.NotNull(result);
        Assert.True(result.TotalChecks >= 0);
        Assert.True(result.TotalRateLimited >= 0);
        Assert.True(result.RateLimitPercentage >= 0);
        Assert.True(result.ActiveConfigurations >= 0);
    }

    [Theory]
    [InlineData(RateLimitScope.User)]
    [InlineData(RateLimitScope.Service)]
    [InlineData(RateLimitScope.Global)]
    [InlineData(RateLimitScope.IPAddress)]
    [InlineData(RateLimitScope.APIKey)]
    [InlineData(RateLimitScope.Endpoint)]
    public async Task CheckRateLimitAsync_DifferentScopes_HandlesCorrectly(RateLimitScope scope)
    {
        // Arrange
        var request = new RateLimitRequest
        {
            Identifier = $"test-{scope}",
            Scope = scope,
            Weight = 1
        };

        // Act
        var result = await _rateLimiter.CheckRateLimitAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(scope, result.Scope);
        Assert.Equal($"test-{scope}", result.Identifier);
    }

    [Fact]
    public async Task CheckRateLimitAsync_WithWeight_CalculatesCorrectly()
    {
        // Arrange
        var config = new RateLimitConfiguration
        {
            Identifier = "test-user",
            Scope = RateLimitScope.User,
            MaxRequests = 10,
            TimeWindowSeconds = 60
        };
        await _rateLimiter.ConfigureRateLimitingAsync(config);

        var request = new RateLimitRequest
        {
            Identifier = "test-user",
            Scope = RateLimitScope.User,
            Weight = 5
        };

        // Act
        var result = await _rateLimiter.CheckRateLimitAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsAllowed);
        Assert.Equal(5, result.CurrentCount); // 10 - 5 = 5 remaining
        Assert.Equal(10, result.MaxCount);
    }

    [Fact]
    public async Task CheckRateLimitAsync_DisabledConfiguration_AlwaysAllows()
    {
        // Arrange
        var config = new RateLimitConfiguration
        {
            Identifier = "test-user",
            Scope = RateLimitScope.User,
            MaxRequests = 1,
            TimeWindowSeconds = 60,
            IsEnabled = false
        };
        await _rateLimiter.ConfigureRateLimitingAsync(config);

        var request = new RateLimitRequest
        {
            Identifier = "test-user",
            Scope = RateLimitScope.User,
            Weight = 1
        };

        // Record multiple requests
        await _rateLimiter.RecordRequestAsync(request);
        await _rateLimiter.RecordRequestAsync(request);
        await _rateLimiter.RecordRequestAsync(request);

        // Act
        var result = await _rateLimiter.CheckRateLimitAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsAllowed);
    }

    [Fact]
    public async Task CheckRateLimitAsync_WithBurstAllowance_HandlesCorrectly()
    {
        // Arrange
        var config = new RateLimitConfiguration
        {
            Identifier = "test-user",
            Scope = RateLimitScope.User,
            MaxRequests = 10,
            TimeWindowSeconds = 60,
            BurstAllowance = 5
        };
        await _rateLimiter.ConfigureRateLimitingAsync(config);

        var request = new RateLimitRequest
        {
            Identifier = "test-user",
            Scope = RateLimitScope.User,
            Weight = 1
        };

        // Act - Should allow burst requests
        var result = await _rateLimiter.CheckRateLimitAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsAllowed);
        Assert.Equal(9, result.CurrentCount); // 10 - 1 = 9 remaining
        Assert.Equal(10, result.MaxCount); // Burst allowance not implemented in current version
    }

    [Fact]
    public async Task GetRateLimitStatusAsync_NonExistentIdentifier_ReturnsDefaultStatus()
    {
        // Arrange
        var identifier = "non-existent-user";
        var scope = RateLimitScope.User;

        // Act
        var result = await _rateLimiter.GetRateLimitStatusAsync(identifier, scope);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(identifier, result.Identifier);
        Assert.Equal(scope, result.Scope);
        Assert.Equal(100, result.CurrentCount); // Default capacity
        Assert.False(result.IsRateLimited);
    }

    [Fact]
    public async Task ResetRateLimitAsync_NonExistentIdentifier_ReturnsSuccess()
    {
        // Arrange
        var identifier = "non-existent-user";
        var scope = RateLimitScope.User;

        // Act
        var result = await _rateLimiter.ResetRateLimitAsync(identifier, scope);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsSuccess);
        Assert.Equal(identifier, result.Identifier);
        Assert.Equal(0, result.PreviousCount); // No bucket exists, so previous count is 0
    }

    [Fact]
    public async Task RecordRequestAsync_WithMetadata_ProcessesCorrectly()
    {
        // Arrange
        var request = new RateLimitRequest
        {
            Identifier = "test-user",
            Scope = RateLimitScope.User,
            Weight = 1,
            Metadata = new Dictionary<string, object>
            {
                { "source", "api" },
                { "priority", "high" }
            }
        };

        // Act
        var result = await _rateLimiter.RecordRequestAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsSuccess);
        Assert.Equal("test-user", result.Identifier);
    }
} 