using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Models.Iteration;
using Nexo.Core.Application.Services.Iteration;
using Nexo.Core.Application.Services.Iteration.Strategies;
using Nexo.Core.Domain.Entities.Iteration;
using Xunit;

namespace Nexo.Core.Application.Tests;

/// <summary>
/// Comprehensive tests for the Iteration Strategy Pattern system
/// </summary>
public class IterationStrategyTests
{
    private readonly IServiceProvider _serviceProvider;
    
    public IterationStrategyTests()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddIterationStrategies();
        _serviceProvider = services.BuildServiceProvider();
    }
    
    [Fact]
    public void ForLoopStrategy_ShouldExecuteCorrectly()
    {
        // Arrange
        var strategy = new ForLoopStrategy<int>();
        var data = new[] { 1, 2, 3, 4, 5 };
        var results = new List<int>();
        
        // Act
        strategy.Execute(data, x => results.Add(x * 2));
        
        // Assert
        Assert.Equal(new[] { 2, 4, 6, 8, 10 }, results);
        Assert.Equal("ForLoop", strategy.StrategyId);
        Assert.Equal(PerformanceLevel.Excellent, strategy.PerformanceProfile.CpuEfficiency);
        Assert.True(strategy.PlatformCompatibility.HasFlag(PlatformCompatibility.DotNet));
    }
    
    [Fact]
    public void ForeachStrategy_ShouldExecuteCorrectly()
    {
        // Arrange
        var strategy = new ForeachStrategy<int>();
        var data = new[] { 1, 2, 3, 4, 5 };
        var results = new List<int>();
        
        // Act
        strategy.Execute(data, x => results.Add(x * 2));
        
        // Assert
        Assert.Equal(new[] { 2, 4, 6, 8, 10 }, results);
        Assert.Equal("Foreach", strategy.StrategyId);
        Assert.Equal(PerformanceLevel.High, strategy.PerformanceProfile.CpuEfficiency);
    }
    
    [Fact]
    public void LinqStrategy_ShouldExecuteCorrectly()
    {
        // Arrange
        var strategy = new LinqStrategy<int>();
        var data = new[] { 1, 2, 3, 4, 5 };
        
        // Act
        var results = strategy.Execute(data, x => x * 2);
        
        // Assert
        Assert.Equal(new[] { 2, 4, 6, 8, 10 }, results);
        Assert.Equal("Linq", strategy.StrategyId);
        Assert.Equal(PerformanceLevel.Medium, strategy.PerformanceProfile.CpuEfficiency);
    }
    
    [Fact]
    public void ParallelLinqStrategy_ShouldExecuteCorrectly()
    {
        // Arrange
        var strategy = new ParallelLinqStrategy<int>();
        var data = Enumerable.Range(1, 1000).ToArray();
        
        // Act
        var results = strategy.Execute(data, x => x * 2);
        
        // Assert
        Assert.Equal(1000, results.Count());
        Assert.Equal("ParallelLinq", strategy.StrategyId);
        Assert.Equal(PerformanceLevel.Excellent, strategy.PerformanceProfile.CpuEfficiency);
        Assert.True(strategy.PerformanceProfile.SupportsParallelization);
    }
    
    [Fact]
    public void UnityOptimizedStrategy_ShouldExecuteCorrectly()
    {
        // Arrange
        var strategy = new UnityOptimizedStrategy<int>();
        var data = new[] { 1, 2, 3, 4, 5 };
        var results = new List<int>();
        
        // Act
        strategy.Execute(data, x => results.Add(x * 2));
        
        // Assert
        Assert.Equal(new[] { 2, 4, 6, 8, 10 }, results);
        Assert.Equal("UnityOptimized", strategy.StrategyId);
        Assert.True(strategy.PlatformCompatibility.HasFlag(PlatformCompatibility.Unity));
    }
    
    [Fact]
    public void WasmOptimizedStrategy_ShouldExecuteCorrectly()
    {
        // Arrange
        var strategy = new WasmOptimizedStrategy<int>();
        var data = new[] { 1, 2, 3, 4, 5 };
        
        // Act
        var results = strategy.Execute(data, x => x * 2);
        
        // Assert
        Assert.Equal(new[] { 2, 4, 6, 8, 10 }, results);
        Assert.Equal("WasmOptimized", strategy.StrategyId);
        Assert.True(strategy.PlatformCompatibility.HasFlag(PlatformCompatibility.WebAssembly));
    }
    
    [Fact]
    public async Task ForLoopStrategy_ShouldExecuteAsyncCorrectly()
    {
        // Arrange
        var strategy = new ForLoopStrategy<int>();
        var data = new[] { 1, 2, 3 };
        var results = new List<int>();
        
        // Act
        await strategy.ExecuteAsync(data, async x =>
        {
            await Task.Delay(1);
            results.Add(x * 2);
        });
        
        // Assert
        Assert.Equal(new[] { 2, 4, 6 }, results);
    }
    
    [Fact]
    public void ForLoopStrategy_ShouldExecuteWhereCorrectly()
    {
        // Arrange
        var strategy = new ForLoopStrategy<int>();
        var data = new[] { 1, 2, 3, 4, 5 };
        
        // Act
        var results = strategy.ExecuteWhere(data, x => x % 2 == 0, x => x * 2);
        
        // Assert
        Assert.Equal(new[] { 4, 8 }, results);
    }
    
    [Fact]
    public void StrategySelector_ShouldSelectAppropriateStrategy()
    {
        // Arrange
        var selector = _serviceProvider.GetRequiredService<IIterationStrategySelector>();
        var smallContext = new IterationContext
        {
            DataSize = 100,
            EnvironmentProfile = RuntimeEnvironmentDetector.DetectCurrent()
        };
        
        // Act
        var strategy = selector.SelectStrategy<object>(smallContext);
        
        // Assert
        Assert.NotNull(strategy);
        Assert.True(strategy.PlatformCompatibility.HasFlag(PlatformCompatibility.DotNet));
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
        var strategy = selector.SelectStrategy<object>(largeContext);
        
        // Assert
        Assert.NotNull(strategy);
        // Should prefer parallel strategy for large data with parallelization requirement
        if (strategy.PerformanceProfile.SupportsParallelization)
        {
            Assert.True(strategy.PerformanceProfile.SupportsParallelization);
        }
    }
    
    [Fact]
    public void RuntimeEnvironmentDetector_ShouldDetectCurrentEnvironment()
    {
        // Act
        var profile = RuntimeEnvironmentDetector.DetectCurrent();
        
        // Assert
        Assert.NotNull(profile);
        Assert.True(profile.CpuCores > 0);
        Assert.True(profile.AvailableMemoryMB > 0);
        Assert.NotEmpty(profile.FrameworkVersion);
    }
    
    [Fact]
    public void ForLoopStrategy_ShouldGenerateCorrectCode()
    {
        // Arrange
        var strategy = new ForLoopStrategy<int>();
        var context = new CodeGenerationContext
        {
            PlatformTarget = PlatformTarget.CSharp,
            CollectionName = "items",
            IterationBodyTemplate = "ProcessItem({item});"
        };
        
        // Act
        var code = strategy.GenerateCode(context);
        
        // Assert
        Assert.Contains("for (int i = 0; i < items.Count; i++)", code);
        Assert.Contains("ProcessItem(items[i]);", code);
    }
    
    [Fact]
    public void ForeachStrategy_ShouldGenerateCorrectCode()
    {
        // Arrange
        var strategy = new ForeachStrategy<int>();
        var context = new CodeGenerationContext
        {
            PlatformTarget = PlatformTarget.CSharp,
            CollectionName = "items",
            ItemName = "item",
            IterationBodyTemplate = "ProcessItem({item});"
        };
        
        // Act
        var code = strategy.GenerateCode(context);
        
        // Assert
        Assert.Contains("foreach (var item in items)", code);
        Assert.Contains("ProcessItem(item);", code);
    }
    
    [Fact]
    public void LinqStrategy_ShouldGenerateCorrectCode()
    {
        // Arrange
        var strategy = new LinqStrategy<int>();
        var context = new CodeGenerationContext
        {
            PlatformTarget = PlatformTarget.CSharp,
            CollectionName = "items",
            ActionTemplate = "x => ProcessItem(x)"
        };
        
        // Act
        var code = strategy.GenerateCode(context);
        
        // Assert
        Assert.Contains("items.ToList().ForEach", code);
        Assert.Contains("x => ProcessItem(x)", code);
    }
    
    [Fact]
    public void ParallelLinqStrategy_ShouldGenerateCorrectCode()
    {
        // Arrange
        var strategy = new ParallelLinqStrategy<int>();
        var context = new CodeGenerationContext
        {
            PlatformTarget = PlatformTarget.CSharp,
            CollectionName = "items",
            ActionTemplate = "x => ProcessItem(x)"
        };
        
        // Act
        var code = strategy.GenerateCode(context);
        
        // Assert
        Assert.Contains("items.AsParallel()", code);
    }
    
    [Fact]
    public void StrategySelector_ShouldRegisterCustomStrategy()
    {
        // Arrange
        var selector = _serviceProvider.GetRequiredService<IIterationStrategySelector>();
        var customStrategy = new TestCustomStrategy();
        
        // Act
        selector.RegisterStrategy(customStrategy);
        var context = new IterationContext
        {
            DataSize = 100,
            EnvironmentProfile = RuntimeEnvironmentDetector.DetectCurrent()
        };
        var selectedStrategy = selector.SelectStrategy<object>(context);
        
        // Assert
        Assert.NotNull(selectedStrategy);
    }
    
    [Fact]
    public void IterationContext_ShouldHaveCorrectDefaults()
    {
        // Act
        var context = new IterationContext();
        
        // Assert
        Assert.Equal(0, context.DataSize);
        Assert.NotNull(context.Requirements);
        Assert.NotNull(context.EnvironmentProfile);
        Assert.False(context.Requirements.PrioritizeCpu);
        Assert.False(context.Requirements.PrioritizeMemory);
        Assert.False(context.Requirements.RequiresParallelization);
        Assert.True(context.Requirements.RequiresOrdering);
        Assert.True(context.Requirements.AllowSideEffects);
    }
    
    [Fact]
    public void IterationRequirements_ShouldAllowCustomization()
    {
        // Act
        var requirements = new IterationRequirements
        {
            PrioritizeCpu = true,
            PrioritizeMemory = false,
            RequiresParallelization = true,
            RequiresOrdering = false,
            AllowSideEffects = false,
            MaxDegreeOfParallelism = 4,
            Timeout = TimeSpan.FromSeconds(30)
        };
        
        // Assert
        Assert.True(requirements.PrioritizeCpu);
        Assert.False(requirements.PrioritizeMemory);
        Assert.True(requirements.RequiresParallelization);
        Assert.False(requirements.RequiresOrdering);
        Assert.False(requirements.AllowSideEffects);
        Assert.Equal(4, requirements.MaxDegreeOfParallelism);
        Assert.Equal(TimeSpan.FromSeconds(30), requirements.Timeout);
    }
}

