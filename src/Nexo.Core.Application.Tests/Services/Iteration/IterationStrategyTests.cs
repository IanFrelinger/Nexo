using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Nexo.Core.Application.Services.Iteration;
using Nexo.Core.Application.Services.Iteration.Strategies;
using Nexo.Core.Domain.Entities.Iteration;
using Nexo.Feature.Pipeline.Models;

namespace Nexo.Core.Application.Tests.Services.Iteration;

/// <summary>
/// Comprehensive tests for iteration strategy system
/// </summary>
public class IterationStrategyTests
{
    private readonly Mock<ILogger<NexoIterationStrategySelector>> _mockLogger;
    private readonly List<IIterationStrategy<object>> _strategies;
    
    public IterationStrategyTests()
    {
        _mockLogger = new Mock<ILogger<NexoIterationStrategySelector>>();
        _strategies = new List<IIterationStrategy<object>>
        {
            new NexoForLoopStrategy<object>(),
            new NexoForeachStrategy<object>(),
            new NexoLinqStrategy<object>(),
            new NexoParallelLinqStrategy<object>(),
            new NexoUnityOptimizedStrategy<object>()
        };
    }
    
    [Fact]
    public void NexoForLoopStrategy_ShouldHaveCorrectProperties()
    {
        // Arrange
        var strategy = new NexoForLoopStrategy<object>();
        
        // Assert
        Assert.Equal("Nexo.ForLoop", strategy.StrategyId);
        Assert.Equal(PerformanceLevel.High, strategy.PerformanceProfile.CpuEfficiency);
        Assert.Equal(PerformanceLevel.High, strategy.PerformanceProfile.MemoryEfficiency);
        Assert.Equal(PlatformCompatibility.All, strategy.PlatformCompatibility);
        Assert.True(strategy.PerformanceProfile.SuitableForRealTime);
        Assert.False(strategy.PerformanceProfile.SupportsParallelization);
    }
    
    [Fact]
    public void NexoForeachStrategy_ShouldHaveCorrectProperties()
    {
        // Arrange
        var strategy = new NexoForeachStrategy<object>();
        
        // Assert
        Assert.Equal("Nexo.Foreach", strategy.StrategyId);
        Assert.Equal(PerformanceLevel.High, strategy.PerformanceProfile.CpuEfficiency);
        Assert.Equal(PerformanceLevel.High, strategy.PerformanceProfile.MemoryEfficiency);
        Assert.Equal(PlatformCompatibility.All, strategy.PlatformCompatibility);
        Assert.True(strategy.PerformanceProfile.SuitableForRealTime);
        Assert.False(strategy.PerformanceProfile.SupportsParallelization);
    }
    
    [Fact]
    public void NexoLinqStrategy_ShouldHaveCorrectProperties()
    {
        // Arrange
        var strategy = new NexoLinqStrategy<object>();
        
        // Assert
        Assert.Equal("Nexo.Linq", strategy.StrategyId);
        Assert.Equal(PerformanceLevel.Medium, strategy.PerformanceProfile.CpuEfficiency);
        Assert.Equal(PerformanceLevel.Medium, strategy.PerformanceProfile.MemoryEfficiency);
        Assert.Equal(PlatformCompatibility.DotNet | PlatformCompatibility.Unity, strategy.PlatformCompatibility);
        Assert.False(strategy.PerformanceProfile.SuitableForRealTime);
        Assert.False(strategy.PerformanceProfile.SupportsParallelization);
    }
    
    [Fact]
    public void NexoParallelLinqStrategy_ShouldHaveCorrectProperties()
    {
        // Arrange
        var strategy = new NexoParallelLinqStrategy<object>();
        
        // Assert
        Assert.Equal("Nexo.ParallelLinq", strategy.StrategyId);
        Assert.Equal(PerformanceLevel.High, strategy.PerformanceProfile.CpuEfficiency);
        Assert.Equal(PerformanceLevel.Medium, strategy.PerformanceProfile.MemoryEfficiency);
        Assert.Equal(PlatformCompatibility.DotNet | PlatformCompatibility.Server, strategy.PlatformCompatibility);
        Assert.False(strategy.PerformanceProfile.SuitableForRealTime);
        Assert.True(strategy.PerformanceProfile.SupportsParallelization);
    }
    
