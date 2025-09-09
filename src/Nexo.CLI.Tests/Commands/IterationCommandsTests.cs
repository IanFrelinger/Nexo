using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Nexo.CLI.Commands;
using Nexo.Core.Application.Services.Iteration;
using Nexo.Core.Domain.Entities.Iteration;

namespace Nexo.CLI.Tests.Commands;

/// <summary>
/// Tests for iteration CLI commands
/// </summary>
public class IterationCommandsTests
{
    private readonly Mock<IServiceProvider> _mockServiceProvider;
    private readonly Mock<ILogger<IterationCommands>> _mockLogger;
    private readonly Mock<IIterationStrategySelector> _mockStrategySelector;
    private readonly Mock<IIterationBenchmarker> _mockBenchmarker;
    private readonly Mock<IIterationCodeGenerator> _mockCodeGenerator;
    private readonly Mock<IIterationCodeOptimizer> _mockCodeOptimizer;
    private readonly IterationCommands _commands;
    
    public IterationCommandsTests()
    {
        _mockServiceProvider = new Mock<IServiceProvider>();
        _mockLogger = new Mock<ILogger<IterationCommands>>();
        _mockStrategySelector = new Mock<IIterationStrategySelector>();
        _mockBenchmarker = new Mock<IIterationBenchmarker>();
        _mockCodeGenerator = new Mock<IIterationCodeGenerator>();
        _mockCodeOptimizer = new Mock<IIterationCodeOptimizer>();
        
        _mockServiceProvider.Setup(x => x.GetRequiredService<IIterationStrategySelector>())
            .Returns(_mockStrategySelector.Object);
        _mockServiceProvider.Setup(x => x.GetRequiredService<IIterationBenchmarker>())
            .Returns(_mockBenchmarker.Object);
        _mockServiceProvider.Setup(x => x.GetRequiredService<IIterationCodeGenerator>())
            .Returns(_mockCodeGenerator.Object);
        _mockServiceProvider.Setup(x => x.GetRequiredService<IIterationCodeOptimizer>())
            .Returns(_mockCodeOptimizer.Object);
        
        _commands = new IterationCommands(_mockServiceProvider.Object, _mockLogger.Object);
    }
    
    [Fact]
    public void CreateIterationAnalyzeCommand_ShouldReturnValidCommand()
    {
        // Act
        var command = _commands.CreateIterationAnalyzeCommand();
        
        // Assert
        Assert.NotNull(command);
        Assert.Equal("analyze", command.Name);
        Assert.Equal("Analyze iteration environment and capabilities", command.Description);
    }
    
    [Fact]
    public void CreateIterationBenchmarkCommand_ShouldReturnValidCommand()
    {
        // Act
        var command = _commands.CreateIterationBenchmarkCommand();
        
        // Assert
        Assert.NotNull(command);
        Assert.Equal("benchmark", command.Name);
        Assert.Equal("Benchmark iteration strategies", command.Description);
    }
    
    [Fact]
    public void CreateIterationGenerateCommand_ShouldReturnValidCommand()
    {
        // Act
        var command = _commands.CreateIterationGenerateCommand();
        
        // Assert
        Assert.NotNull(command);
        Assert.Equal("generate", command.Name);
        Assert.Equal("Generate optimized iteration code", command.Description);
    }
    
    [Fact]
    public void CreateIterationOptimizeCommand_ShouldReturnValidCommand()
    {
        // Act
        var command = _commands.CreateIterationOptimizeCommand();
        
        // Assert
        Assert.NotNull(command);
        Assert.Equal("optimize", command.Name);
        Assert.Equal("Optimize existing iteration code", command.Description);
    }
    
    [Fact]
    public void CreateIterationRecommendationsCommand_ShouldReturnValidCommand()
    {
        // Act
        var command = _commands.CreateIterationRecommendationsCommand();
        
        // Assert
        Assert.NotNull(command);
        Assert.Equal("recommendations", command.Name);
        Assert.Equal("Get iteration strategy recommendations", command.Description);
    }
    
    [Fact]
    public async Task AnalyzeEnvironment_ShouldCallCorrectServices()
    {
        // Arrange
        var mockProfile = new RuntimeEnvironmentProfile
        {
            PlatformType = PlatformType.DotNet,
            CpuCores = 8,
            AvailableMemoryMB = 16384,
            IsConstrained = false,
            IsMobile = false,
            IsWeb = false,
            IsUnity = false
        };
        
        var mockRecommendations = new List<StrategyRecommendation>
        {
            new StrategyRecommendation
            {
                Scenario = "High-performance data processing",
                RecommendedStrategyId = "Nexo.ForLoop",
                Reasoning = "For-loop provides best performance for most scenarios",
                DataSizeRange = (0, 100000),
                PerformanceCharacteristics = "Excellent CPU and memory efficiency"
            }
        };
        
        _mockStrategySelector.Setup(x => x.GetRecommendations(It.IsAny<PlatformType>()))
            .Returns(mockRecommendations);
        
        // Act
        await _commands.AnalyzeEnvironment(false, "auto");
        
        // Assert
        _mockStrategySelector.Verify(x => x.GetRecommendations(It.IsAny<PlatformType>()), Times.Once);
    }
    
