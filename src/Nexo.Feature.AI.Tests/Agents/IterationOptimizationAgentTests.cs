using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Nexo.Feature.AI.Agents;
using Nexo.Feature.AI.Interfaces;
using Nexo.Feature.AI.Models;
using Nexo.Core.Application.Services.Iteration;
using Nexo.Core.Domain.Entities.Iteration;

namespace Nexo.Feature.AI.Tests.Agents;

/// <summary>
/// Tests for the iteration optimization AI agent
/// </summary>
public class IterationOptimizationAgentTests
{
    private readonly Mock<IIterationStrategySelector> _mockStrategySelector;
    private readonly Mock<IModelOrchestrator> _mockModelOrchestrator;
    private readonly Mock<ILogger<IterationOptimizationAgent>> _mockLogger;
    private readonly IterationOptimizationAgent _agent;
    
    public IterationOptimizationAgentTests()
    {
        _mockStrategySelector = new Mock<IIterationStrategySelector>();
        _mockModelOrchestrator = new Mock<IModelOrchestrator>();
        _mockLogger = new Mock<ILogger<IterationOptimizationAgent>>();
        
        _agent = new IterationOptimizationAgent(
            _mockStrategySelector.Object,
            _mockModelOrchestrator.Object,
            _mockLogger.Object);
    }
    
    [Fact]
    public void Agent_ShouldHaveCorrectProperties()
    {
        // Assert
        Assert.Equal("IterationOptimization", _agent.AgentId);
        Assert.Equal(AgentCapabilities.CodeGeneration | AgentCapabilities.PerformanceAnalysis, _agent.Capabilities);
    }
    
    [Fact]
    public Task ProcessAsync_ShouldReturnNoActionForNonIterationRequest()
    {
        return Task.CompletedTask;
    };
        
        // Mock the model orchestrator to return analysis indicating no iteration needed
        _mockModelOrchestrator.Setup(x => x.ProcessAsync(It.IsAny<string>()))
            .ReturnsAsync(new ModelResponse
            {
                Response = "RequiresIteration: false\nEstimatedDataSize: 0\nTargetPlatform: DotNet",
                IsSuccessful = true
            });
        
        // Act
        var response = await _agent.ProcessAsync(request);
        
