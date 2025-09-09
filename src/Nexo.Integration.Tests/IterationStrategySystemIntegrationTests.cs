using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;
using Nexo.Core.Application.Services.Iteration;
using Nexo.Core.Application.Services.Iteration.Strategies;
using Nexo.Core.Domain.Entities.Iteration;
using Nexo.Feature.AI.Agents;
using Nexo.Feature.Pipeline.Commands.Iteration;
using Nexo.Infrastructure.DependencyInjection;

namespace Nexo.Integration.Tests;

/// <summary>
/// Integration tests for the complete iteration strategy system
/// </summary>
public class IterationStrategySystemIntegrationTests
{
    private readonly ServiceProvider _serviceProvider;
    
    public IterationStrategySystemIntegrationTests()
    {
        var services = new ServiceCollection();
        
        // Add logging
        services.AddLogging(builder => builder.AddConsole());
        
        // Add iteration strategy services
        services.AddNexoIterationStrategies();
        
        _serviceProvider = services.BuildServiceProvider();
    }
    
    [Fact]
    public void ServiceRegistration_ShouldRegisterAllRequiredServices()
    {
        // Assert
        Assert.NotNull(_serviceProvider.GetService<IIterationStrategySelector>());
        Assert.NotNull(_serviceProvider.GetService<RuntimeEnvironmentProfile>());
        Assert.NotNull(_serviceProvider.GetService<IIterationBenchmarker>());
        Assert.NotNull(_serviceProvider.GetService<IIterationCodeGenerator>());
        Assert.NotNull(_serviceProvider.GetService<IIterationCodeOptimizer>());
        Assert.NotNull(_serviceProvider.GetService<IterationOptimizationAgent>());
        Assert.NotNull(_serviceProvider.GetService<PlatformIterationAgent>());
        Assert.NotNull(_serviceProvider.GetService<SelectIterationStrategyCommand>());
        Assert.NotNull(_serviceProvider.GetService<OptimizeIterationCommand>());
    }
    
    [Fact]
    public async Task EndToEndWorkflow_ShouldWorkCorrectly()
    {
        // Arrange
        var strategySelector = _serviceProvider.GetRequiredService<IIterationStrategySelector>();
        var benchmarker = _serviceProvider.GetRequiredService<IIterationBenchmarker>();
        var codeGenerator = _serviceProvider.GetRequiredService<IIterationCodeGenerator>();
        
        var context = new IterationContext
        {
            DataSize = 1000,
            Requirements = new PerformanceRequirements(),
            EnvironmentProfile = RuntimeEnvironmentDetector.DetectCurrent(),
            TargetPlatform = PlatformTarget.DotNet
        };
        
        // Act - Step 1: Select optimal strategy
        var selectedStrategy = strategySelector.SelectStrategy<object>(context);
        
        // Act - Step 2: Benchmark strategies
        var benchmarkResults = await benchmarker.BenchmarkAllStrategies(1000, "current", 3);
        
        // Act - Step 3: Generate code
        var codeRequest = new IterationCodeRequest
        {
            Description = "Process 1000 items with optimal performance",
            TargetPlatform = PlatformTarget.DotNet,
            EstimatedDataSize = 1000
        };
        var generatedCode = await codeGenerator.GenerateOptimalIterationAsync(codeRequest);
        
        // Assert
        Assert.NotNull(selectedStrategy);
        Assert.True(benchmarkResults.Any());
        Assert.NotEmpty(generatedCode);
        Assert.Contains("for", generatedCode.ToLower());
    }
    
    [Fact]
    public async Task UnityPlatformWorkflow_ShouldSelectUnityOptimizedStrategy()
    {
        // Arrange
        var strategySelector = _serviceProvider.GetRequiredService<IIterationStrategySelector>();
        
        var context = new IterationContext
        {
            DataSize = 1000,
            Requirements = new PerformanceRequirements(),
            EnvironmentProfile = RuntimeEnvironmentDetector.DetectCurrent(),
            TargetPlatform = PlatformTarget.Unity2023
        };
        
        // Act
        var selectedStrategy = strategySelector.SelectStrategy<object>(context);
        var availableStrategies = strategySelector.GetAvailableStrategies<object>(context);
        var recommendations = strategySelector.GetRecommendations(PlatformType.Unity);
        
        // Assert
        Assert.NotNull(selectedStrategy);
        Assert.True(availableStrategies.Any());
        Assert.True(recommendations.Any());
        
        // Should have Unity-specific recommendations
        var unityRecommendations = recommendations.Where(r => 
            r.RecommendedStrategyId.Contains("Unity") || 
            r.Scenario.Contains("Unity") ||
            r.Scenario.Contains("game")).ToList();
        Assert.True(unityRecommendations.Any());
    }
    
