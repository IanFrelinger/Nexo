using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Models.Iteration;
using Nexo.Core.Application.Services.Iteration;
using Nexo.Core.Application.Services.Iteration.Strategies;
using Nexo.Core.Domain.Entities.Iteration;
using Nexo.Feature.AI.Services;

namespace Nexo.Examples;

/// <summary>
/// Comprehensive demonstration of the Iteration Strategy Pattern system
/// </summary>
public class IterationStrategyDemo
{
    public static async Task RunDemo()
    {
        Console.WriteLine("=== Nexo Iteration Strategy Pattern Demo ===\n");
        
        // Setup dependency injection
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole());
        services.AddIterationStrategies();
        
        var serviceProvider = services.BuildServiceProvider();
        
        // Get services
        var strategySelector = serviceProvider.GetRequiredService<IIterationStrategySelector>();
        var codeGenerator = serviceProvider.GetRequiredService<IIterationCodeGenerator>();
        var logger = serviceProvider.GetRequiredService<ILogger<IterationStrategyDemo>>();
        
        // Demo 1: Environment Analysis
        await DemoEnvironmentAnalysis(strategySelector);
        
        // Demo 2: Strategy Selection
        await DemoStrategySelection(strategySelector);
        
        // Demo 3: Performance Benchmarking
        await DemoPerformanceBenchmarking();
        
        // Demo 4: Code Generation
        await DemoCodeGeneration(codeGenerator);
        
        // Demo 5: Multi-Platform Code Generation
        await DemoMultiPlatformGeneration(codeGenerator);
        
        // Demo 6: Real-world Usage Scenarios
        await DemoRealWorldScenarios(strategySelector);
        
