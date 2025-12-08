using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Nexo.Orchestration.Resilience;
using Xunit;

namespace Nexo.Tests.Orchestration.Resilience;

public class RetryPolicyTests
{
    private readonly Mock<ILogger<RetryPolicy>> _loggerMock;

    public RetryPolicyTests()
    {
        _loggerMock = new Mock<ILogger<RetryPolicy>>();
    }

    [Fact]
    public async Task ExecuteAsync_ShouldSucceed_OnFirstAttempt()
    {
        // Arrange
        var policy = new RetryPolicy(maxAttempts: 3, logger: _loggerMock.Object);
        var callCount = 0;
        var operation = new Func<Task<int>>(async () =>
        {
            callCount++;
            await Task.CompletedTask;
            return 42;
        });

        // Act
        var result = await policy.ExecuteAsync(operation);

        // Assert
        result.Should().Be(42);
        callCount.Should().Be(1);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldRetry_OnFailure()
    {
        // Arrange
        var policy = new RetryPolicy(maxAttempts: 3, logger: _loggerMock.Object);
        var callCount = 0;
        var operation = new Func<Task<int>>(async () =>
        {
            callCount++;
            await Task.CompletedTask;
            if (callCount < 2)
            {
                throw new InvalidOperationException("Test error");
            }
            return 42;
        });

        // Act
        var result = await policy.ExecuteAsync(operation);

        // Assert
        result.Should().Be(42);
        callCount.Should().Be(2);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldThrow_AfterMaxAttempts()
    {
        // Arrange
        var policy = new RetryPolicy(maxAttempts: 3, logger: _loggerMock.Object);
        var operation = new Func<Task<int>>(async () =>
        {
            await Task.CompletedTask;
            throw new InvalidOperationException("Test error");
        });

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            policy.ExecuteAsync(operation));
    }

    [Fact]
    public async Task ExecuteAsync_ShouldRespectShouldRetry()
    {
        // Arrange
        var policy = new RetryPolicy(maxAttempts: 3, logger: _loggerMock.Object);
        var callCount = 0;
        var operation = new Func<Task<int>>(async () =>
        {
            callCount++;
            await Task.CompletedTask;
            throw new ArgumentException("Non-retryable");
        });
        var shouldRetry = new Func<Exception, bool>(ex => ex is not ArgumentException);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            policy.ExecuteAsync(operation, shouldRetry));
        callCount.Should().Be(1); // Should not retry
    }

    [Fact]
    public async Task ExecuteAsync_ShouldUseExponentialBackoff()
    {
        // Arrange
        var policy = new RetryPolicy(
            RetryStrategy.ExponentialBackoff,
            maxAttempts: 3,
            initialDelay: TimeSpan.FromMilliseconds(100),
            logger: _loggerMock.Object);
        var delays = new List<DateTimeOffset>();
        var operation = new Func<Task<int>>(async () =>
        {
            delays.Add(DateTimeOffset.UtcNow);
            await Task.CompletedTask;
            throw new InvalidOperationException("Test error");
        });

        // Act
        var startTime = DateTimeOffset.UtcNow;
        try
        {
            await policy.ExecuteAsync(operation);
        }
        catch { }

        // Assert
        delays.Count.Should().Be(3);
        if (delays.Count >= 2)
        {
            var delay1 = (delays[1] - delays[0]).TotalMilliseconds;
            delay1.Should().BeGreaterThan(90); // Should be ~100ms
        }
    }

    [Fact]
    public async Task ExecuteAsync_Void_ShouldWork()
    {
        // Arrange
        var policy = new RetryPolicy(logger: _loggerMock.Object);
        var callCount = 0;
        var operation = new Func<Task>(async () =>
        {
            callCount++;
            await Task.CompletedTask;
        });

        // Act
        await policy.ExecuteAsync(operation);

        // Assert
        callCount.Should().Be(1);
    }
}

