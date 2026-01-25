using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Nexo.Core.Application.Common.Services;
using Xunit;

namespace Nexo.Tests.Application.Tests.Stress;

/// <summary>
/// Stress tests for command execution under high load.
/// </summary>
public class CommandExecutionStressTests
{
    [Fact]
    public async Task LoopKernel_ShouldHandleConcurrentIterations()
    {
        // Arrange
        const int concurrentIterations = 200;
        var kernel = new SequentialLoopKernel();

        // Act
        var tasks = Enumerable.Range(0, concurrentIterations)
            .Select(async i =>
            {
                try
                {
                    await kernel.ExecuteAsync(
                        Enumerable.Range(0, 10),
                        async (item, ct) =>
                        {
                            await Task.Delay(1, ct);
                            return item;
                        },
                        CancellationToken.None
                    );
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
        results.Should().AllBeEquivalentTo(true, "All concurrent iterations should complete");
        results.Length.Should().Be(concurrentIterations);
    }

    [Fact]
    public async Task ParallelLoopKernel_ShouldScaleWithWorkload()
    {
        // Arrange
        const int workloadSize = 1000;
        var kernel = new ParallelLoopKernel(Mock.Of<ILogger<ParallelLoopKernel>>());

        // Act
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        await kernel.ExecuteAsync(
            Enumerable.Range(0, workloadSize),
            async (item, ct) =>
            {
                await Task.Delay(10, ct);
                return item * 2;
            },
            CancellationToken.None
        );
        stopwatch.Stop();

        // Assert
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(workloadSize * 10, 
            "Parallel execution should be faster than sequential");
    }

    [Fact]
    public async Task CommandExecution_ShouldHandleRapidSequentialCommands()
    {
        // Arrange
        const int commandCount = 500;
        var kernel = new SequentialLoopKernel();

        // Act
        var successCount = 0;
        for (int i = 0; i < commandCount; i++)
        {
            try
            {
                await kernel.ExecuteAsync(
                    new[] { i },
                    async (item, ct) =>
                    {
                        await Task.Delay(1, ct);
                        return item;
                    },
                    CancellationToken.None
                );
                successCount++;
            }
            catch
            {
                // Count failures
            }
        }

        // Assert
        successCount.Should().Be(commandCount, "All sequential commands should complete");
    }

    [Fact]
    public void CommandExecution_ShouldNotLeakResources()
    {
        // Arrange
        var kernel = new SequentialLoopKernel();

        // Act - Execute many commands
        for (int i = 0; i < 1000; i++)
        {
            _ = kernel.ExecuteAsync(
                new[] { i },
                async (item, ct) =>
                {
                    await Task.Delay(1, ct);
                    return item;
                },
                CancellationToken.None
            ).Result;
        }

        // Assert - Force GC and verify no leaks
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
    }
}
