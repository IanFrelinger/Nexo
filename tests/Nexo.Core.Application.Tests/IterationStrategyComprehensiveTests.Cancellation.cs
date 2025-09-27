using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Services.Iteration;
using Nexo.Core.Application.Services.Iteration.Strategies;
using Nexo.Core.Domain.Entities.Iteration;
using Nexo.Core.Domain.Entities.Infrastructure;
using Nexo.Core.Domain.Interfaces.Infrastructure;
using Xunit;

namespace Nexo.Core.Application.Tests;

/// <summary>
/// Cancellation tests for iteration strategies.
/// </summary>
public partial class IterationStrategyComprehensiveTests
{
    [Fact]
    public async Task AllStrategies_ShouldHandleCancellation()
    {
        // Arrange
        var strategies = new IIterationStrategy<int>[]
        {
            new ForLoopStrategy<int>(),
            new ForeachStrategy<int>(),
            new LinqStrategy<int>(),
            new ParallelLinqStrategy<int>(),
            new UnityOptimizedStrategy<int>(),
            new WasmOptimizedStrategy<int>()
        };
        
        var data = Enumerable.Range(1, 1000).ToArray();
        var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();
        
        foreach (var strategy in strategies)
        {
            // Act & Assert - should handle cancellation gracefully
            await Assert.ThrowsAsync<OperationCanceledException>(() =>
                strategy.ExecuteAsync(data, async x => 
                {
                    await Task.Delay(1, cancellationTokenSource.Token);
                    return Task.CompletedTask;
                }));
        }
    }
    
    [Fact]
    public async Task AllStrategies_ShouldHandleCancellationDuringExecution()
    {
        // Arrange
        var strategies = new IIterationStrategy<int>[]
        {
            new ForLoopStrategy<int>(),
            new ForeachStrategy<int>(),
            new LinqStrategy<int>(),
            new ParallelLinqStrategy<int>(),
            new UnityOptimizedStrategy<int>(),
            new WasmOptimizedStrategy<int>()
        };
        
        var data = Enumerable.Range(1, 1000).ToArray();
        var cancellationTokenSource = new CancellationTokenSource();
        
        // Cancel after a short delay to simulate cancellation during execution
        _ = Task.Run(async () =>
        {
            await Task.Delay(10);
            cancellationTokenSource.Cancel();
        });
        
        foreach (var strategy in strategies)
        {
            // Act & Assert - should handle cancellation during execution
            await Assert.ThrowsAsync<OperationCanceledException>(() =>
                strategy.ExecuteAsync(data, async x => 
                {
                    await Task.Delay(1, cancellationTokenSource.Token);
                    return Task.CompletedTask;
                }));
        }
    }
    
    [Fact]
    public async Task AllStrategies_ShouldHandleCancellationWithLongRunningOperations()
    {
        // Arrange
        var strategies = new IIterationStrategy<int>[]
        {
            new ForLoopStrategy<int>(),
            new ForeachStrategy<int>(),
            new LinqStrategy<int>(),
            new ParallelLinqStrategy<int>(),
            new UnityOptimizedStrategy<int>(),
            new WasmOptimizedStrategy<int>()
        };
        
        var data = Enumerable.Range(1, 100).ToArray();
        var cancellationTokenSource = new CancellationTokenSource();
        
        // Cancel after a short delay
        _ = Task.Run(async () =>
        {
            await Task.Delay(5);
            cancellationTokenSource.Cancel();
        });
        
        foreach (var strategy in strategies)
        {
            // Act & Assert - should handle cancellation with long running operations
            await Assert.ThrowsAsync<OperationCanceledException>(() =>
                strategy.ExecuteAsync(data, async x => 
                {
                    await Task.Delay(10, cancellationTokenSource.Token);
                    return Task.CompletedTask;
                }));
        }
    }
    
    [Fact]
    public async Task AllStrategies_ShouldHandleCancellationWithMultipleOperations()
    {
        // Arrange
        var strategies = new IIterationStrategy<int>[]
        {
            new ForLoopStrategy<int>(),
            new ForeachStrategy<int>(),
            new LinqStrategy<int>(),
            new ParallelLinqStrategy<int>(),
            new UnityOptimizedStrategy<int>(),
            new WasmOptimizedStrategy<int>()
        };
        
        var data = Enumerable.Range(1, 100).ToArray();
        var cancellationTokenSource = new CancellationTokenSource();
        
        // Cancel after a short delay
        _ = Task.Run(async () =>
        {
            await Task.Delay(5);
            cancellationTokenSource.Cancel();
        });
        
        foreach (var strategy in strategies)
        {
            // Act & Assert - should handle cancellation with multiple operations
            var tasks = new List<Task>();
            
            for (int i = 0; i < 5; i++)
            {
                tasks.Add(strategy.ExecuteAsync(data, async x => 
                {
                    await Task.Delay(1, cancellationTokenSource.Token);
                    return Task.CompletedTask;
                }));
            }
            
            await Assert.ThrowsAsync<OperationCanceledException>(() => Task.WhenAll(tasks));
        }
    }
    
    [Fact]
    public async Task AllStrategies_ShouldHandleCancellationWithException()
    {
        // Arrange
        var strategies = new IIterationStrategy<int>[]
        {
            new ForLoopStrategy<int>(),
            new ForeachStrategy<int>(),
            new LinqStrategy<int>(),
            new ParallelLinqStrategy<int>(),
            new UnityOptimizedStrategy<int>(),
            new WasmOptimizedStrategy<int>()
        };
        
        var data = Enumerable.Range(1, 100).ToArray();
        var cancellationTokenSource = new CancellationTokenSource();
        
        // Cancel after a short delay
        _ = Task.Run(async () =>
        {
            await Task.Delay(5);
            cancellationTokenSource.Cancel();
        });
        
        foreach (var strategy in strategies)
        {
            // Act & Assert - should handle cancellation with exception
            await Assert.ThrowsAsync<OperationCanceledException>(() =>
                strategy.ExecuteAsync(data, async x => 
                {
                    if (x == 50)
                    {
                        throw new InvalidOperationException("Test exception");
                    }
                    await Task.Delay(1, cancellationTokenSource.Token);
                    return Task.CompletedTask;
                }));
        }
    }
}
