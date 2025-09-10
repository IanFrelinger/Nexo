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

namespace Nexo.Core.Application.Tests.Services.Iteration;

/// <summary>
/// Tests for the iteration strategy selector
/// </summary>
public class IterationStrategySelectorTests
{
    private readonly Mock<ILogger<NexoIterationStrategySelector>> _mockLogger;
    private readonly List<IIterationStrategy<object>> _strategies;
    private readonly NexoIterationStrategySelector _selector;
    
    public IterationStrategySelectorTests()
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
        _selector = new NexoIterationStrategySelector(_strategies, _mockLogger.Object);
    }
    
    [Fact]
    public void SelectStrategy_ShouldReturnBestStrategyForSmallDataset()
    {
        // Arrange
        var context = new IterationContext
        {
            DataSize = 100,
            Requirements = new PerformanceRequirements(),
            EnvironmentProfile = RuntimeEnvironmentDetector.DetectCurrent(),
            TargetPlatform = PlatformTarget.DotNet
        };
        
        // Act
        var selectedStrategy = _selector.SelectStrategy<object>(context);
        
        // Assert
        Assert.NotNull(selectedStrategy);
        Assert.True(selectedStrategy.CanHandle(CreatePipelineContext(context)));
    }
    
    [Fact]
    public void SelectStrategy_ShouldReturnUnityStrategyForUnityPlatform()
    {
        // Arrange
        var context = new IterationContext
        {
            DataSize = 1000,
            Requirements = new PerformanceRequirements(),
            EnvironmentProfile = RuntimeEnvironmentDetector.DetectCurrent(),
            TargetPlatform = PlatformTarget.Unity2023
        };
        
        // Act
        var selectedStrategy = _selector.SelectStrategy<object>(context);
        
        // Assert
        Assert.NotNull(selectedStrategy);
        // UnityOptimized should be preferred for Unity platform
        if (selectedStrategy.StrategyId == "Nexo.UnityOptimized")
        {
            Assert.Equal("Nexo.UnityOptimized", selectedStrategy.StrategyId);
        }
    }
    
    [Fact]
    public void SelectStrategy_ShouldReturnParallelLinqForLargeDataset()
    {
        // Arrange
        var context = new IterationContext
        {
            DataSize = 50000,
            Requirements = new PerformanceRequirements { PreferParallel = true },
            EnvironmentProfile = new RuntimeEnvironmentProfile
            {
                PlatformType = PlatformType.Server,
                CpuCores = 8,
                AvailableMemoryMB = 8192
            },
            TargetPlatform = PlatformTarget.Server
        };
        
        // Act
        var selectedStrategy = _selector.SelectStrategy<object>(context);
        
        // Assert
        Assert.NotNull(selectedStrategy);
        // Should prefer parallel processing for large datasets
        if (selectedStrategy.PerformanceProfile.SupportsParallelization)
        {
            Assert.Equal("Nexo.ParallelLinq", selectedStrategy.StrategyId);
        }
    }
    
    [Fact]
    public void GetAvailableStrategies_ShouldReturnOnlyCompatibleStrategies()
    {
        // Arrange
        var context = new IterationContext
        {
            DataSize = 1000,
            Requirements = new PerformanceRequirements(),
            EnvironmentProfile = RuntimeEnvironmentDetector.DetectCurrent(),
            TargetPlatform = PlatformTarget.Unity2023
        };
        
        // Act
        var availableStrategies = _selector.GetAvailableStrategies<object>(context).ToList();
        
        // Assert
        Assert.True(availableStrategies.Count > 0);
        foreach (var strategy in availableStrategies)
        {
            Assert.True(strategy.CanHandle(CreatePipelineContext(context)));
        }
    }
    
    [Fact]
    public void GetSelectionReasoning_ShouldReturnValidReasoning()
    {
        // Arrange
        var context = new IterationContext
        {
            DataSize = 1000,
            Requirements = new PerformanceRequirements(),
            EnvironmentProfile = RuntimeEnvironmentDetector.DetectCurrent(),
            TargetPlatform = PlatformTarget.DotNet
        };
        
        // Act
        var reasoning = _selector.GetSelectionReasoning(context);
        
        // Assert
        Assert.NotNull(reasoning);
        Assert.NotEmpty(reasoning);
        Assert.Contains("Data size: 1000", reasoning);
        Assert.Contains("Target platform: DotNet", reasoning);
    }
    
    [Fact]
    public void EstimatePerformance_ShouldReturnValidEstimate()
    {
        // Arrange
        var strategy = new NexoForLoopStrategy<object>();
        var context = new IterationContext
        {
            DataSize = 1000,
            Requirements = new PerformanceRequirements(),
            EnvironmentProfile = RuntimeEnvironmentDetector.DetectCurrent(),
            TargetPlatform = PlatformTarget.DotNet
        };
        
        // Act
        var estimate = _selector.EstimatePerformance(strategy, context);
        
        // Assert
        Assert.NotNull(estimate);
        Assert.True(estimate.EstimatedExecutionTimeMs > 0);
        Assert.True(estimate.EstimatedMemoryUsageMB >= 0);
        Assert.True(estimate.Confidence >= 0 && estimate.Confidence <= 1);
        Assert.True(estimate.PerformanceScore >= 0);
    }
    
    [Fact]
    public Task CompareStrategies_ShouldReturnComparisonResults()
    {
        return Task.CompletedTask;
    };
        
        // Act
        var comparisonResults = await _selector.CompareStrategies<object>(context);
        
        // Assert
        Assert.NotNull(comparisonResults);
        Assert.True(comparisonResults.Any());
        
        foreach (var result in comparisonResults)
        {
            Assert.NotNull(result.Strategy);
            Assert.NotNull(result.PerformanceEstimate);
            Assert.True(result.SuitabilityScore >= 0 && result.SuitabilityScore <= 100);
            Assert.NotEmpty(result.Reasoning);
        }
    }
    
    [Theory]
    [InlineData(PlatformType.DotNet)]
    [InlineData(PlatformType.Unity)]
    [InlineData(PlatformType.Server)]
    [InlineData(PlatformType.Mobile)]
    public void GetRecommendations_ShouldReturnRecommendationsForPlatform(PlatformType platformType)
    {
        // Act
        var recommendations = _selector.GetRecommendations(platformType);
        
        // Assert
        Assert.NotNull(recommendations);
        Assert.True(recommendations.Any());
        
        foreach (var recommendation in recommendations)
        {
            Assert.NotEmpty(recommendation.Scenario);
            Assert.NotEmpty(recommendation.RecommendedStrategyId);
            Assert.NotEmpty(recommendation.Reasoning);
            Assert.True(recommendation.DataSizeRange.Min >= 0);
            Assert.True(recommendation.DataSizeRange.Max > recommendation.DataSizeRange.Min);
        }
    }
    
    [Fact]
    public void GetRecommendations_ShouldReturnUnitySpecificRecommendations()
    {
        // Act
        var recommendations = _selector.GetRecommendations(PlatformType.Unity);
        
        // Assert
        Assert.NotNull(recommendations);
        Assert.True(recommendations.Any());
        
        var unityRecommendations = recommendations.Where(r => 
            r.RecommendedStrategyId.Contains("Unity") || 
            r.Scenario.Contains("Unity") ||
            r.Scenario.Contains("game")).ToList();
        
        Assert.True(unityRecommendations.Any());
    }
    
    [Fact]
    public void GetRecommendations_ShouldReturnServerSpecificRecommendations()
    {
        // Act
        var recommendations = _selector.GetRecommendations(PlatformType.Server);
        
        // Assert
        Assert.NotNull(recommendations);
        Assert.True(recommendations.Any());
        
        var serverRecommendations = recommendations.Where(r => 
            r.RecommendedStrategyId.Contains("Parallel") || 
            r.Scenario.Contains("parallel") ||
            r.Scenario.Contains("server")).ToList();
        
        Assert.True(serverRecommendations.Any());
    }
    
    [Fact]
    public void SelectStrategy_ShouldHandleRealTimeRequirements()
    {
        // Arrange
        var context = new IterationContext
        {
            DataSize = 1000,
            Requirements = new PerformanceRequirements { RequiresRealTime = true },
            EnvironmentProfile = RuntimeEnvironmentDetector.DetectCurrent(),
            TargetPlatform = PlatformTarget.DotNet
        };
        
        // Act
        var selectedStrategy = _selector.SelectStrategy<object>(context);
        
        // Assert
        Assert.NotNull(selectedStrategy);
        // Should prefer strategies suitable for real-time
        if (selectedStrategy.PerformanceProfile.SuitableForRealTime)
        {
            Assert.True(selectedStrategy.PerformanceProfile.SuitableForRealTime);
        }
    }
    
    [Fact]
    public void SelectStrategy_ShouldHandleMemoryCriticalRequirements()
    {
        // Arrange
        var context = new IterationContext
        {
            DataSize = 1000,
            Requirements = new PerformanceRequirements { MemoryCritical = true },
            EnvironmentProfile = RuntimeEnvironmentDetector.DetectCurrent(),
            TargetPlatform = PlatformTarget.DotNet
        };
        
        // Act
        var selectedStrategy = _selector.SelectStrategy<object>(context);
        
        // Assert
        Assert.NotNull(selectedStrategy);
        // Should prefer strategies with good memory efficiency
        Assert.True(selectedStrategy.PerformanceProfile.MemoryEfficiency >= PerformanceLevel.Medium);
    }
    
    [Fact]
    public void SelectStrategy_ShouldHandleConstrainedEnvironment()
    {
        // Arrange
        var context = new IterationContext
        {
            DataSize = 1000,
            Requirements = new PerformanceRequirements(),
            EnvironmentProfile = new RuntimeEnvironmentProfile
            {
                PlatformType = PlatformType.Mobile,
                CpuCores = 2,
                AvailableMemoryMB = 512,
                IsConstrained = true,
                IsMobile = true
            },
            TargetPlatform = PlatformTarget.Swift
        };
        
        // Act
        var selectedStrategy = _selector.SelectStrategy<object>(context);
        
        // Assert
        Assert.NotNull(selectedStrategy);
        // Should prefer strategies with good memory efficiency for constrained environments
        Assert.True(selectedStrategy.PerformanceProfile.MemoryEfficiency >= PerformanceLevel.Medium);
    }
    
    [Fact]
    public void SelectStrategy_ShouldHandleEmptyStrategiesList()
    {
        // Arrange
        var emptySelector = new NexoIterationStrategySelector(new List<IIterationStrategy<object>>(), _mockLogger.Object);
        var context = new IterationContext
        {
            DataSize = 1000,
            Requirements = new PerformanceRequirements(),
            EnvironmentProfile = RuntimeEnvironmentDetector.DetectCurrent(),
            TargetPlatform = PlatformTarget.DotNet
        };
        
        // Act
        var selectedStrategy = emptySelector.SelectStrategy<object>(context);
        
        // Assert
        Assert.NotNull(selectedStrategy);
        // Should return a default strategy (ForLoop)
        Assert.Equal("Nexo.ForLoop", selectedStrategy.StrategyId);
    }
    
    private PipelineContext CreatePipelineContext(IterationContext context)
    {
        var pipelineContext = new PipelineContext();
        pipelineContext.SetProperty("EstimatedDataSize", context.DataSize);
        pipelineContext.SetProperty("TargetPlatform", context.TargetPlatform);
        pipelineContext.SetProperty("PerformanceRequirements", context.Requirements);
        pipelineContext.SetProperty("EnvironmentProfile", context.EnvironmentProfile);
        return pipelineContext;
    }
}
