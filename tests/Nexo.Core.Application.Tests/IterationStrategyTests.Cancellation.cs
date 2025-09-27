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
/// Cancellation test cases for IterationStrategyTests.
/// </summary>
public partial class IterationStrategyTests
{
    [Fact]
    public async Task ForLoopStrategy_ExecuteAsync_WithCancellation_ShouldRespectCancellation()
    {
        // Arrange
        var strategy = new ForLoopStrategy<int>();
        var data = Enumerable.Range(1, 1000).ToArray();
        var results = new List<int>();
        var cts = new CancellationTokenSource();
        cts.Cancel(); // Cancel immediately
        
        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() => 
            strategy.ExecuteAsync(data, async x =>
            {
                await Task.Delay(1);
                results.Add(x * 2);
            }, cts.Token));
    }
    
    [Fact]
    public async Task ForLoopStrategy_ExecuteAsync_WithCancellationDuringExecution_ShouldRespectCancellation()
    {
        // Arrange
        var strategy = new ForLoopStrategy<int>();
        var data = Enumerable.Range(1, 1000).ToArray();
        var results = new List<int>();
        var cts = new CancellationTokenSource();
        
        // Cancel after a short delay
        cts.CancelAfter(50);
        
        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() => 
            strategy.ExecuteAsync(data, async x =>
            {
                await Task.Delay(10);
                results.Add(x * 2);
            }, cts.Token));
    }
    
    [Fact]
    public async Task ForeachStrategy_ExecuteAsync_WithCancellation_ShouldRespectCancellation()
    {
        // Arrange
        var strategy = new ForeachStrategy<int>();
        var data = Enumerable.Range(1, 1000).ToArray();
        var results = new List<int>();
        var cts = new CancellationTokenSource();
        cts.Cancel(); // Cancel immediately
        
        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() => 
            strategy.ExecuteAsync(data, async x =>
            {
                await Task.Delay(1);
                results.Add(x * 2);
            }, cts.Token));
    }
    
    [Fact]
    public async Task ForeachStrategy_ExecuteAsync_WithCancellationDuringExecution_ShouldRespectCancellation()
    {
        // Arrange
        var strategy = new ForeachStrategy<int>();
        var data = Enumerable.Range(1, 1000).ToArray();
        var results = new List<int>();
        var cts = new CancellationTokenSource();
        
        // Cancel after a short delay
        cts.CancelAfter(50);
        
        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() => 
            strategy.ExecuteAsync(data, async x =>
            {
                await Task.Delay(10);
                results.Add(x * 2);
            }, cts.Token));
    }
    
    [Fact]
    public async Task LinqStrategy_ExecuteAsync_WithCancellation_ShouldRespectCancellation()
    {
        // Arrange
        var strategy = new LinqStrategy<int>();
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
    public async Task LinqStrategy_ExecuteAsync_WithCancellationDuringExecution_ShouldRespectCancellation()
    {
        // Arrange
        var strategy = new LinqStrategy<int>();
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
    public async Task ParallelLinqStrategy_ExecuteAsync_WithCancellation_ShouldRespectCancellation()
    {
        // Arrange
        var strategy = new ParallelLinqStrategy<int>();
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
    public async Task ParallelLinqStrategy_ExecuteAsync_WithCancellationDuringExecution_ShouldRespectCancellation()
    {
        // Arrange
        var strategy = new ParallelLinqStrategy<int>();
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
    public async Task UnityOptimizedStrategy_ExecuteAsync_WithCancellation_ShouldRespectCancellation()
    {
        // Arrange
        var strategy = new UnityOptimizedStrategy<int>();
        var data = Enumerable.Range(1, 1000).ToArray();
        var results = new List<int>();
        var cts = new CancellationTokenSource();
        cts.Cancel(); // Cancel immediately
        
        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() => 
            strategy.ExecuteAsync(data, async x =>
            {
                await Task.Delay(1);
                results.Add(x * 2);
            }, cts.Token));
    }
    
    [Fact]
    public async Task UnityOptimizedStrategy_ExecuteAsync_WithCancellationDuringExecution_ShouldRespectCancellation()
    {
        // Arrange
        var strategy = new UnityOptimizedStrategy<int>();
        var data = Enumerable.Range(1, 1000).ToArray();
        var results = new List<int>();
        var cts = new CancellationTokenSource();
        
        // Cancel after a short delay
        cts.CancelAfter(50);
        
        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() => 
            strategy.ExecuteAsync(data, async x =>
            {
                await Task.Delay(10);
                results.Add(x * 2);
            }, cts.Token));
    }
    
    [Fact]
    public async Task WasmOptimizedStrategy_ExecuteAsync_WithCancellation_ShouldRespectCancellation()
    {
        // Arrange
        var strategy = new WasmOptimizedStrategy<int>();
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
    public async Task WasmOptimizedStrategy_ExecuteAsync_WithCancellationDuringExecution_ShouldRespectCancellation()
    {
        // Arrange
        var strategy = new WasmOptimizedStrategy<int>();
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
    public async Task TestCustomStrategy_ExecuteAsync_WithCancellation_ShouldRespectCancellation()
    {
        // Arrange
        var strategy = new TestCustomStrategy();
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
        var strategy = new TestCustomStrategy();
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
}
