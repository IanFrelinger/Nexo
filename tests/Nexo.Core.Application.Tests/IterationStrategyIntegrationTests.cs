using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Services.Iteration;
using Nexo.Core.Application.Services.Iteration.Strategies;
using Nexo.Core.Domain.Entities.Iteration;
using Nexo.Core.Domain.Entities.Infrastructure;
using Nexo.Core.Domain.Interfaces.Infrastructure;
// using Nexo.Feature.AI.Services; // Will be available when AI project is built
using Xunit;

namespace Nexo.Core.Application.Tests;

/// <summary>
/// Integration tests for the iteration strategy system
/// </summary>
public class IterationStrategyIntegrationTests
{
    private readonly IServiceProvider _serviceProvider;
    
    public IterationStrategyIntegrationTests()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddIterationStrategies();
        _serviceProvider = services.BuildServiceProvider();
    }
    
    [Fact]
    public void DependencyInjection_ShouldRegisterAllServices()
    {
        // Act & Assert
        var selector = _serviceProvider.GetRequiredService<IIterationStrategySelector>();
        // var codeGenerator = _serviceProvider.GetRequiredService<IIterationCodeGenerator>(); // Will be available when AI project is built
        var environmentProfile = _serviceProvider.GetRequiredService<RuntimeEnvironmentProfile>();
        
        Assert.NotNull(selector);
        // Assert.NotNull(codeGenerator);
        Assert.NotNull(environmentProfile);
    }
    
    [Fact]
    public void StrategySelector_ShouldWorkWithRealData()
    {
        // Arrange
        var selector = _serviceProvider.GetRequiredService<IIterationStrategySelector>();
        var testData = Enumerable.Range(1, 1000).Select(i => new TestDataItem 
        { 
            Id = i, 
            Value = i * 2.5, 
            IsValid = i % 3 == 0 
        }).ToList();
        
        // Act
        var strategy = selector.SelectStrategy<TestDataItem>(testData, new IterationRequirements());
        
        // Assert
        Assert.NotNull(strategy);
        
        // Test execution
        var results = new List<TestDataItem>();
        strategy.Execute(testData, item => results.Add(item));
        Assert.Equal(testData.Count, results.Count);
    }
    
    [Fact]
    public void StrategySelector_ShouldAdaptToDifferentScenarios()
    {
        // Arrange
        var selector = _serviceProvider.GetRequiredService<IIterationStrategySelector>();
        var environmentProfile = RuntimeEnvironmentDetector.DetectCurrent();
        
        var scenarios = new[]
        {
            new { Name = "Small Data", Context = new IterationContext { DataSize = 100, EnvironmentProfile = environmentProfile } },
            new { Name = "Medium Data", Context = new IterationContext { DataSize = 10000, EnvironmentProfile = environmentProfile } },
            new { Name = "Large Data", Context = new IterationContext { DataSize = 100000, EnvironmentProfile = environmentProfile } },
            new { Name = "CPU Priority", Context = new IterationContext { DataSize = 10000, Requirements = new IterationRequirements { PrioritizeCpu = true }, EnvironmentProfile = environmentProfile } },
            new { Name = "Memory Priority", Context = new IterationContext { DataSize = 10000, Requirements = new IterationRequirements { PrioritizeMemory = true }, EnvironmentProfile = environmentProfile } },
            new { Name = "Parallel Required", Context = new IterationContext { DataSize = 10000, Requirements = new IterationRequirements { RequiresParallelization = true }, EnvironmentProfile = environmentProfile } }
        };
        
        // Act & Assert
        foreach (var scenario in scenarios)
        {
            var strategy = selector.SelectStrategy<object>(scenario.Context);
            Assert.NotNull(strategy);
            Assert.NotEmpty(strategy.StrategyId);
        }
    }
    
    [Fact]
    public void CodeGeneration_ShouldWorkWithAllStrategies()
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
        
        var platforms = new[] { PlatformTarget.CSharp, PlatformTarget.JavaScript, PlatformTarget.Python, PlatformTarget.Swift };
        
        foreach (var strategy in strategies)
        {
            foreach (var platform in platforms)
            {
                // Act
                var context = new CodeGenerationContext
                {
                    PlatformTarget = platform,
                    CollectionName = "items",
                    ItemName = "item",
                    IterationBodyTemplate = "ProcessItem({item});"
                };
                
                var code = strategy.GenerateCode(context);
                
                // Assert
                Assert.NotNull(code);
                Assert.NotEmpty(code);
                Assert.Contains("items", code);
            }
        }
    }
    
    [Fact]
    public void CodeGenerator_ShouldGenerateCodeForMultiplePlatforms()
    {
        // This test will be available when AI project is built
        // For now, we'll test the strategy code generation directly
        
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
        
        var platforms = new[] { PlatformTarget.CSharp, PlatformTarget.JavaScript, PlatformTarget.Python };
        
        // Act & Assert
        foreach (var strategy in strategies)
        {
            foreach (var platform in platforms)
            {
                var context = new CodeGenerationContext
                {
                    PlatformTarget = platform,
                    CollectionName = "users",
                    IterationBodyTemplate = "ValidateUser({item});"
                };
                
                var code = strategy.GenerateCode(context);
                
                Assert.NotNull(code);
                Assert.NotEmpty(code);
                Assert.Contains("users", code);
            }
        }
    }
    
    [Fact]
    public void RuntimeEnvironmentDetector_ShouldProvideValidProfile()
    {
        // Act
        var profile = RuntimeEnvironmentDetector.DetectCurrent();
        
        // Assert
        Assert.NotNull(profile);
        Assert.True(profile.CpuCores > 0);
        Assert.True(profile.AvailableMemoryMB > 0);
        Assert.NotEmpty(profile.FrameworkVersion);
        Assert.True(Enum.IsDefined(typeof(OptimizationLevel), profile.OptimizationLevel));
        // In test environment, we expect DotNet platform
        Assert.Equal(PlatformType.DotNet, profile.PlatformType);
    }
    
    [Fact]
    public void StrategySelector_ShouldHandleCustomStrategies()
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
    public void Strategies_ShouldWorkWithComplexDataTypes()
    {
        // Arrange
        var strategies = new IIterationStrategy<ComplexDataItem>[]
        {
            new ForLoopStrategy<ComplexDataItem>(),
            new ForeachStrategy<ComplexDataItem>(),
            new LinqStrategy<ComplexDataItem>(),
            new UnityOptimizedStrategy<ComplexDataItem>(),
            new WasmOptimizedStrategy<ComplexDataItem>()
        };
        
        var complexData = Enumerable.Range(1, 100).Select(i => new ComplexDataItem
        {
            Id = i,
            Name = $"Item {i}",
            Values = Enumerable.Range(1, 10).Select(j => j * i).ToList(),
            Metadata = new Dictionary<string, object> { { "key", i }, { "type", "test" } }
        }).ToList();
        
        foreach (var strategy in strategies)
        {
            // Act
            var results = new List<ComplexDataItem>();
            strategy.Execute(complexData, item => results.Add(item));
            
            var transformed = strategy.Execute(complexData, item => new { item.Id, item.Name });
            
            var filtered = strategy.ExecuteWhere(complexData, 
                item => item.Id % 2 == 0, 
                item => item.Name.ToUpper());
            
            // Assert
            Assert.Equal(complexData.Count, results.Count);
            Assert.Equal(complexData.Count, transformed.Count());
            Assert.Equal(50, filtered.Count()); // Half should be even
        }
    }
    
    [Fact]
    public async Task AsyncStrategies_ShouldHandleConcurrentExecution()
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
        
        var data = Enumerable.Range(1, 10).ToList(); // Smaller dataset for more reliable testing
        
        // Act - Run multiple strategies concurrently
        var tasks = strategies.Select(async strategy =>
        {
            var results = new List<int>();
            var dataCopy = data.ToList(); // Each strategy gets its own copy
            await strategy.ExecuteAsync(dataCopy, async x =>
            {
                // Use a simple async operation that's more predictable
                await Task.Yield();
                results.Add(x * 2);
            });
            return results;
        });
        
        var results = await Task.WhenAll(tasks);
        
        // Assert
        Assert.Equal(strategies.Length, results.Length);
        foreach (var result in results)
        {
            Assert.Equal(data.Count, result.Count);
            // Sort both collections to avoid order dependency issues
            var expected = data.Select(x => x * 2).OrderBy(x => x).ToList();
            var actual = result.OrderBy(x => x).ToList();
            Assert.Equal(expected, actual);
        }
    }
    
    [Fact]
    public void StrategySelector_ShouldRespectPlatformCompatibility()
    {
        // Arrange
        var selector = _serviceProvider.GetRequiredService<IIterationStrategySelector>();
        var unityProfile = new RuntimeEnvironmentProfile
        {
            PlatformType = PlatformType.Unity,
            CpuCores = 4,
            AvailableMemoryMB = 1024,
            IsDebugMode = false,
            FrameworkVersion = "Unity 2023.1",
            OptimizationLevel = OptimizationLevel.Balanced
        };
        
        var wasmProfile = new RuntimeEnvironmentProfile
        {
            PlatformType = PlatformType.WebAssembly,
            CpuCores = 1,
            AvailableMemoryMB = 512,
            IsDebugMode = false,
            FrameworkVersion = "WebAssembly",
            OptimizationLevel = OptimizationLevel.Balanced
        };
        
        // Act
        selector.SetEnvironmentProfile(unityProfile);
        var unityStrategy = selector.SelectStrategy<object>(new IterationContext 
        { 
            DataSize = 1000, 
            EnvironmentProfile = unityProfile 
        });
        
        selector.SetEnvironmentProfile(wasmProfile);
        var wasmStrategy = selector.SelectStrategy<object>(new IterationContext 
        { 
            DataSize = 1000, 
            EnvironmentProfile = wasmProfile 
        });
        
        // Assert
        Assert.NotNull(unityStrategy);
        Assert.NotNull(wasmStrategy);
        
        // Unity strategy should be compatible with Unity
        Assert.True(unityStrategy.PlatformCompatibility.HasFlag(PlatformCompatibility.Unity));
        
        // WASM strategy should be compatible with WebAssembly
        Assert.True(wasmStrategy.PlatformCompatibility.HasFlag(PlatformCompatibility.WebAssembly));
    }
}

/// <summary>
/// Test data item for integration tests
/// </summary>
public class TestDataItem
{
    public int Id { get; set; }
    public double Value { get; set; }
    public bool IsValid { get; set; }
}

/// <summary>
/// Complex data item for testing complex scenarios
/// </summary>
public class ComplexDataItem
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public List<int> Values { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();
}
