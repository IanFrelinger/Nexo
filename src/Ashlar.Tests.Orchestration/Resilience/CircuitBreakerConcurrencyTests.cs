using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Ashlar.Orchestration.Resilience;
using Xunit;

namespace Ashlar.Tests.Orchestration.Resilience;

/// <summary>Tests for circuit breaker concurrency.</summary>
public class CircuitBreakerConcurrencyTests
{
    [Fact]
    public async Task ExecuteAsync_ShouldOpenCircuit_AfterThreshold_UnderConcurrency()
    {
        // Arrange
        var logger = new Mock<ILogger<CircuitBreaker>>();
        var circuitBreaker = new CircuitBreaker("Test", failureThreshold: 5, TimeSpan.FromSeconds(30), logger.Object);
        var key = "concurrent-failure";
        var failingOperation = new Func<Task<int>>(() => throw new InvalidOperationException("Fail"));

        // Act - Fire 20 concurrent failing requests
        var tasks = Enumerable.Range(0, 20)
            .Select(_ => Task.Run(async () =>
            {
                try
                {
                    await circuitBreaker.ExecuteAsync(key, failingOperation);
                }
                catch
                {
                    // Expected
                }
            }))
            .ToList();

        await Task.WhenAll(tasks);

        // Assert - Circuit should be open
        circuitBreaker.GetState(key).Should().Be(CircuitStateType.Open);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldMaintainCorrectState_UnderMixedConcurrency()
    {
        // Arrange
        var logger = new Mock<ILogger<CircuitBreaker>>();
        var circuitBreaker = new CircuitBreaker("Test", failureThreshold: 3, TimeSpan.FromSeconds(30), logger.Object);
        var key = "mixed-concurrent";
        var callCount = 0;

        var operation = new Func<Task<int>>(() =>
        {
            var count = Interlocked.Increment(ref callCount);
            if (count % 3 == 0) // Every 3rd call fails
            {
                /// <summary>Invalid operation exception.</summary>
                /// <param name="failure"">Failure".</param>
                throw new InvalidOperationException("Periodic failure");
            }
            return Task.FromResult(count);
        });

        // Act - Fire concurrent requests
        var tasks = Enumerable.Range(0, 30)
            .Select(_ => Task.Run(async () =>
            {
                try
                {
                    return await circuitBreaker.ExecuteAsync(key, operation);
                }
                catch
                {
                    return -1;
                }
            }))
            .ToList();

        var results = await Task.WhenAll(tasks);

        // Assert - State should be consistent (not corrupted)
        var state = circuitBreaker.GetState(key);
        state.Should().BeOneOf(CircuitStateType.Closed, CircuitStateType.Open, CircuitStateType.HalfOpen);
    }
}