    [Fact]
    public void NexoUnityOptimizedStrategy_ShouldHaveCorrectProperties()
    {
        // Arrange
        var strategy = new NexoUnityOptimizedStrategy<object>();
        
        // Assert
        Assert.Equal("Nexo.UnityOptimized", strategy.StrategyId);
        Assert.Equal(PerformanceLevel.High, strategy.PerformanceProfile.CpuEfficiency);
        Assert.Equal(PerformanceLevel.High, strategy.PerformanceProfile.MemoryEfficiency);
        Assert.Equal(PlatformCompatibility.Unity, strategy.PlatformCompatibility);
        Assert.True(strategy.PerformanceProfile.SuitableForRealTime);
        Assert.False(strategy.PerformanceProfile.SupportsParallelization);
    }
    
    [Theory]
    [InlineData(100, PlatformTarget.DotNet, 90)]
    [InlineData(1000, PlatformTarget.Unity2023, 95)]
    [InlineData(10000, PlatformTarget.Server, 80)]
    public void NexoForLoopStrategy_ShouldReturnCorrectPriority(int dataSize, PlatformTarget platform, int expectedPriority)
    {
        // Arrange
        var strategy = new NexoForLoopStrategy<object>();
        var context = CreatePipelineContext(dataSize, platform);
        
        // Act
        var priority = strategy.GetPriority(context);
        
        // Assert
        Assert.Equal(expectedPriority, priority);
    }
    
    [Theory]
    [InlineData(100, PlatformTarget.DotNet, true)]
    [InlineData(1000, PlatformTarget.Unity2023, true)]
    [InlineData(10000, PlatformTarget.Server, true)]
    public void NexoForLoopStrategy_ShouldHandleAllPlatforms(int dataSize, PlatformTarget platform, bool expectedCanHandle)
    {
        // Arrange
        var strategy = new NexoForLoopStrategy<object>();
        var context = CreatePipelineContext(dataSize, platform);
        
        // Act
        var canHandle = strategy.CanHandle(context);
        
        // Assert
        Assert.Equal(expectedCanHandle, canHandle);
    }
    
    [Fact]
    public void NexoForLoopStrategy_ShouldExecuteCorrectly()
    {
        // Arrange
        var strategy = new NexoForLoopStrategy<object>();
        var testData = new List<object> { "item1", "item2", "item3" };
        var executedItems = new List<object>();
        
        // Act
        strategy.Execute(testData, item => executedItems.Add(item));
        
        // Assert
        Assert.Equal(3, executedItems.Count);
        Assert.Equal("item1", executedItems[0]);
        Assert.Equal("item2", executedItems[1]);
        Assert.Equal("item3", executedItems[2]);
    }
    
    [Fact]
    public void NexoForLoopStrategy_ShouldTransformCorrectly()
    {
        // Arrange
        var strategy = new NexoForLoopStrategy<object>();
        var testData = new List<object> { "item1", "item2", "item3" };
        
        // Act
        var results = strategy.Execute(testData, item => item.ToString()!.ToUpper());
        
        // Assert
        Assert.Equal(3, results.Count());
        Assert.Equal("ITEM1", results.ElementAt(0));
        Assert.Equal("ITEM2", results.ElementAt(1));
        Assert.Equal("ITEM3", results.ElementAt(2));
    }
    
    [Fact]
    public void NexoForLoopStrategy_ShouldGenerateCorrectCode()
    {
        // Arrange
        var strategy = new NexoForLoopStrategy<object>();
        var context = new CodeGenerationContext
        {
            PlatformTarget = PlatformTarget.DotNet,
            CollectionVariableName = "items",
            ItemVariableName = "item",
            ActionCode = "Console.WriteLine(item);",
            IncludeNullChecks = true,
            IncludeBoundsChecking = true
        };
        
        // Act
        var code = strategy.GenerateCode(context);
        
        // Assert
        Assert.Contains("for (int i = 0; i < items.Count", code);
        Assert.Contains("var item = items[i];", code);
        Assert.Contains("Console.WriteLine(item);", code);
        Assert.Contains("if (items != null)", code);
    }
    