    [Fact]
    public async Task AnalyzeEnvironment_ShouldShowDetailedAnalysisWhenRequested()
    {
        // Arrange
        var mockProfile = new RuntimeEnvironmentProfile
        {
            PlatformType = PlatformType.DotNet,
            CpuCores = 8,
            AvailableMemoryMB = 16384
        };
        
        var mockRecommendations = new List<StrategyRecommendation>
        {
            new StrategyRecommendation
            {
                Scenario = "High-performance data processing",
                RecommendedStrategyId = "Nexo.ForLoop",
                Reasoning = "For-loop provides best performance",
                DataSizeRange = (0, 100000),
                PerformanceCharacteristics = "Excellent performance"
            }
        };
        
        var mockComparisonResults = new List<StrategyComparisonResult>
        {
            new StrategyComparisonResult
            {
                Strategy = new Mock<IIterationStrategy<object>>().Object,
                PerformanceEstimate = new PerformanceEstimate
                {
                    EstimatedExecutionTimeMs = 1.0,
                    EstimatedMemoryUsageMB = 0.1,
                    Confidence = 0.9,
                    PerformanceScore = 95.0,
                    MeetsRequirements = true
                },
                SuitabilityScore = 90.0,
                Reasoning = "Excellent performance for this context",
                IsRecommended = true
            }
        };
        
        _mockStrategySelector.Setup(x => x.GetRecommendations(It.IsAny<PlatformType>()))
            .Returns(mockRecommendations);
        _mockStrategySelector.Setup(x => x.CompareStrategies<object>(It.IsAny<IterationContext>()))
            .ReturnsAsync(mockComparisonResults);
        
        // Act
        await _commands.AnalyzeEnvironment(true, "auto");
        
        // Assert
        _mockStrategySelector.Verify(x => x.GetRecommendations(It.IsAny<PlatformType>()), Times.Once);
        _mockStrategySelector.Verify(x => x.CompareStrategies<object>(It.IsAny<IterationContext>()), Times.AtLeastOnce);
    }
    
    [Fact]
    public async Task BenchmarkStrategies_ShouldCallBenchmarker()
    {
        // Arrange
        var mockResults = new List<BenchmarkResult>
        {
            new BenchmarkResult
            {
                StrategyId = "Nexo.ForLoop",
                ExecutionTime = 1.5,
                MemoryUsageMB = 0.1,
                PerformanceScore = 95.0,
                Platform = "Current",
                IsRecommended = true
            },
            new BenchmarkResult
            {
                StrategyId = "Nexo.Foreach",
                ExecutionTime = 2.0,
                MemoryUsageMB = 0.2,
                PerformanceScore = 85.0,
                Platform = "Current",
                IsRecommended = false
            }
        };
        
        _mockBenchmarker.Setup(x => x.BenchmarkAllStrategies(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int>()))
            .ReturnsAsync(mockResults);
        
        // Act
        await _commands.BenchmarkStrategies(10000, "current", 5);
        
        // Assert
        _mockBenchmarker.Verify(x => x.BenchmarkAllStrategies(10000, "current", 5), Times.Once);
    }
    
    [Fact]
    public async Task GenerateOptimizedIteration_ShouldCallCodeGenerator()
    {
        // Arrange
        var mockCode = @"// Generated optimized iteration code
for (int i = 0; i < items.Count; i++)
{
    var item = items[i];
    // Process item
}";
        
        _mockCodeGenerator.Setup(x => x.GenerateOptimalIterationAsync(It.IsAny<IterationCodeRequest>()))
            .ReturnsAsync(mockCode);
        
        // Act
        await _commands.GenerateOptimizedIteration("Process items", "auto", 1000, null);
        
        // Assert
        _mockCodeGenerator.Verify(x => x.GenerateOptimalIterationAsync(It.IsAny<IterationCodeRequest>()), Times.Once);
    }
    
    [Fact]
    public async Task OptimizeIterationCode_ShouldCallCodeOptimizer()
    {
        // Arrange
        var mockResult = new IterationOptimizationResult
        {
            OptimizedCode = @"// Optimized iteration code
for (int i = 0; i < items.Count; i++)
{
    var item = items[i];
    // Optimized processing
}",
            OptimizationMetrics = new OptimizationMetrics
            {
                PerformanceImprovementPercentage = 25.0,
                MemoryImprovementPercentage = 15.0,
                OptimizationScore = 20.0
            },
            SelectedStrategy = new Mock<IIterationStrategy<object>>().Object
        };
        
        _mockCodeOptimizer.Setup(x => x.OptimizeIterationCodeAsync(It.IsAny<IterationOptimizationRequest>()))
            .ReturnsAsync(mockResult);
        
        // Act
        await _commands.OptimizeIterationCode("test-input.cs", "auto", null);
        
        // Assert
        _mockCodeOptimizer.Verify(x => x.OptimizeIterationCodeAsync(It.IsAny<IterationOptimizationRequest>()), Times.Once);
    }
    
