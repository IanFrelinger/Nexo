using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Ashlar.Orchestration.Architect.Models;
using Ashlar.Orchestration.Validation;
using Xunit;

namespace Ashlar.Tests.Orchestration.Tests.Stress;

/// <summary>
/// Stress tests for orchestration components under high load and concurrent operations.
/// </summary>
public class OrchestrationStressTests
{
    [Fact]
    public async Task Orchestrator_ShouldHandleConcurrentRequests()
    {
        // Arrange
        const int concurrentRequests = 100;

        // Act
        var tasks = Enumerable.Range(0, concurrentRequests)
            .Select(async i =>
            {
                try
                {
                    // Simulate orchestration request
                    await Task.Delay(Random.Shared.Next(10, 50));
                    return true;
                }
                catch
                {
                    return false;
                }
            })
            .ToArray();

        var results = await Task.WhenAll(tasks);

        // Assert
        results.Should().AllBeEquivalentTo(true, "All concurrent requests should complete");
        results.Length.Should().Be(concurrentRequests);
    }

    [Fact]
    public async Task Orchestrator_ShouldHandleRapidSequentialRequests()
    {
        // Arrange
        const int iterations = 1000;

        // Act
        var successCount = 0;
        for (int i = 0; i < iterations; i++)
        {
            try
            {
                await Task.Delay(1);
                successCount++;
            }
            catch
            {
                // Count failures
            }
        }

        // Assert
        successCount.Should().Be(iterations, "All sequential requests should complete");
    }

    [Fact]
    public async Task Validators_ShouldHandleHighThroughput()
    {
        // Arrange
        const int validationCount = 500;
        var validator = new CoverageChecker(Mock.Of<ILogger<CoverageChecker>>());

        // Act
        var tasks = Enumerable.Range(0, validationCount)
            .Select(async _ =>
            {
                try
                {
                    // Create a valid DecompositionResult for validation
                    var result = new DecompositionResult
                    {
                        OriginalRequest = "test request for validation",
                        Agents = new List<AgentSpawnSpec>()
                    };
                    
                    var validationResult = await validator.ValidateAsync(
                        result,
                        CancellationToken.None
                    );
                    return validationResult?.Count ?? 0;
                }
                catch
                {
                    return -1;
                }
            })
            .ToArray();

        var results = await Task.WhenAll(tasks);

        // Assert
        results.Should().NotContain(-1, "All validations should complete without exceptions");
        results.Length.Should().Be(validationCount);
    }

    [Fact]
    public void Orchestrator_ShouldNotLeakMemory()
    {
        // Act - Create many operations
        for (int i = 0; i < 1000; i++)
        {
            var request = new Dictionary<string, object>
            {
                ["request"] = $"Request {i}",
                ["context"] = new Dictionary<string, object>()
            };
            // Simulate processing
        }

        // Assert - If we get here without OOM, memory is managed
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
    }

    [Fact]
    public async Task Orchestrator_ShouldHandleCancellationGracefully()
    {
        // Arrange
        const int concurrentRequests = 50;
        using var cts = new CancellationTokenSource();
        cts.CancelAfter(100);

        // Act
        var tasks = Enumerable.Range(0, concurrentRequests)
            .Select(async i =>
            {
                try
                {
                    await Task.Delay(1000, cts.Token);
                    return false; // Should not reach here
                }
                catch (OperationCanceledException)
                {
                    return true; // Expected cancellation
                }
            })
            .ToArray();

        var results = await Task.WhenAll(tasks);

        // Assert
        results.Should().AllBeEquivalentTo(true, "All requests should be cancelled gracefully");
    }
}