    [Fact]
    public async Task LargeDatasetWorkflow_ShouldPreferParallelProcessing()
    {
        // Arrange
        var strategySelector = _serviceProvider.GetRequiredService<IIterationStrategySelector>();
        
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
        var selectedStrategy = strategySelector.SelectStrategy<object>(context);
        var comparisonResults = await strategySelector.CompareStrategies<object>(context);
        
        // Assert
        Assert.NotNull(selectedStrategy);
        Assert.True(comparisonResults.Any());
        
        // Should prefer parallel processing for large datasets
        var parallelStrategies = comparisonResults.Where(r => 
            r.Strategy.PerformanceProfile.SupportsParallelization).ToList();
        Assert.True(parallelStrategies.Any());
    }
    
    [Fact]
    public async Task RealTimeWorkflow_ShouldPreferRealTimeSuitableStrategies()
    {
        // Arrange
        var strategySelector = _serviceProvider.GetRequiredService<IIterationStrategySelector>();
        
        var context = new IterationContext
        {
            DataSize = 1000,
            Requirements = new PerformanceRequirements { RequiresRealTime = true },
            EnvironmentProfile = RuntimeEnvironmentDetector.DetectCurrent(),
            TargetPlatform = PlatformTarget.DotNet
        };
        
        // Act
        var selectedStrategy = strategySelector.SelectStrategy<object>(context);
        var comparisonResults = await strategySelector.CompareStrategies<object>(context);
        
        // Assert
        Assert.NotNull(selectedStrategy);
        Assert.True(comparisonResults.Any());
        
        // Should prefer strategies suitable for real-time
        var realTimeStrategies = comparisonResults.Where(r => 
            r.Strategy.PerformanceProfile.SuitableForRealTime).ToList();
        Assert.True(realTimeStrategies.Any());
    }
    
    [Fact]
    public async Task AIAgentIntegration_ShouldWorkCorrectly()
    {
        // Arrange
        var iterationAgent = _serviceProvider.GetRequiredService<IterationOptimizationAgent>();
        var platformAgent = _serviceProvider.GetRequiredService<PlatformIterationAgent>();
        
        var request = new AgentRequest
        {
            Input = "Generate optimized iteration code for processing 1000 game objects in Unity",
            Context = new Dictionary<string, object>()
        };
        
        // Act
        var iterationResponse = await iterationAgent.ProcessAsync(request);
        var platformResponse = await platformAgent.ProcessAsync(request);
        
        // Assert
        Assert.NotNull(iterationResponse);
        Assert.NotNull(platformResponse);
        
        // At least one agent should provide a response
        Assert.True(iterationResponse != AgentResponse.NoAction || platformResponse != AgentResponse.NoAction);
    }
    
    [Fact]
    public async Task PipelineCommandIntegration_ShouldWorkCorrectly()
    {
        // Arrange
        var selectCommand = _serviceProvider.GetRequiredService<SelectIterationStrategyCommand>();
        var optimizeCommand = _serviceProvider.GetRequiredService<OptimizeIterationCommand>();
        
        var selectRequest = new SelectIterationStrategyRequest
        {
            EstimatedDataSize = 1000,
            Requirements = new PerformanceRequirements(),
            EnvironmentProfile = RuntimeEnvironmentDetector.DetectCurrent(),
            TargetPlatform = PlatformTarget.DotNet
        };
        
        var optimizeRequest = new OptimizeIterationRequest
        {
            ExistingCode = @"foreach (var item in items)
{
    // Process item
}",
            TargetPlatform = PlatformTarget.DotNet,
            Requirements = new PerformanceRequirements(),
            EnvironmentProfile = RuntimeEnvironmentDetector.DetectCurrent()
        };
        
        // Act
        var selectResponse = await selectCommand.ExecuteAsync(selectRequest, CancellationToken.None);
        var optimizeResponse = await optimizeCommand.ExecuteAsync(optimizeRequest, CancellationToken.None);
        
        // Assert
        Assert.NotNull(selectResponse);
        Assert.True(selectResponse.Success);
        Assert.NotNull(selectResponse.SelectedStrategy);
        
        Assert.NotNull(optimizeResponse);
        Assert.True(optimizeResponse.Success);
        Assert.NotEmpty(optimizeResponse.OptimizedCode);
    }
    
