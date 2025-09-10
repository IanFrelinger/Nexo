using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Nexo.Feature.Pipeline.Commands.Iteration;
using Nexo.Core.Application.Services.Iteration;
using Nexo.Core.Domain.Entities.Iteration;

namespace Nexo.Feature.Pipeline.Tests.Commands.Iteration;

/// <summary>
/// Tests for the select iteration strategy command
/// </summary>
public class SelectIterationStrategyCommandTests
{
    private readonly Mock<IIterationStrategySelector> _mockStrategySelector;
    private readonly Mock<ILogger<SelectIterationStrategyCommand>> _mockLogger;
    private readonly SelectIterationStrategyCommand _command;
    
    public SelectIterationStrategyCommandTests()
    {
        _mockStrategySelector = new Mock<IIterationStrategySelector>();
        _mockLogger = new Mock<ILogger<SelectIterationStrategyCommand>>();
        
        _command = new SelectIterationStrategyCommand(
            _mockStrategySelector.Object,
            _mockLogger.Object);
    }
    
    [Fact]
    public Task ExecuteAsync_ShouldReturnSuccessfulResponse()
    {
        return Task.CompletedTask;
    };
        
        var mockStrategy = new Mock<IIterationStrategy<object>>();
        mockStrategy.Setup(x => x.StrategyId).Returns("Nexo.ForLoop");
        
        _mockStrategySelector.Setup(x => x.SelectStrategy<object>(It.IsAny<IterationContext>()))
            .Returns(mockStrategy.Object);
        _mockStrategySelector.Setup(x => x.GetSelectionReasoning(It.IsAny<IterationContext>()))
            .Returns("Selected ForLoop strategy for optimal performance");
        _mockStrategySelector.Setup(x => x.EstimatePerformance(It.IsAny<IIterationStrategy<object>>(), It.IsAny<IterationContext>()))
            .Returns(new PerformanceEstimate
            {
                EstimatedExecutionTimeMs = 1.0,
                EstimatedMemoryUsageMB = 0.1,
                Confidence = 0.9,
                PerformanceScore = 95.0,
                MeetsRequirements = true
            });
        
        // Act
        var response = await _command.ExecuteAsync(request, CancellationToken.None);
        
