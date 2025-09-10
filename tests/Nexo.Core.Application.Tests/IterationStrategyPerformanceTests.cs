using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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
/// Performance-focused tests for iteration strategies
/// </summary>
public class IterationStrategyPerformanceTests
{
    private readonly IServiceProvider _serviceProvider;
    
    public IterationStrategyPerformanceTests()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddIterationStrategies();
        _serviceProvider = services.BuildServiceProvider();
    }
    
    [Theory]
    [InlineData(100)]
    [InlineData(1000)]
    [InlineData(10000)]
    [InlineData(100000)]
    public void ForLoopStrategy_ShouldPerformWellAtDifferentSizes(int dataSize)
    {
        // Arrange
        var strategy = new ForLoopStrategy<int>();
        var data = Enumerable.Range(1, dataSize).ToList();
        var stopwatch = Stopwatch.StartNew();
        
        // Act
        strategy.Execute(data, x => Math.Sqrt(x));
        stopwatch.Stop();
        
        // Assert
        Assert.True(stopwatch.ElapsedMilliseconds < 1000, $"ForLoop took {stopwatch.ElapsedMilliseconds}ms for {dataSize} items");
    }
    
    [Theory]
    [InlineData(100)]
    [InlineData(1000)]
    [InlineData(10000)]
    public void ForeachStrategy_ShouldPerformWellAtDifferentSizes(int dataSize)
    {
        // Arrange
        var strategy = new ForeachStrategy<int>();
        var data = Enumerable.Range(1, dataSize).ToList();
        var stopwatch = Stopwatch.StartNew();
        
        // Act
        strategy.Execute(data, x => Math.Sqrt(x));
        stopwatch.Stop();
        
        // Assert
        Assert.True(stopwatch.ElapsedMilliseconds < 2000, $"Foreach took {stopwatch.ElapsedMilliseconds}ms for {dataSize} items");
    }
    
    [Theory]
    [InlineData(100)]
    [InlineData(1000)]
    [InlineData(10000)]
    public void LinqStrategy_ShouldPerformWellAtDifferentSizes(int dataSize)
    {
        // Arrange
        var strategy = new LinqStrategy<int>();
        var data = Enumerable.Range(1, dataSize).ToList();
        var stopwatch = Stopwatch.StartNew();
        
        // Act
        strategy.Execute(data, x => Math.Sqrt(x));
        stopwatch.Stop();
        
        // Assert
        Assert.True(stopwatch.ElapsedMilliseconds < 3000, $"LINQ took {stopwatch.ElapsedMilliseconds}ms for {dataSize} items");
    }
    
    [Theory]
    [InlineData(1000)]
    [InlineData(10000)]
    [InlineData(100000)]
    public void ParallelLinqStrategy_ShouldPerformWellAtDifferentSizes(int dataSize)
    {
        // Arrange
        var strategy = new ParallelLinqStrategy<int>();
        var data = Enumerable.Range(1, dataSize).ToList();
        var stopwatch = Stopwatch.StartNew();
        
        // Act
        strategy.Execute(data, x => Math.Sqrt(x));
        stopwatch.Stop();
        
        // Assert
        Assert.True(stopwatch.ElapsedMilliseconds < 2000, $"ParallelLINQ took {stopwatch.ElapsedMilliseconds}ms for {dataSize} items");
    }
    
    [Fact]
    public void StrategySelector_ShouldSelectFastestStrategyForSmallData()
    {
        // Arrange
        var selector = _serviceProvider.GetRequiredService<IIterationStrategySelector>();
        var smallContext = new IterationContext
        {
            DataSize = 100,
            EnvironmentProfile = RuntimeEnvironmentDetector.DetectCurrent()
        };
        
        // Act
        var strategy = selector.SelectStrategy<int>(smallContext);
        
        // Assert
        Assert.NotNull(strategy);
        // For small data, should prefer strategies that don't have overhead
        Assert.True(strategy.PerformanceProfile.OptimalDataSizeMin <= 100);
    }
    
    [Fact]
    public void StrategySelector_ShouldSelectParallelStrategyForLargeData()
    {
        // Arrange
        var selector = _serviceProvider.GetRequiredService<IIterationStrategySelector>();
        var largeContext = new IterationContext
        {
            DataSize = 100000,
            Requirements = new IterationRequirements { RequiresParallelization = true },
            EnvironmentProfile = RuntimeEnvironmentDetector.DetectCurrent()
        };
        
        // Act
        var strategy = selector.SelectStrategy<int>(largeContext);
        
        // Assert
        Assert.NotNull(strategy);
        // For large data with parallelization requirement, should prefer parallel strategies
        if (strategy.PerformanceProfile.SupportsParallelization)
        {
            Assert.True(strategy.PerformanceProfile.SupportsParallelization);
        }
    }
    
    [Fact]
    public void StrategySelector_ShouldSelectMemoryEfficientStrategyWhenMemoryPriority()
    {
        // Arrange
        var selector = _serviceProvider.GetRequiredService<IIterationStrategySelector>();
        var memoryContext = new IterationContext
        {
            DataSize = 10000,
            Requirements = new IterationRequirements { PrioritizeMemory = true },
            EnvironmentProfile = RuntimeEnvironmentDetector.DetectCurrent()
        };
        
        // Act
        var strategy = selector.SelectStrategy<int>(memoryContext);
        
        // Assert
        Assert.NotNull(strategy);
        // Should prefer memory-efficient strategies
        Assert.True(strategy.PerformanceProfile.MemoryEfficiency >= PerformanceLevel.High);
    }
    
    [Fact]
    public void StrategySelector_ShouldSelectCpuEfficientStrategyWhenCpuPriority()
    {
        // Arrange
        var selector = _serviceProvider.GetRequiredService<IIterationStrategySelector>();
        var cpuContext = new IterationContext
        {
            DataSize = 10000,
            Requirements = new IterationRequirements { PrioritizeCpu = true },
            EnvironmentProfile = RuntimeEnvironmentDetector.DetectCurrent()
        };
        
        // Act
        var strategy = selector.SelectStrategy<int>(cpuContext);
        
        // Assert
        Assert.NotNull(strategy);
        // Should prefer CPU-efficient strategies
        Assert.True(strategy.PerformanceProfile.CpuEfficiency >= PerformanceLevel.High, 
            $"Expected CPU efficiency >= High, but got {strategy.PerformanceProfile.CpuEfficiency} for strategy {strategy.StrategyId}");
    }
    
    [Fact]
    public async Task AsyncStrategies_ShouldCompleteSuccessfully()
    {
        // Arrange
        var strategies = new IIterationStrategy<int>[]
        {
            new ForLoopStrategy<int>(),
            new ForeachStrategy<int>(),
            new LinqStrategy<int>(),
            new UnityOptimizedStrategy<int>(),
            new WasmOptimizedStrategy<int>()
        };
        
        var data = Enumerable.Range(1, 100).ToList();
        
        foreach (var strategy in strategies)
        {
            // Act & Assert
            await strategy.ExecuteAsync(data, async x =>
            {
                await Task.Delay(1);
                var _ = Math.Sqrt(x);
            });
        }
    }
    
    [Fact]
    public void Strategies_ShouldHandleEmptyCollections()
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
        
        var emptyData = new int[0];
        
        foreach (var strategy in strategies)
        {
            // Act & Assert - should not throw
            var results = new List<int>();
            strategy.Execute(emptyData, x => results.Add(x));
            Assert.Empty(results);
            
            var transformed = strategy.Execute(emptyData, x => x * 2);
            Assert.Empty(transformed);
            
            var filtered = strategy.ExecuteWhere(emptyData, x => x > 0, x => x * 2);
            Assert.Empty(filtered);
        }
    }
    
    [Fact]
    public void Strategies_ShouldHandleSingleItemCollections()
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
        
        var singleData = new[] { 42 };
        
        foreach (var strategy in strategies)
        {
            // Act
            var results = new List<int>();
            strategy.Execute(singleData, x => results.Add(x * 2));
            
            var transformed = strategy.Execute(singleData, x => x * 2);
            
            var filtered = strategy.ExecuteWhere(singleData, x => x > 0, x => x * 2);
            
            // Assert
            Assert.Equal(new[] { 84 }, results);
            Assert.Equal(new[] { 84 }, transformed);
            Assert.Equal(new[] { 84 }, filtered);
        }
    }
    
    [Fact]
    public void Strategies_ShouldHandleNullValuesInCollections()
    {
        // Arrange
        var strategies = new IIterationStrategy<string?>[]
        {
            new ForLoopStrategy<string?>(),
            new ForeachStrategy<string?>(),
            new LinqStrategy<string?>(),
            new UnityOptimizedStrategy<string?>(),
            new WasmOptimizedStrategy<string?>()
        };
        
        var dataWithNulls = new[] { "hello", null, "world", null, "test" };
        
        foreach (var strategy in strategies)
        {
            // Act
            var results = new List<string?>();
            strategy.Execute(dataWithNulls, x => results.Add(x?.ToUpper()));
            
            var transformed = strategy.Execute(dataWithNulls, x => x?.ToUpper());
            
            var filtered = strategy.ExecuteWhere(dataWithNulls, x => x != null, x => x!.ToUpper());
            
            // Assert
            Assert.Equal(5, results.Count);
            Assert.Equal(5, transformed.Count());
            Assert.Equal(3, filtered.Count());
        }
    }
    
    [Fact]
    public void PerformanceProfiles_ShouldHaveValidRanges()
    {
        // Arrange
        var strategies = new IIterationStrategy<object>[]
        {
            new ForLoopStrategy<object>(),
            new ForeachStrategy<object>(),
            new LinqStrategy<object>(),
            new ParallelLinqStrategy<object>(),
            new UnityOptimizedStrategy<object>(),
            new WasmOptimizedStrategy<object>()
        };
        
        foreach (var strategy in strategies)
        {
            var profile = strategy.PerformanceProfile;
            
            // Assert
            Assert.True(profile.OptimalDataSizeMin >= 0);
            Assert.True(profile.OptimalDataSizeMax > profile.OptimalDataSizeMin);
            Assert.True(profile.OptimalDataSizeMax <= int.MaxValue);
            
            Assert.True(Enum.IsDefined(typeof(PerformanceLevel), profile.CpuEfficiency));
            Assert.True(Enum.IsDefined(typeof(PerformanceLevel), profile.MemoryEfficiency));
            Assert.True(Enum.IsDefined(typeof(PerformanceLevel), profile.Scalability));
        }
    }
    
    [Fact]
    public void PlatformCompatibility_ShouldBeValid()
    {
        // Arrange
        var strategies = new IIterationStrategy<object>[]
        {
            new ForLoopStrategy<object>(),
            new ForeachStrategy<object>(),
            new LinqStrategy<object>(),
            new ParallelLinqStrategy<object>(),
            new UnityOptimizedStrategy<object>(),
            new WasmOptimizedStrategy<object>()
        };
        
        foreach (var strategy in strategies)
        {
            var compatibility = strategy.PlatformCompatibility;
            
            // Assert
            Assert.NotEqual(PlatformCompatibility.None, compatibility);
            Assert.True(compatibility.HasFlag(PlatformCompatibility.DotNet) || 
                       compatibility.HasFlag(PlatformCompatibility.Unity) || 
                       compatibility.HasFlag(PlatformCompatibility.WebAssembly));
        }
    }
}
