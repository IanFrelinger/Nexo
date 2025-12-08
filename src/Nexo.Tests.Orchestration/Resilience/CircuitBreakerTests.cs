using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Nexo.Orchestration.Resilience;
using Xunit;

namespace Nexo.Tests.Orchestration.Resilience;

public class CircuitBreakerTests
{
    private readonly Mock<ILogger<CircuitBreaker>> _loggerMock;
    private readonly CircuitBreaker _circuitBreaker;

    public CircuitBreakerTests()
    {
        _loggerMock = new Mock<ILogger<CircuitBreaker>>();
        _circuitBreaker = new CircuitBreaker("Test", failureThreshold: 3, timeout: TimeSpan.FromSeconds(1), _loggerMock.Object);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldSucceed_WhenOperationSucceeds()
    {
        // Arrange
        var operation = new Func<Task<int>>(async () =>
        {
            await Task.Delay(10);
            return 42;
        });

        // Act
        var result = await _circuitBreaker.ExecuteAsync("test", operation);

        // Assert
        result.Should().Be(42);
        _circuitBreaker.GetState("test").Should().Be(CircuitStateType.Closed);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldOpenCircuit_AfterFailureThreshold()
    {
        // Arrange
        var callCount = 0;
        var operation = new Func<Task<int>>(async () =>
        {
            callCount++;
            await Task.Delay(10);
            throw new InvalidOperationException("Test error");
        });

        // Act & Assert
        for (int i = 0; i < 3; i++)
        {
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _circuitBreaker.ExecuteAsync("test", operation));
        }

        // Circuit should be open now
        _circuitBreaker.GetState("test").Should().Be(CircuitStateType.Open);

        // Next call should use fallback or throw CircuitBreakerOpenException
        var fallbackCalled = false;
        var fallback = new Func<Exception, int>(ex =>
        {
            fallbackCalled = true;
            return -1;
        });

        var result = await _circuitBreaker.ExecuteAsync("test", operation, fallback);
        result.Should().Be(-1);
        fallbackCalled.Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteAsync_ShouldTransitionToHalfOpen_AfterTimeout()
    {
        // Arrange
        var operation = new Func<Task<int>>(async () =>
        {
            await Task.CompletedTask;
            throw new InvalidOperationException("Test error");
        });

        // Open the circuit
        for (int i = 0; i < 3; i++)
        {
            try
            {
                await _circuitBreaker.ExecuteAsync("test", operation);
            }
            catch { }
        }

        _circuitBreaker.GetState("test").Should().Be(CircuitStateType.Open);

        // Wait for timeout
        await Task.Delay(1100);

        // Act - should transition to half-open
        var successOperation = new Func<Task<int>>(async () =>
        {
            await Task.CompletedTask;
            return 42;
        });
        var result = await _circuitBreaker.ExecuteAsync("test", successOperation);

        // Assert
        result.Should().Be(42);
        _circuitBreaker.GetState("test").Should().Be(CircuitStateType.HalfOpen);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldCloseCircuit_AfterSuccessfulHalfOpen()
    {
        // Arrange
        var operation = new Func<Task<int>>(async () =>
        {
            await Task.CompletedTask;
            throw new InvalidOperationException("Test error");
        });

        // Open the circuit
        for (int i = 0; i < 3; i++)
        {
            try
            {
                await _circuitBreaker.ExecuteAsync("test", operation);
            }
            catch { }
        }

        await Task.Delay(1100);

        // Act - succeed twice in half-open state
        var successOperation = new Func<Task<int>>(async () =>
        {
            await Task.CompletedTask;
            return 42;
        });
        await _circuitBreaker.ExecuteAsync("test", successOperation);
        await _circuitBreaker.ExecuteAsync("test", successOperation);

        // Assert
        _circuitBreaker.GetState("test").Should().Be(CircuitStateType.Closed);
    }

    [Fact]
    public void Open_ShouldManuallyOpenCircuit()
    {
        // Act
        _circuitBreaker.Open("test");

        // Assert
        _circuitBreaker.GetState("test").Should().Be(CircuitStateType.Open);
    }

    [Fact]
    public void Close_ShouldManuallyCloseCircuit()
    {
        // Arrange
        _circuitBreaker.Open("test");

        // Act
        _circuitBreaker.Close("test");

        // Assert
        _circuitBreaker.GetState("test").Should().Be(CircuitStateType.Closed);
    }

    [Fact]
    public void Reset_ShouldClearAllCircuits()
    {
        // Arrange
        _circuitBreaker.Open("test1");
        _circuitBreaker.Open("test2");

        // Act
        _circuitBreaker.Reset();

        // Assert - circuits should be reset (new state will be Closed)
        _circuitBreaker.GetState("test1").Should().Be(CircuitStateType.Closed);
    }
}