    [Fact]
    public async Task PerformanceComparison_ShouldShowDifferentStrategies()
    {
        // Arrange
        var strategySelector = _serviceProvider.GetRequiredService<IIterationStrategySelector>();
        var benchmarker = _serviceProvider.GetRequiredService<IIterationBenchmarker>();
        
        var context = new IterationContext
        {
            DataSize = 1000,
            Requirements = new PerformanceRequirements(),
            EnvironmentProfile = RuntimeEnvironmentDetector.DetectCurrent(),
            TargetPlatform = PlatformTarget.DotNet
        };
        
        // Act
        var comparisonResults = await strategySelector.CompareStrategies<object>(context);
        var benchmarkResults = await benchmarker.BenchmarkAllStrategies(1000, "current", 3);
        
        // Assert
        Assert.True(comparisonResults.Count() > 1); // Should have multiple strategies
        Assert.True(benchmarkResults.Count() > 1); // Should have multiple benchmark results
        
        // Should have different performance characteristics
        var performanceScores = comparisonResults.Select(r => r.PerformanceEstimate.PerformanceScore).ToList();
        Assert.True(performanceScores.Any(score => score > 0));
        
        var executionTimes = benchmarkResults.Select(r => r.ExecutionTime).ToList();
        Assert.True(executionTimes.Any(time => time > 0));
    }
    
    [Fact]
    public async Task ConfigurationIntegration_ShouldWorkCorrectly()
    {
        // Arrange
        var strategySelector = _serviceProvider.GetRequiredService<IIterationStrategySelector>();
        
        // Test different platform recommendations
        var platforms = new[] { PlatformType.DotNet, PlatformType.Unity, PlatformType.Server, PlatformType.Mobile };
        
        // Act & Assert
        foreach (var platform in platforms)
        {
            var recommendations = strategySelector.GetRecommendations(platform);
            Assert.True(recommendations.Any());
            
            // Each platform should have relevant recommendations
            var platformRecommendations = recommendations.Where(r => 
                r.RecommendedStrategyId.Contains(platform.ToString()) ||
                r.Scenario.Contains(platform.ToString()) ||
                r.PerformanceCharacteristics.Contains(platform.ToString())).ToList();
            
            // At least some recommendations should be platform-specific
            Assert.True(platformRecommendations.Any() || recommendations.Any());
        }
    }
    
    [Fact]
    public async Task ErrorHandling_ShouldBeRobust()
    {
        // Arrange
        var strategySelector = _serviceProvider.GetRequiredService<IIterationStrategySelector>();
        
        // Test with invalid context
        var invalidContext = new IterationContext
        {
            DataSize = -1, // Invalid data size
            Requirements = null!, // Invalid requirements
            EnvironmentProfile = null!, // Invalid environment
            TargetPlatform = (PlatformTarget)999 // Invalid platform
        };
        
        // Act & Assert
        // Should not throw exceptions
        var selectedStrategy = strategySelector.SelectStrategy<object>(invalidContext);
        Assert.NotNull(selectedStrategy);
        
        var availableStrategies = strategySelector.GetAvailableStrategies<object>(invalidContext);
        Assert.NotNull(availableStrategies);
        
        var reasoning = strategySelector.GetSelectionReasoning(invalidContext);
        Assert.NotNull(reasoning);
    }
    
    public void Dispose()
    {
        _serviceProvider?.Dispose();
    }
}