    [Fact]
    public async Task ShowRecommendations_ShouldCallStrategySelector()
    {
        // Arrange
        var mockRecommendations = new List<StrategyRecommendation>
        {
            new StrategyRecommendation
            {
                Scenario = "Unity game development",
                RecommendedStrategyId = "Nexo.UnityOptimized",
                Reasoning = "Optimized for Unity's performance characteristics",
                DataSizeRange = (0, 10000),
                PerformanceCharacteristics = "Excellent for real-time scenarios"
            }
        };
        
        _mockStrategySelector.Setup(x => x.GetRecommendations(It.IsAny<PlatformType>()))
            .Returns(mockRecommendations);
        
        // Act
        await _commands.ShowRecommendations("unity");
        
        // Assert
        _mockStrategySelector.Verify(x => x.GetRecommendations(PlatformType.Unity), Times.Once);
    }
    
    [Fact]
    public void ParsePlatform_ShouldReturnCorrectPlatformTarget()
    {
        // Act & Assert
        Assert.Equal(PlatformTarget.DotNet, _commands.ParsePlatform("auto"));
        Assert.Equal(PlatformTarget.DotNet, _commands.ParsePlatform("dotnet"));
        Assert.Equal(PlatformTarget.Unity2023, _commands.ParsePlatform("unity"));
        Assert.Equal(PlatformTarget.JavaScript, _commands.ParsePlatform("web"));
        Assert.Equal(PlatformTarget.Swift, _commands.ParsePlatform("mobile"));
        Assert.Equal(PlatformTarget.Server, _commands.ParsePlatform("server"));
        Assert.Equal(PlatformTarget.DotNet, _commands.ParsePlatform("unknown"));
    }
    
    [Fact]
    public void ParsePlatformType_ShouldReturnCorrectPlatformType()
    {
        // Act & Assert
        Assert.Equal(PlatformType.DotNet, _commands.ParsePlatformType("auto"));
        Assert.Equal(PlatformType.DotNet, _commands.ParsePlatformType("dotnet"));
        Assert.Equal(PlatformType.Unity, _commands.ParsePlatformType("unity"));
        Assert.Equal(PlatformType.Web, _commands.ParsePlatformType("web"));
        Assert.Equal(PlatformType.Mobile, _commands.ParsePlatformType("mobile"));
        Assert.Equal(PlatformType.Server, _commands.ParsePlatformType("server"));
        Assert.Equal(PlatformType.DotNet, _commands.ParsePlatformType("unknown"));
    }
    
    [Fact]
    public async Task AnalyzeEnvironment_ShouldHandleErrorsGracefully()
    {
        // Arrange
        _mockStrategySelector.Setup(x => x.GetRecommendations(It.IsAny<PlatformType>()))
            .Throws(new Exception("Service unavailable"));
        
        // Act & Assert
        // Should not throw exception
        await _commands.AnalyzeEnvironment(false, "auto");
    }
    
    [Fact]
    public async Task BenchmarkStrategies_ShouldHandleErrorsGracefully()
    {
        // Arrange
        _mockBenchmarker.Setup(x => x.BenchmarkAllStrategies(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int>()))
            .ThrowsAsync(new Exception("Benchmark failed"));
        
        // Act & Assert
        // Should not throw exception
        await _commands.BenchmarkStrategies(10000, "current", 5);
    }
    
    [Fact]
    public async Task GenerateOptimizedIteration_ShouldHandleErrorsGracefully()
    {
        // Arrange
        _mockCodeGenerator.Setup(x => x.GenerateOptimalIterationAsync(It.IsAny<IterationCodeRequest>()))
            .ThrowsAsync(new Exception("Code generation failed"));
        
        // Act & Assert
        // Should not throw exception
        await _commands.GenerateOptimizedIteration("Process items", "auto", 1000, null);
    }
    
    [Fact]
    public async Task OptimizeIterationCode_ShouldHandleErrorsGracefully()
    {
        // Arrange
        _mockCodeOptimizer.Setup(x => x.OptimizeIterationCodeAsync(It.IsAny<IterationOptimizationRequest>()))
            .ThrowsAsync(new Exception("Optimization failed"));
        
        // Act & Assert
        // Should not throw exception
        await _commands.OptimizeIterationCode("test-input.cs", "auto", null);
    }
    
    [Fact]
    public async Task ShowRecommendations_ShouldHandleErrorsGracefully()
    {
        // Arrange
        _mockStrategySelector.Setup(x => x.GetRecommendations(It.IsAny<PlatformType>()))
            .Throws(new Exception("Recommendations unavailable"));
        
        // Act & Assert
        // Should not throw exception
        await _commands.ShowRecommendations("unity");
    }
}
