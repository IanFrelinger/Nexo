using System;
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
/// Cancellation test cases for IterationStrategyComprehensiveTests.
/// </summary>
public partial class IterationStrategyComprehensiveTests
{
    [Fact]
    public async Task AllStrategies_ExecuteAsync_WithCancellation_ShouldRespectCancellation()
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
        var cts = new CancellationTokenSource();
        cts.Cancel(); // Cancel immediately
        
        foreach (var strategy in strategies)
        {
            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(() => 
                strategy.ExecuteAsync(data, async x =>
                {
                    await Task.Delay(1);
                }, cts.Token));
        }
    }
    
    [Fact]
    public async Task AllStrategies_ExecuteAsync_WithCancellationDuringExecution_ShouldRespectCancellation()
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
        var cts = new CancellationTokenSource();
        
        // Cancel after a short delay
        cts.CancelAfter(50);
        
        foreach (var strategy in strategies)
        {
            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(() => 
                strategy.ExecuteAsync(data, async x =>
                {
                    await Task.Delay(10);
                }, cts.Token));
        }
    }
    
    [Fact]
    public async Task AllStrategies_ExecuteAsync_WithCancellationForTransform_ShouldRespectCancellation()
    {
        // Arrange
        var strategies = new IIterationStrategy<int>[]
        {
            new LinqStrategy<int>(),
            new ParallelLinqStrategy<int>(),
            new WasmOptimizedStrategy<int>()
        };
        
        var data = Enumerable.Range(1, 1000).ToArray();
        var cts = new CancellationTokenSource();
        cts.Cancel(); // Cancel immediately
        
        foreach (var strategy in strategies)
        {
            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(() => 
                strategy.ExecuteAsync(data, async x =>
                {
                    await Task.Delay(1);
                    return x * 2;
                }, cts.Token));
        }
    }
    
    [Fact]
    public async Task AllStrategies_ExecuteAsync_WithCancellationDuringExecutionForTransform_ShouldRespectCancellation()
    {
        // Arrange
        var strategies = new IIterationStrategy<int>[]
        {
            new LinqStrategy<int>(),
            new ParallelLinqStrategy<int>(),
            new WasmOptimizedStrategy<int>()
        };
        
        var data = Enumerable.Range(1, 1000).ToArray();
        var cts = new CancellationTokenSource();
        
        // Cancel after a short delay
        cts.CancelAfter(50);
        
        foreach (var strategy in strategies)
        {
            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(() => 
                strategy.ExecuteAsync(data, async x =>
                {
                    await Task.Delay(10);
                    return x * 2;
                }, cts.Token));
        }
    }
    
    [Fact]
    public async Task AllStrategies_ExecuteAsync_WithCancellationForWhere_ShouldRespectCancellation()
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
        var cts = new CancellationTokenSource();
        cts.Cancel(); // Cancel immediately
        
        foreach (var strategy in strategies)
        {
            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(() => 
                strategy.ExecuteAsync(data, async x =>
                {
                    await Task.Delay(1);
                    return x > 500;
                }, async x =>
                {
                    await Task.Delay(1);
                    return x * 2;
                }, cts.Token));
        }
    }
    
    [Fact]
    public async Task AllStrategies_ExecuteAsync_WithCancellationDuringExecutionForWhere_ShouldRespectCancellation()
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
        var cts = new CancellationTokenSource();
        
        // Cancel after a short delay
        cts.CancelAfter(50);
        
        foreach (var strategy in strategies)
        {
            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(() => 
                strategy.ExecuteAsync(data, async x =>
                {
                    await Task.Delay(10);
                    return x > 500;
                }, async x =>
                {
                    await Task.Delay(10);
                    return x * 2;
                }, cts.Token));
        }
    }
    
    [Fact]
    public async Task TestCustomStrategy_ExecuteAsync_WithCancellation_ShouldRespectCancellation()
    {
        // Arrange
        var strategy = new TestCustomStrategy<int>();
        var data = Enumerable.Range(1, 1000).ToArray();
        var cts = new CancellationTokenSource();
        cts.Cancel(); // Cancel immediately
        
        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() => 
            strategy.ExecuteAsync(data, async x =>
            {
                await Task.Delay(1);
            }, cts.Token));
    }
    
    [Fact]
    public async Task TestCustomStrategy_ExecuteAsync_WithCancellationDuringExecution_ShouldRespectCancellation()
    {
        // Arrange
        var strategy = new TestCustomStrategy<int>();
        var data = Enumerable.Range(1, 1000).ToArray();
        var cts = new CancellationTokenSource();
        
        // Cancel after a short delay
        cts.CancelAfter(50);
        
        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() => 
            strategy.ExecuteAsync(data, async x =>
            {
                await Task.Delay(10);
            }, cts.Token));
    }
    
    [Fact]
    public async Task TestCustomStrategy_ExecuteAsync_WithCancellationForTransform_ShouldRespectCancellation()
    {
        // Arrange
        var strategy = new TestCustomStrategy<int>();
        var data = Enumerable.Range(1, 1000).ToArray();
        var cts = new CancellationTokenSource();
        cts.Cancel(); // Cancel immediately
        
        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() => 
            strategy.ExecuteAsync(data, async x =>
            {
                await Task.Delay(1);
                return x * 2;
            }, cts.Token));
    }
    
    [Fact]
    public async Task TestCustomStrategy_ExecuteAsync_WithCancellationDuringExecutionForTransform_ShouldRespectCancellation()
    {
        // Arrange
        var strategy = new TestCustomStrategy<int>();
        var data = Enumerable.Range(1, 1000).ToArray();
        var cts = new CancellationTokenSource();
        
        // Cancel after a short delay
        cts.CancelAfter(50);
        
        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() => 
            strategy.ExecuteAsync(data, async x =>
            {
                await Task.Delay(10);
                return x * 2;
            }, cts.Token));
    }
    
    [Fact]
    public async Task TestCustomStrategy_ExecuteAsync_WithCancellationForWhere_ShouldRespectCancellation()
    {
        // Arrange
        var strategy = new TestCustomStrategy<int>();
        var data = Enumerable.Range(1, 1000).ToArray();
        var cts = new CancellationTokenSource();
        cts.Cancel(); // Cancel immediately
        
        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() => 
            strategy.ExecuteAsync(data, async x =>
            {
                await Task.Delay(1);
                return x > 500;
            }, async x =>
            {
                await Task.Delay(1);
                return x * 2;
            }, cts.Token));
    }
    
    [Fact]
    public async Task TestCustomStrategy_ExecuteAsync_WithCancellationDuringExecutionForWhere_ShouldRespectCancellation()
    {
        // Arrange
        var strategy = new TestCustomStrategy<int>();
        var data = Enumerable.Range(1, 1000).ToArray();
        var cts = new CancellationTokenSource();
        
        // Cancel after a short delay
        cts.CancelAfter(50);
        
        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() => 
            strategy.ExecuteAsync(data, async x =>
            {
                await Task.Delay(10);
                return x > 500;
            }, async x =>
            {
                await Task.Delay(10);
                return x * 2;
            }, cts.Token));
    }
}