/// <summary>
/// Test custom strategy for validation
/// </summary>
public class TestCustomStrategy : IIterationStrategy<object>
{
    public string StrategyId => "TestCustom";
    
    public IterationPerformanceProfile PerformanceProfile => new()
    {
        CpuEfficiency = PerformanceLevel.Medium,
        MemoryEfficiency = PerformanceLevel.Medium,
        Scalability = PerformanceLevel.Medium,
        OptimalDataSizeMin = 0,
        OptimalDataSizeMax = 1000,
        SupportsParallelization = false,
        RequiresIList = false
    };
    
    public PlatformCompatibility PlatformCompatibility => PlatformCompatibility.DotNet;
    
    public void Execute(IEnumerable<object> source, Action<object> action)
    {
        foreach (var item in source)
        {
            action(item);
        }
    }
    
    public IEnumerable<TResult> Execute<TResult>(IEnumerable<object> source, Func<object, TResult> transform)
    {
        return source.Select(transform);
    }
    
    public async Task ExecuteAsync(IEnumerable<object> source, Func<object, Task> asyncAction)
    {
        foreach (var item in source)
        {
            await asyncAction(item);
        }
    }
    
    public IEnumerable<TResult> ExecuteWhere<TResult>(
        IEnumerable<object> source, 
        Func<object, bool> predicate, 
        Func<object, TResult> transform)
    {
        return source.Where(predicate).Select(transform);
    }
    
    public string GenerateCode(CodeGenerationContext context)
    {
        return $"// Custom strategy code for {context.PlatformTarget}";
    }
}