        // Assert
        Assert.NotNull(response);
        Assert.True(response.Success);
        Assert.NotNull(response.SelectedStrategy);
        Assert.Equal("Nexo.ForLoop", response.SelectedStrategy.StrategyId);
        Assert.NotEmpty(response.SelectionReasoning);
        Assert.NotNull(response.PerformanceEstimate);
        Assert.True(response.PerformanceEstimate.MeetsRequirements);
    }
    
    [Fact]
    public Task ExecuteAsync_ShouldHandleUnityPlatformRequest()
    {
        return Task.CompletedTask;
    };
        
        var mockStrategy = new Mock<IIterationStrategy<object>>();
        mockStrategy.Setup(x => x.StrategyId).Returns("Nexo.UnityOptimized");
        
        _mockStrategySelector.Setup(x => x.SelectStrategy<object>(It.IsAny<IterationContext>()))
            .Returns(mockStrategy.Object);
        _mockStrategySelector.Setup(x => x.GetSelectionReasoning(It.IsAny<IterationContext>()))
            .Returns("Selected UnityOptimized strategy for Unity platform");
        _mockStrategySelector.Setup(x => x.EstimatePerformance(It.IsAny<IIterationStrategy<object>>(), It.IsAny<IterationContext>()))
            .Returns(new PerformanceEstimate
            {
                EstimatedExecutionTimeMs = 0.8,
                EstimatedMemoryUsageMB = 0.05,
                Confidence = 0.95,
                PerformanceScore = 98.0,
                MeetsRequirements = true
            });
        
        // Act
        var response = await _command.ExecuteAsync(request, CancellationToken.None);
        
        // Assert
        Assert.NotNull(response);
        Assert.True(response.Success);
        Assert.NotNull(response.SelectedStrategy);
        Assert.Equal("Nexo.UnityOptimized", response.SelectedStrategy.StrategyId);
        Assert.Contains("Unity", response.SelectionReasoning);
    }
    
    [Fact]
    public Task ExecuteAsync_ShouldHandleRealTimeRequirements()
    {
        return Task.CompletedTask;
    },
            EnvironmentProfile = RuntimeEnvironmentDetector.DetectCurrent(),
            TargetPlatform = PlatformTarget.DotNet
        };
        
        var mockStrategy = new Mock<IIterationStrategy<object>>();
        mockStrategy.Setup(x => x.StrategyId).Returns("Nexo.ForLoop");
        mockStrategy.Setup(x => x.PerformanceProfile).Returns(new IterationPerformanceProfile
        {
            SuitableForRealTime = true
        });
        
        _mockStrategySelector.Setup(x => x.SelectStrategy<object>(It.IsAny<IterationContext>()))
            .Returns(mockStrategy.Object);
        _mockStrategySelector.Setup(x => x.GetSelectionReasoning(It.IsAny<IterationContext>()))
            .Returns("Selected ForLoop strategy for real-time performance");
        _mockStrategySelector.Setup(x => x.EstimatePerformance(It.IsAny<IIterationStrategy<object>>(), It.IsAny<IterationContext>()))
            .Returns(new PerformanceEstimate
            {
                EstimatedExecutionTimeMs = 0.5,
                EstimatedMemoryUsageMB = 0.05,
                Confidence = 0.95,
                PerformanceScore = 98.0,
                MeetsRequirements = true
            });
        
        // Act
        var response = await _command.ExecuteAsync(request, CancellationToken.None);
        
        // Assert
        Assert.NotNull(response);
        Assert.True(response.Success);
        Assert.NotNull(response.SelectedStrategy);
        Assert.True(response.SelectedStrategy.PerformanceProfile.SuitableForRealTime);
    }
    
    [Fact]
    public Task ExecuteAsync_ShouldHandleParallelProcessingRequirements()
    {
        return Task.CompletedTask;
    },
            EnvironmentProfile = RuntimeEnvironmentDetector.DetectCurrent(),
            TargetPlatform = PlatformTarget.Server
        };
        
        var mockStrategy = new Mock<IIterationStrategy<object>>();
        mockStrategy.Setup(x => x.StrategyId).Returns("Nexo.ParallelLinq");
        mockStrategy.Setup(x => x.PerformanceProfile).Returns(new IterationPerformanceProfile
        {
            SupportsParallelization = true
        });
        
        _mockStrategySelector.Setup(x => x.SelectStrategy<object>(It.IsAny<IterationContext>()))
            .Returns(mockStrategy.Object);
        _mockStrategySelector.Setup(x => x.GetSelectionReasoning(It.IsAny<IterationContext>()))
            .Returns("Selected ParallelLinq strategy for parallel processing");
        _mockStrategySelector.Setup(x => x.EstimatePerformance(It.IsAny<IIterationStrategy<object>>(), It.IsAny<IterationContext>()))
            .Returns(new PerformanceEstimate
            {
                EstimatedExecutionTimeMs = 5.0,
                EstimatedMemoryUsageMB = 1.0,
                Confidence = 0.85,
                PerformanceScore = 90.0,
                MeetsRequirements = true
            });
        
        // Act
        var response = await _command.ExecuteAsync(request, CancellationToken.None);
        
        // Assert
        Assert.NotNull(response);
        Assert.True(response.Success);
        Assert.NotNull(response.SelectedStrategy);
        Assert.True(response.SelectedStrategy.PerformanceProfile.SupportsParallelization);
    }
    
    [Fact]
    public Task ExecuteAsync_ShouldHandleMemoryCriticalRequirements()
    {
        return Task.CompletedTask;
    },
            EnvironmentProfile = RuntimeEnvironmentDetector.DetectCurrent(),
            TargetPlatform = PlatformTarget.Swift
        };
        
        var mockStrategy = new Mock<IIterationStrategy<object>>();
        mockStrategy.Setup(x => x.StrategyId).Returns("Nexo.ForLoop");
        mockStrategy.Setup(x => x.PerformanceProfile).Returns(new IterationPerformanceProfile
        {
            MemoryEfficiency = PerformanceLevel.High
        });
        
        _mockStrategySelector.Setup(x => x.SelectStrategy<object>(It.IsAny<IterationContext>()))
            .Returns(mockStrategy.Object);
        _mockStrategySelector.Setup(x => x.GetSelectionReasoning(It.IsAny<IterationContext>()))
            .Returns("Selected ForLoop strategy for memory efficiency");
        _mockStrategySelector.Setup(x => x.EstimatePerformance(It.IsAny<IIterationStrategy<object>>(), It.IsAny<IterationContext>()))
            .Returns(new PerformanceEstimate
            {
                EstimatedExecutionTimeMs = 1.0,
                EstimatedMemoryUsageMB = 0.01,
                Confidence = 0.9,
                PerformanceScore = 95.0,
                MeetsRequirements = true
            });
        
        // Act
        var response = await _command.ExecuteAsync(request, CancellationToken.None);
        
        // Assert
        Assert.NotNull(response);
        Assert.True(response.Success);
        Assert.NotNull(response.SelectedStrategy);
        Assert.Equal(PerformanceLevel.High, response.SelectedStrategy.PerformanceProfile.MemoryEfficiency);
    }
    
    [Fact]
    public Task ExecuteAsync_ShouldHandleAsyncRequirements()
    {
        return Task.CompletedTask;
    };
        
        var mockStrategy = new Mock<IIterationStrategy<object>>();
        mockStrategy.Setup(x => x.StrategyId).Returns("Nexo.ParallelLinq");
        mockStrategy.Setup(x => x.PerformanceProfile).Returns(new IterationPerformanceProfile
        {
            SupportsAsync = true
        });
        
        _mockStrategySelector.Setup(x => x.SelectStrategy<object>(It.IsAny<IterationContext>()))
            .Returns(mockStrategy.Object);
        _mockStrategySelector.Setup(x => x.GetSelectionReasoning(It.IsAny<IterationContext>()))
            .Returns("Selected ParallelLinq strategy for async processing");
        _mockStrategySelector.Setup(x => x.EstimatePerformance(It.IsAny<IIterationStrategy<object>>(), It.IsAny<IterationContext>()))
            .Returns(new PerformanceEstimate
            {
                EstimatedExecutionTimeMs = 2.0,
                EstimatedMemoryUsageMB = 0.5,
                Confidence = 0.8,
                PerformanceScore = 85.0,
                MeetsRequirements = true
            });
        
        // Act
        var response = await _command.ExecuteAsync(request, CancellationToken.None);
        
        // Assert
        Assert.NotNull(response);
        Assert.True(response.Success);
        Assert.NotNull(response.SelectedStrategy);
        Assert.True(response.SelectedStrategy.PerformanceProfile.SupportsAsync);
    }
    
    [Fact]
    public Task ExecuteAsync_ShouldHandleErrorsGracefully()
    {
        return Task.CompletedTask;
    };
        
        _mockStrategySelector.Setup(x => x.SelectStrategy<object>(It.IsAny<IterationContext>()))
            .Throws(new Exception("Strategy selection failed"));
        
        // Act
        var response = await _command.ExecuteAsync(request, CancellationToken.None);
        
        // Assert
        Assert.NotNull(response);
        Assert.False(response.Success);
        Assert.Equal("Strategy selection failed", response.ErrorMessage);
    }
    
    [Fact]
    public Task ExecuteAsync_ShouldUseDefaultEnvironmentProfileWhenNotProvided()
    {
        return Task.CompletedTask;
    };
        
        var mockStrategy = new Mock<IIterationStrategy<object>>();
        mockStrategy.Setup(x => x.StrategyId).Returns("Nexo.ForLoop");
        
        _mockStrategySelector.Setup(x => x.SelectStrategy<object>(It.IsAny<IterationContext>()))
            .Returns(mockStrategy.Object);
        _mockStrategySelector.Setup(x => x.GetSelectionReasoning(It.IsAny<IterationContext>()))
            .Returns("Selected ForLoop strategy");
        _mockStrategySelector.Setup(x => x.EstimatePerformance(It.IsAny<IIterationStrategy<object>>(), It.IsAny<IterationContext>()))
            .Returns(new PerformanceEstimate
            {
                EstimatedExecutionTimeMs = 1.0,
                EstimatedMemoryUsageMB = 0.1,
                Confidence = 0.9,
                PerformanceScore = 95.0,
                MeetsRequirements = true
            });
        
        // Act
        var response = await _command.ExecuteAsync(request, CancellationToken.None);
        
        // Assert
        Assert.NotNull(response);
        Assert.True(response.Success);
        Assert.NotNull(response.Context);
        Assert.NotNull(response.Context.EnvironmentProfile);
    }
    
    [Fact]
    public Task ExecuteAsync_ShouldCreateCorrectIterationContext()
    {
        return Task.CompletedTask;
    },
            EnvironmentProfile = RuntimeEnvironmentDetector.DetectCurrent(),
            TargetPlatform = PlatformTarget.Unity2023,
            IsCpuBound = true,
            IsIoBound = false,
            RequiresAsync = false
        };
        
        var mockStrategy = new Mock<IIterationStrategy<object>>();
        mockStrategy.Setup(x => x.StrategyId).Returns("Nexo.UnityOptimized");
        
        _mockStrategySelector.Setup(x => x.SelectStrategy<object>(It.IsAny<IterationContext>()))
            .Returns(mockStrategy.Object);
        _mockStrategySelector.Setup(x => x.GetSelectionReasoning(It.IsAny<IterationContext>()))
            .Returns("Selected UnityOptimized strategy");
        _mockStrategySelector.Setup(x => x.EstimatePerformance(It.IsAny<IIterationStrategy<object>>(), It.IsAny<IterationContext>()))
            .Returns(new PerformanceEstimate
            {
                EstimatedExecutionTimeMs = 0.8,
                EstimatedMemoryUsageMB = 0.05,
                Confidence = 0.95,
                PerformanceScore = 98.0,
                MeetsRequirements = true
            });
        
        // Act
        var response = await _command.ExecuteAsync(request, CancellationToken.None);
        
        // Assert
        Assert.NotNull(response);
        Assert.True(response.Success);
        Assert.NotNull(response.Context);
        Assert.Equal(5000, response.Context.DataSize);
        Assert.Equal(PlatformTarget.Unity2023, response.Context.TargetPlatform);
        Assert.True(response.Context.Requirements.RequiresRealTime);
        Assert.True(response.Context.IsCpuBound);
        Assert.False(response.Context.IsIoBound);
        Assert.False(response.Context.RequiresAsync);
    }
}