        Console.WriteLine("\n=== Demo Complete ===");
    }
    
    private static async Task DemoEnvironmentAnalysis(IIterationStrategySelector selector)
    {
        Console.WriteLine("1. Environment Analysis");
        Console.WriteLine("======================");
        
        var profile = RuntimeEnvironmentDetector.DetectCurrent();
        
        Console.WriteLine($"Platform: {profile.PlatformType}");
        Console.WriteLine($"CPU Cores: {profile.CpuCores}");
        Console.WriteLine($"Available Memory: {profile.AvailableMemoryMB} MB");
        Console.WriteLine($"Debug Mode: {profile.IsDebugMode}");
        Console.WriteLine($"Framework: {profile.FrameworkVersion}");
        Console.WriteLine($"Optimization Level: {profile.OptimizationLevel}");
        
        // Show strategy recommendations for different scenarios
        var scenarios = new[]
        {
            ("Small Dataset (100 items)", new IterationContext { DataSize = 100, EnvironmentProfile = profile }),
            ("Medium Dataset (10,000 items)", new IterationContext { DataSize = 10000, EnvironmentProfile = profile }),
            ("Large Dataset (1,000,000 items)", new IterationContext { DataSize = 1000000, EnvironmentProfile = profile }),
            ("CPU-Intensive Task", new IterationContext { DataSize = 10000, Requirements = new IterationRequirements { PrioritizeCpu = true }, EnvironmentProfile = profile }),
            ("Memory-Conscious Task", new IterationContext { DataSize = 10000, Requirements = new IterationRequirements { PrioritizeMemory = true }, EnvironmentProfile = profile }),
            ("Parallel Processing", new IterationContext { DataSize = 10000, Requirements = new IterationRequirements { RequiresParallelization = true }, EnvironmentProfile = profile })
        };
        
        Console.WriteLine("\nRecommended Strategies:");
        foreach (var (scenario, context) in scenarios)
        {
            var strategy = selector.SelectStrategy<object>(context);
            Console.WriteLine($"  {scenario}: {strategy.StrategyId}");
        }
        
        Console.WriteLine();
    }
    
    private static async Task DemoStrategySelection(IIterationStrategySelector selector)
    {
        Console.WriteLine("2. Strategy Selection Examples");
        Console.WriteLine("==============================");
        
        var testData = Enumerable.Range(1, 1000).ToList();
        
        // Example 1: Small dataset
        var smallContext = new IterationContext
        {
            DataSize = 100,
            EnvironmentProfile = RuntimeEnvironmentDetector.DetectCurrent()
        };
        var smallStrategy = selector.SelectStrategy<int>(smallContext);
        Console.WriteLine($"Small dataset (100 items): {smallStrategy.StrategyId}");
        
        // Example 2: Large dataset with CPU priority
        var largeContext = new IterationContext
        {
            DataSize = 100000,
            Requirements = new IterationRequirements { PrioritizeCpu = true },
            EnvironmentProfile = RuntimeEnvironmentDetector.DetectCurrent()
        };
        var largeStrategy = selector.SelectStrategy<int>(largeContext);
        Console.WriteLine($"Large dataset with CPU priority: {largeStrategy.StrategyId}");
        
        // Example 3: Memory-conscious processing
        var memoryContext = new IterationContext
        {
            DataSize = 50000,
            Requirements = new IterationRequirements { PrioritizeMemory = true },
            EnvironmentProfile = RuntimeEnvironmentDetector.DetectCurrent()
        };
        var memoryStrategy = selector.SelectStrategy<int>(memoryContext);
        Console.WriteLine($"Memory-conscious processing: {memoryStrategy.StrategyId}");
        
        Console.WriteLine();
    }
    
    private static async Task DemoPerformanceBenchmarking()
    {
        Console.WriteLine("3. Performance Benchmarking");
        Console.WriteLine("===========================");
        
        var dataSizes = new[] { 1000, 10000, 100000 };
        var strategies = new IIterationStrategy<int>[]
        {
            new ForLoopStrategy<int>(),
            new ForeachStrategy<int>(),
            new LinqStrategy<int>(),
            new ParallelLinqStrategy<int>()
        };
        
        foreach (var dataSize in dataSizes)
        {
            Console.WriteLine($"\nData Size: {dataSize:N0} items");
            Console.WriteLine("Strategy\t\tTime (ms)\t\tRelative");
            
            var data = Enumerable.Range(1, dataSize).ToList();
            var results = new List<(string Strategy, long Milliseconds)>();
            
            foreach (var strategy in strategies)
            {
                try
                {
                    var stopwatch = Stopwatch.StartNew();
                    strategy.Execute(data, x => Math.Sqrt(x));
                    stopwatch.Stop();
                    
                    results.Add((strategy.StrategyId, stopwatch.ElapsedMilliseconds));
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"{strategy.StrategyId}\t\tERROR\t\t\t{ex.Message}");
                }
            }
            
            if (results.Any())
            {
                var fastest = results.Min(r => r.Milliseconds);
                foreach (var (strategy, time) in results.OrderBy(r => r.Milliseconds))
                {
                    var relative = fastest > 0 ? (double)time / fastest : 1.0;
                    Console.WriteLine($"{strategy}\t\t{time}\t\t\t{relative:F2}x");
                }
            }
        }
        
        Console.WriteLine();
    }
    
    private static async Task DemoCodeGeneration(IIterationCodeGenerator generator)
    {
        Console.WriteLine("4. Code Generation Examples");
        Console.WriteLine("===========================");
        
        var platforms = new[] { PlatformTarget.CSharp, PlatformTarget.JavaScript, PlatformTarget.Python, PlatformTarget.Swift };
        
        foreach (var platform in platforms)
        {
            Console.WriteLine($"\n{platform} Code Generation:");
            Console.WriteLine("-" * 30);
            
            var request = new IterationCodeRequest
            {
                Context = new IterationContext
                {
                    DataSize = 1000,
                    EnvironmentProfile = RuntimeEnvironmentDetector.DetectCurrent()
                },
                CodeGeneration = new CodeGenerationContext
                {
                    PlatformTarget = platform,
                    CollectionName = "users",
                    IterationBodyTemplate = "ProcessUser({item});"
                },
                UseAIEnhancement = false // Use base generation for demo
            };
            
            try
            {
                var code = await generator.GenerateOptimalIterationCodeAsync(request);
                Console.WriteLine(code);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }
        
        Console.WriteLine();
    }
    
    private static async Task DemoMultiPlatformGeneration(IIterationCodeGenerator generator)
    {
        Console.WriteLine("5. Multi-Platform Code Generation");
        Console.WriteLine("==================================");
        
        var request = new IterationCodeRequest
        {
            Context = new IterationContext
            {
                DataSize = 10000,
                Requirements = new IterationRequirements { PrioritizeCpu = true },
                EnvironmentProfile = RuntimeEnvironmentDetector.DetectCurrent()
            },
            CodeGeneration = new CodeGenerationContext
            {
                CollectionName = "products",
                IterationBodyTemplate = "ValidateProduct({item});",
                HasWhere = true,
                PredicateTemplate = "p => p.IsActive"
            },
            UseAIEnhancement = false,
            TargetPlatforms = new[] { PlatformTarget.CSharp, PlatformTarget.JavaScript, PlatformTarget.Python }
        };
        
        try
        {
            var codes = await generator.GenerateMultiplePlatformIterationsAsync(request);
            
            foreach (var code in codes)
            {
                Console.WriteLine(code);
                Console.WriteLine();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }
    
    private static async Task DemoRealWorldScenarios(IIterationStrategySelector selector)
    {
        Console.WriteLine("6. Real-World Usage Scenarios");
        Console.WriteLine("=============================");
        
        // Scenario 1: Data Processing Pipeline
        Console.WriteLine("\nScenario 1: Data Processing Pipeline");
        var processingData = Enumerable.Range(1, 50000).Select(i => new { Id = i, Value = i * 2.5, IsValid = i % 3 == 0 }).ToList();
        
        var processingContext = new IterationContext
        {
            DataSize = processingData.Count,
            Requirements = new IterationRequirements { PrioritizeCpu = true },
            EnvironmentProfile = RuntimeEnvironmentDetector.DetectCurrent()
        };
        
        var processingStrategy = selector.SelectStrategy<object>(processingContext);
        Console.WriteLine($"Selected strategy: {processingStrategy.StrategyId}");
        
        var stopwatch = Stopwatch.StartNew();
        var validItems = processingStrategy.ExecuteWhere(processingData, 
            item => ((dynamic)item).IsValid, 
            item => ((dynamic)item).Value);
        var result = validItems.Sum();
        stopwatch.Stop();
        
        Console.WriteLine($"Processed {processingData.Count} items, found {validItems.Count()} valid items");
        Console.WriteLine($"Sum of valid values: {result:F2}");
        Console.WriteLine($"Processing time: {stopwatch.ElapsedMilliseconds}ms");
        
        // Scenario 2: UI Element Processing (Unity-like)
        Console.WriteLine("\nScenario 2: UI Element Processing");
        var uiElements = Enumerable.Range(1, 1000).Select(i => new { Id = i, Visible = i % 2 == 0, X = i * 10, Y = i * 5 }).ToList();
        
        var uiContext = new IterationContext
        {
            DataSize = uiElements.Count,
            Requirements = new IterationRequirements { PrioritizeMemory = true },
            EnvironmentProfile = RuntimeEnvironmentDetector.DetectCurrent()
        };
        
        var uiStrategy = selector.SelectStrategy<object>(uiContext);
        Console.WriteLine($"Selected strategy: {uiStrategy.StrategyId}");
        
        stopwatch.Restart();
        uiStrategy.Execute(uiElements, element => 
        {
            // Simulate UI processing
            var el = (dynamic)element;
            if (el.Visible)
            {
                // Process visible element
                var _ = el.X + el.Y;
            }
        });
        stopwatch.Stop();
        
        Console.WriteLine($"Processed {uiElements.Count} UI elements");
        Console.WriteLine($"Processing time: {stopwatch.ElapsedMilliseconds}ms");
        
        // Scenario 3: Async Data Loading
        Console.WriteLine("\nScenario 3: Async Data Loading");
        var asyncData = Enumerable.Range(1, 100).ToList();
        
        var asyncContext = new IterationContext
        {
            DataSize = asyncData.Count,
            EnvironmentProfile = RuntimeEnvironmentDetector.DetectCurrent()
        };
        
        var asyncStrategy = selector.SelectStrategy<int>(asyncContext);
        Console.WriteLine($"Selected strategy: {asyncStrategy.StrategyId}");
        
        stopwatch.Restart();
        await asyncStrategy.ExecuteAsync(asyncData, async item =>
        {
            // Simulate async operation
            await Task.Delay(1);
            var _ = Math.Sqrt(item);
        });
        stopwatch.Stop();
        
        Console.WriteLine($"Processed {asyncData.Count} items asynchronously");
        Console.WriteLine($"Processing time: {stopwatch.ElapsedMilliseconds}ms");
        
        Console.WriteLine();
    }
}

/// <summary>
/// Extension method for string repetition
/// </summary>
public static class StringExtensions
{
    public static string Repeat(this string str, int count)
    {
        return string.Concat(Enumerable.Repeat(str, count));
    }
}