        // Assert
        Assert.NotNull(response);
        Assert.Equal(AgentResponse.NoAction, response);
    }
    
    [Fact]
    public Task ProcessAsync_ShouldReturnOptimizedCodeForIterationRequest()
    {
        return Task.CompletedTask;
    };
        
        var mockStrategy = new Mock<IIterationStrategy<object>>();
        mockStrategy.Setup(x => x.StrategyId).Returns("Nexo.ForLoop");
        mockStrategy.Setup(x => x.PerformanceProfile).Returns(new IterationPerformanceProfile
        {
            CpuEfficiency = PerformanceLevel.High,
            MemoryEfficiency = PerformanceLevel.High
        });
        mockStrategy.Setup(x => x.EstimatePerformance(It.IsAny<IterationContext>()))
            .Returns(new PerformanceEstimate
            {
                EstimatedExecutionTimeMs = 1.0,
                EstimatedMemoryUsageMB = 0.1,
                Confidence = 0.9,
                PerformanceScore = 95.0,
                MeetsRequirements = true
            });
        
        // Mock the model orchestrator to return analysis indicating iteration is needed
        _mockModelOrchestrator.Setup(x => x.ProcessAsync(It.IsAny<string>()))
            .ReturnsAsync(new ModelResponse
            {
                Response = "RequiresIteration: true\nEstimatedDataSize: 1000\nTargetPlatform: DotNet\nCollectionVariableName: items\nItemVariableName: item\nActionCode: // Process item\nRequiresComplexLogic: false",
                IsSuccessful = true
            });
        
        // Mock the strategy selector
        _mockStrategySelector.Setup(x => x.SelectStrategy<object>(It.IsAny<IterationContext>()))
            .Returns(mockStrategy.Object);
        
        // Act
        var response = await _agent.ProcessAsync(request);
        
        // Assert
        Assert.NotNull(response);
        Assert.NotEqual(AgentResponse.NoAction, response);
        Assert.True(response.Confidence > 0);
        Assert.NotNull(response.Metadata);
        Assert.True(response.Metadata.ContainsKey("IterationStrategy"));
        Assert.Equal("Nexo.ForLoop", response.Metadata["IterationStrategy"]);
    }
    
    [Fact]
    public Task ProcessAsync_ShouldHandleComplexLogicEnhancement()
    {
        return Task.CompletedTask;
    };
        
        var mockStrategy = new Mock<IIterationStrategy<object>>();
        mockStrategy.Setup(x => x.StrategyId).Returns("Nexo.ForLoop");
        mockStrategy.Setup(x => x.PerformanceProfile).Returns(new IterationPerformanceProfile());
        mockStrategy.Setup(x => x.GenerateCode(It.IsAny<CodeGenerationContext>()))
            .Returns("// Basic for-loop code");
        mockStrategy.Setup(x => x.EstimatePerformance(It.IsAny<IterationContext>()))
            .Returns(new PerformanceEstimate
            {
                EstimatedExecutionTimeMs = 1.0,
                EstimatedMemoryUsageMB = 0.1,
                Confidence = 0.9,
                PerformanceScore = 95.0,
                MeetsRequirements = true
            });
        
        // Mock the model orchestrator to return analysis indicating complex logic is needed
        _mockModelOrchestrator.SetupSequence(x => x.ProcessAsync(It.IsAny<string>()))
            .ReturnsAsync(new ModelResponse
            {
                Response = "RequiresIteration: true\nEstimatedDataSize: 1000\nTargetPlatform: DotNet\nCollectionVariableName: items\nItemVariableName: item\nActionCode: // Process item\nRequiresComplexLogic: true",
                IsSuccessful = true
            })
            .ReturnsAsync(new ModelResponse
            {
                Response = "// Enhanced code with error handling and logging\nfor (int i = 0; i < items.Count; i++)\n{\n    try\n    {\n        var item = items[i];\n        // Process item with error handling\n        logger.LogInformation(\"Processing item {Index}\", i);\n    }\n    catch (Exception ex)\n    {\n        logger.LogError(ex, \"Error processing item {Index}\", i);\n    }\n}",
                IsSuccessful = true
            });
        
        // Mock the strategy selector
        _mockStrategySelector.Setup(x => x.SelectStrategy<object>(It.IsAny<IterationContext>()))
            .Returns(mockStrategy.Object);
        
        // Act
        var response = await _agent.ProcessAsync(request);
        
        // Assert
        Assert.NotNull(response);
        Assert.NotEqual(AgentResponse.NoAction, response);
        Assert.True(response.Confidence > 0);
        Assert.NotNull(response.Result);
        Assert.Contains("Enhanced code", response.Result);
        Assert.Contains("error handling", response.Result);
    }
    
    [Fact]
    public Task ProcessAsync_ShouldHandleErrorsGracefully()
    {
        return Task.CompletedTask;
    };
        
        // Mock the model orchestrator to throw an exception
        _mockModelOrchestrator.Setup(x => x.ProcessAsync(It.IsAny<string>()))
            .ThrowsAsync(new Exception("AI service unavailable"));
        
        // Act
        var response = await _agent.ProcessAsync(request);
        
        // Assert
        Assert.NotNull(response);
        Assert.Equal(0.0, response.Confidence);
        Assert.Equal("AI service unavailable", response.ErrorMessage);
    }
    
    [Fact]
    public Task ProcessAsync_ShouldHandleUnityPlatformRequest()
    {
        return Task.CompletedTask;
    };
        
        var mockStrategy = new Mock<IIterationStrategy<object>>();
        mockStrategy.Setup(x => x.StrategyId).Returns("Nexo.UnityOptimized");
        mockStrategy.Setup(x => x.PerformanceProfile).Returns(new IterationPerformanceProfile
        {
            CpuEfficiency = PerformanceLevel.High,
            MemoryEfficiency = PerformanceLevel.High
        });
        mockStrategy.Setup(x => x.EstimatePerformance(It.IsAny<IterationContext>()))
            .Returns(new PerformanceEstimate
            {
                EstimatedExecutionTimeMs = 0.8,
                EstimatedMemoryUsageMB = 0.05,
                Confidence = 0.95,
                PerformanceScore = 98.0,
                MeetsRequirements = true
            });
        
        // Mock the model orchestrator to return Unity-specific analysis
        _mockModelOrchestrator.Setup(x => x.ProcessAsync(It.IsAny<string>()))
            .ReturnsAsync(new ModelResponse
            {
                Response = "RequiresIteration: true\nEstimatedDataSize: 1000\nTargetPlatform: Unity2023\nCollectionVariableName: gameObjects\nItemVariableName: gameObject\nActionCode: // Process game object\nRequiresComplexLogic: false",
                IsSuccessful = true
            });
        
        // Mock the strategy selector
        _mockStrategySelector.Setup(x => x.SelectStrategy<object>(It.IsAny<IterationContext>()))
            .Returns(mockStrategy.Object);
        
        // Act
        var response = await _agent.ProcessAsync(request);
        
        // Assert
        Assert.NotNull(response);
        Assert.NotEqual(AgentResponse.NoAction, response);
        Assert.True(response.Confidence > 0);
        Assert.NotNull(response.Metadata);
        Assert.True(response.Metadata.ContainsKey("IterationStrategy"));
        Assert.Equal("Nexo.UnityOptimized", response.Metadata["IterationStrategy"]);
    }
    
    [Fact]
    public Task ProcessAsync_ShouldHandleLargeDatasetRequest()
    {
        return Task.CompletedTask;
    };
        
        var mockStrategy = new Mock<IIterationStrategy<object>>();
        mockStrategy.Setup(x => x.StrategyId).Returns("Nexo.ParallelLinq");
        mockStrategy.Setup(x => x.PerformanceProfile).Returns(new IterationPerformanceProfile
        {
            CpuEfficiency = PerformanceLevel.High,
            MemoryEfficiency = PerformanceLevel.Medium,
            SupportsParallelization = true
        });
        mockStrategy.Setup(x => x.EstimatePerformance(It.IsAny<IterationContext>()))
            .Returns(new PerformanceEstimate
            {
                EstimatedExecutionTimeMs = 50.0,
                EstimatedMemoryUsageMB = 2.0,
                Confidence = 0.85,
                PerformanceScore = 90.0,
                MeetsRequirements = true
            });
        
        // Mock the model orchestrator to return large dataset analysis
        _mockModelOrchestrator.Setup(x => x.ProcessAsync(It.IsAny<string>()))
            .ReturnsAsync(new ModelResponse
            {
                Response = "RequiresIteration: true\nEstimatedDataSize: 100000\nTargetPlatform: Server\nCollectionVariableName: items\nItemVariableName: item\nActionCode: // Process item\nRequiresComplexLogic: false",
                IsSuccessful = true
            });
        
        // Mock the strategy selector
        _mockStrategySelector.Setup(x => x.SelectStrategy<object>(It.IsAny<IterationContext>()))
            .Returns(mockStrategy.Object);
        
        // Act
        var response = await _agent.ProcessAsync(request);
        
        // Assert
        Assert.NotNull(response);
        Assert.NotEqual(AgentResponse.NoAction, response);
        Assert.True(response.Confidence > 0);
        Assert.NotNull(response.Metadata);
        Assert.True(response.Metadata.ContainsKey("IterationStrategy"));
        Assert.Equal("Nexo.ParallelLinq", response.Metadata["IterationStrategy"]);
    }
    
    [Fact]
    public Task ProcessAsync_ShouldCalculateConfidenceCorrectly()
    {
        return Task.CompletedTask;
    };
        
        var mockStrategy = new Mock<IIterationStrategy<object>>();
        mockStrategy.Setup(x => x.StrategyId).Returns("Nexo.ForLoop");
        mockStrategy.Setup(x => x.PerformanceProfile).Returns(new IterationPerformanceProfile());
        mockStrategy.Setup(x => x.EstimatePerformance(It.IsAny<IterationContext>()))
            .Returns(new PerformanceEstimate
            {
                EstimatedExecutionTimeMs = 1.0,
                EstimatedMemoryUsageMB = 0.1,
                Confidence = 0.9,
                PerformanceScore = 95.0,
                MeetsRequirements = true
            });
        
        // Mock the model orchestrator to return well-defined analysis
        _mockModelOrchestrator.Setup(x => x.ProcessAsync(It.IsAny<string>()))
            .ReturnsAsync(new ModelResponse
            {
                Response = "RequiresIteration: true\nEstimatedDataSize: 1000\nTargetPlatform: DotNet\nCollectionVariableName: items\nItemVariableName: item\nActionCode: // Process item\nRequiresComplexLogic: false",
                IsSuccessful = true
            });
        
        // Mock the strategy selector
        _mockStrategySelector.Setup(x => x.SelectStrategy<object>(It.IsAny<IterationContext>()))
            .Returns(mockStrategy.Object);
        
        // Act
        var response = await _agent.ProcessAsync(request);
        
        // Assert
        Assert.NotNull(response);
        Assert.True(response.Confidence > 0.8); // Should have high confidence for well-defined requirements
    }
}
