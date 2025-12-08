using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Nexo.Orchestration.RateLimiting;
using Xunit;

namespace Nexo.Tests.Orchestration.RateLimiting;

public class RateLimiterTests
{
    private readonly Mock<ILogger<RateLimiter>> _loggerMock;
    private readonly RateLimiter _rateLimiter;

    public RateLimiterTests()
    {
        _loggerMock = new Mock<ILogger<RateLimiter>>();
        _rateLimiter = new RateLimiter("Test", maxRequests: 5, window: TimeSpan.FromSeconds(1), _loggerMock.Object);
    }

    [Fact]
    public async Task AcquireAsync_ShouldAllow_WithinLimit()
    {
        // Act
        var result = await _rateLimiter.AcquireAsync("test");

        // Assert
        result.Allowed.Should().BeTrue();
        result.RemainingRequests.Should().Be(4);
    }

    [Fact]
    public async Task AcquireAsync_ShouldDeny_WhenLimitExceeded()
    {
        // Arrange - exhaust the limit
        for (int i = 0; i < 5; i++)
        {
            await _rateLimiter.AcquireAsync("test");
        }

        // Act
        var result = await _rateLimiter.AcquireAsync("test");

        // Assert
        result.Allowed.Should().BeFalse();
        result.RemainingRequests.Should().Be(0);
        result.RetryAfter.Should().BeGreaterThan(TimeSpan.Zero);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldExecute_WhenAllowed()
    {
        // Arrange
        var operation = new Func<Task<int>>(async () =>
        {
            await Task.CompletedTask;
            return 42;
        });

        // Act
        var result = await _rateLimiter.ExecuteAsync("test", operation);

        // Assert
        result.Should().Be(42);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldThrow_WhenRateLimited()
    {
        // Arrange - exhaust the limit
        for (int i = 0; i < 5; i++)
        {
            await _rateLimiter.AcquireAsync("test");
        }

        var operation = new Func<Task<int>>(async () =>
        {
            await Task.CompletedTask;
            return 42;
        });

        // Act & Assert
        await Assert.ThrowsAsync<RateLimitExceededException>(() =>
            _rateLimiter.ExecuteAsync("test", operation));
    }

    [Fact]
    public async Task GetStatus_ShouldReturnCorrectStatus()
    {
        // Arrange
        await _rateLimiter.AcquireAsync("test");

        // Act
        var status = _rateLimiter.GetStatus("test");

        // Assert
        status.RemainingRequests.Should().Be(4);
        status.Limit.Should().Be(5);
    }

    [Fact]
    public async Task AcquireAsync_ShouldReset_AfterWindow()
    {
        // Arrange - exhaust the limit
        for (int i = 0; i < 5; i++)
        {
            await _rateLimiter.AcquireAsync("test");
        }

        // Wait for window to expire
        await Task.Delay(1100);

        // Act
        var result = await _rateLimiter.AcquireAsync("test");

        // Assert
        result.Allowed.Should().BeTrue();
        result.RemainingRequests.Should().Be(4);
    }

    [Fact]
    public async Task Reset_ShouldResetRateLimit()
    {
        // Arrange
        await _rateLimiter.AcquireAsync("test");
        await _rateLimiter.AcquireAsync("test");

        // Act
        _rateLimiter.Reset("test");
        var status = _rateLimiter.GetStatus("test");

        // Assert
        status.RemainingRequests.Should().Be(5); // Should be reset
    }

    [Fact]
    public async Task ResetAll_ShouldResetAllLimits()
    {
        // Arrange
        await _rateLimiter.AcquireAsync("test1");
        await _rateLimiter.AcquireAsync("test2");

        // Act
        _rateLimiter.ResetAll();
        var status1 = _rateLimiter.GetStatus("test1");
        var status2 = _rateLimiter.GetStatus("test2");

        // Assert
        status1.RemainingRequests.Should().Be(5);
        status2.RemainingRequests.Should().Be(5);
    }
}