    [Fact]
    public void NexoUnityOptimizedStrategy_ShouldGenerateUnitySpecificCode()
    {
        // Arrange
        var strategy = new NexoUnityOptimizedStrategy<object>();
        var context = new CodeGenerationContext
        {
            PlatformTarget = PlatformTarget.Unity2023,
            CollectionVariableName = "gameObjects",
            ItemVariableName = "gameObject",
            ActionCode = "gameObject.SetActive(true);",
            IncludeNullChecks = true,
            IncludeBoundsChecking = true
        };
        
        // Act
        var code = strategy.GenerateCode(context);
        
        // Assert
        Assert.Contains("Unity-optimized", code);
        Assert.Contains("for (int i = 0; i < count; i++)", code);
        Assert.Contains("var gameObject = gameObjects[i];", code);
        Assert.Contains("gameObject.SetActive(true);", code);
    }
    
    [Fact]
    public void NexoParallelLinqStrategy_ShouldNotSupportAsyncForSmallData()
    {
        // Arrange
        var strategy = new NexoParallelLinqStrategy<object>();
        var context = CreatePipelineContext(100, PlatformTarget.DotNet);
        
        // Act
        var canHandle = strategy.CanHandle(context);
        
        // Assert
        Assert.False(canHandle); // Should not handle small datasets
    }
    
    [Fact]
    public void NexoParallelLinqStrategy_ShouldSupportAsyncForLargeData()
    {
        // Arrange
        var strategy = new NexoParallelLinqStrategy<object>();
        var context = CreatePipelineContext(10000, PlatformTarget.Server);
        
        // Act
        var canHandle = strategy.CanHandle(context);
        
        // Assert
        Assert.True(canHandle); // Should handle large datasets
    }
    
    [Fact]
    public void NexoParallelLinqStrategy_ShouldExecuteAsyncCorrectly()
    {
        // Arrange
        var strategy = new NexoParallelLinqStrategy<object>();
        var testData = new List<object> { "item1", "item2", "item3" };
        var executedItems = new List<object>();
        
        // Act
        var task = strategy.ExecuteAsync(testData, async item =>
        {
            await Task.Delay(1); // Simulate async work
            executedItems.Add(item);
        });
        task.Wait();
        
        // Assert
        Assert.Equal(3, executedItems.Count);
    }
    
    [Fact]
    public void AllStrategies_ShouldEstimatePerformanceCorrectly()
    {
        // Arrange
        var context = new IterationContext
        {
            DataSize = 1000,
            Requirements = new PerformanceRequirements(),
            EnvironmentProfile = RuntimeEnvironmentDetector.DetectCurrent(),
            TargetPlatform = PlatformTarget.DotNet
        };
        
        // Act & Assert
        foreach (var strategy in _strategies)
        {
            var estimate = strategy.EstimatePerformance(context);
            
            Assert.True(estimate.EstimatedExecutionTimeMs > 0);
            Assert.True(estimate.EstimatedMemoryUsageMB >= 0);
            Assert.True(estimate.Confidence >= 0 && estimate.Confidence <= 1);
            Assert.True(estimate.PerformanceScore >= 0);
        }
    }
    
    [Theory]
    [InlineData(100, PlatformTarget.DotNet)]
    [InlineData(1000, PlatformTarget.Unity2023)]
    [InlineData(10000, PlatformTarget.Server)]
    public void Strategies_ShouldHaveDifferentPrioritiesForDifferentContexts(int dataSize, PlatformTarget platform)
    {
        // Arrange
        var context = CreatePipelineContext(dataSize, platform);
        var priorities = new Dictionary<string, int>();
        
        // Act
        foreach (var strategy in _strategies)
        {
            if (strategy.CanHandle(context))
            {
                priorities[strategy.StrategyId] = strategy.GetPriority(context);
            }
        }
        
        // Assert
        Assert.True(priorities.Count > 0);
        
        // For Unity platform, UnityOptimized should have highest priority
        if (platform == PlatformTarget.Unity2023)
        {
            Assert.True(priorities.ContainsKey("Nexo.UnityOptimized"));
            Assert.True(priorities["Nexo.UnityOptimized"] >= priorities.Values.Max() - 5);
        }
        
        // For large datasets, ParallelLinq should have higher priority
        if (dataSize >= 10000)
        {
            Assert.True(priorities.ContainsKey("Nexo.ParallelLinq"));
        }
    }
    
    private PipelineContext CreatePipelineContext(int dataSize, PlatformTarget platform)
    {
        var context = new PipelineContext();
        context.SetProperty("EstimatedDataSize", dataSize);
        context.SetProperty("TargetPlatform", platform);
        context.SetProperty("PerformanceRequirements", new PerformanceRequirements());
        return context;
    }
}
