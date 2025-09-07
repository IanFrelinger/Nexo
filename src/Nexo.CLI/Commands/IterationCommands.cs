using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Models.Iteration;
using Nexo.Core.Application.Services.Iteration;
using Nexo.Core.Application.Services.Iteration.Strategies;
using Nexo.Core.Domain.Entities.Iteration;
using Nexo.Feature.AI.Services;

namespace Nexo.CLI.Commands;

/// <summary>
/// CLI commands for testing and demonstrating iteration strategies
/// </summary>
[Command("iteration")]
public class IterationCommands
{
    private readonly IIterationStrategySelector _strategySelector;
    private readonly IIterationCodeGenerator _codeGenerator;
    private readonly ILogger<IterationCommands> _logger;
    
    public IterationCommands(
        IIterationStrategySelector strategySelector,
        IIterationCodeGenerator codeGenerator,
        ILogger<IterationCommands> logger)
    {
        _strategySelector = strategySelector;
        _codeGenerator = codeGenerator;
        _logger = logger;
    }
    
    [Command("benchmark")]
    public async Task BenchmarkStrategies(
        [Option] int dataSize = 10000,
        [Option] bool parallel = false,
        [Option] string platform = "dotnet")
    {
        Console.WriteLine($"Benchmarking iteration strategies for {dataSize} items...");
        
        var data = Enumerable.Range(1, dataSize).ToList();
        var requirements = new IterationRequirements
        {
            RequiresParallelization = parallel
        };
        
        // Test each strategy
        var strategies = new IIterationStrategy<int>[]
        {
            new ForLoopStrategy<int>(),
            new ForeachStrategy<int>(),
            new LinqStrategy<int>(),
            new ParallelLinqStrategy<int>()
        };
        
        var results = new List<(string Strategy, long Milliseconds)>();
        
        foreach (var strategy in strategies)
        {
            try
            {
                var stopwatch = Stopwatch.StartNew();
                
                strategy.Execute(data, x => Math.Sqrt(x));
                
                stopwatch.Stop();
                results.Add((strategy.StrategyId, stopwatch.ElapsedMilliseconds));
                Console.WriteLine($"{strategy.StrategyId}: {stopwatch.ElapsedMilliseconds}ms");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{strategy.StrategyId}: ERROR - {ex.Message}");
            }
        }
        
        // Show optimal selection
        var context = new IterationContext
        {
            DataSize = dataSize,
            Requirements = requirements,
            EnvironmentProfile = RuntimeEnvironmentDetector.DetectCurrent()
        };
        
        var optimal = _strategySelector.SelectStrategy<int>(context);
        Console.WriteLine($"\nOptimal strategy: {optimal.StrategyId}");
        
        // Show performance comparison
        if (results.Any())
        {
            var fastest = results.OrderBy(r => r.Milliseconds).First();
            var slowest = results.OrderByDescending(r => r.Milliseconds).First();
            var speedup = (double)slowest.Milliseconds / fastest.Milliseconds;
            
            Console.WriteLine($"\nPerformance Analysis:");
            Console.WriteLine($"Fastest: {fastest.Strategy} ({fastest.Milliseconds}ms)");
            Console.WriteLine($"Slowest: {slowest.Strategy} ({slowest.Milliseconds}ms)");
            Console.WriteLine($"Speedup: {speedup:F2}x");
        }
    }
    
    [Command("generate")]
    public async Task GenerateIterationCode(
        [Option] string platform = "csharp",
        [Option] string collection = "items",
        [Option] string action = "ProcessItem({item})",
        [Option] bool aiEnhance = true)
    {
        try
        {
            var platformTarget = Enum.Parse<PlatformTarget>(platform, true);
            
            var request = new IterationCodeRequest
            {
                Context = new IterationContext
                {
                    DataSize = 1000,
                    Requirements = new IterationRequirements(),
                    EnvironmentProfile = RuntimeEnvironmentDetector.DetectCurrent()
                },
                CodeGeneration = new CodeGenerationContext
                {
                    PlatformTarget = platformTarget,
                    CollectionName = collection,
                    IterationBodyTemplate = action
                },
                UseAIEnhancement = aiEnhance
            };
            
            var code = await _codeGenerator.GenerateOptimalIterationCodeAsync(request);
            
            Console.WriteLine($"Generated {platform} iteration code:");
            Console.WriteLine("=" * 50);
            Console.WriteLine(code);
            Console.WriteLine("=" * 50);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error generating code: {ex.Message}");
            _logger.LogError(ex, "Error in generate command");
        }
    }
    
    [Command("analyze")]
    public async Task AnalyzeEnvironment()
    {
        try
        {
            var profile = RuntimeEnvironmentDetector.DetectCurrent();
            
            Console.WriteLine("Runtime Environment Analysis:");
            Console.WriteLine("=" * 40);
            Console.WriteLine($"Platform: {profile.PlatformType}");
            Console.WriteLine($"CPU Cores: {profile.CpuCores}");
            Console.WriteLine($"Available Memory: {profile.AvailableMemoryMB} MB");
            Console.WriteLine($"Debug Mode: {profile.IsDebugMode}");
            Console.WriteLine($"Framework: {profile.FrameworkVersion}");
            Console.WriteLine($"Optimization: {profile.OptimizationLevel}");
            
            // Show recommended strategies for different scenarios
            var scenarios = new[]
            {
                ("Small Dataset (100 items)", new IterationContext { DataSize = 100, EnvironmentProfile = profile }),
                ("Medium Dataset (10,000 items)", new IterationContext { DataSize = 10000, EnvironmentProfile = profile }),
                ("Large Dataset (1,000,000 items)", new IterationContext { DataSize = 1000000, EnvironmentProfile = profile }),
                ("CPU-Intensive", new IterationContext { DataSize = 10000, Requirements = new IterationRequirements { PrioritizeCpu = true }, EnvironmentProfile = profile }),
                ("Memory-Conscious", new IterationContext { DataSize = 10000, Requirements = new IterationRequirements { PrioritizeMemory = true }, EnvironmentProfile = profile }),
                ("Parallel Processing", new IterationContext { DataSize = 10000, Requirements = new IterationRequirements { RequiresParallelization = true }, EnvironmentProfile = profile })
            };
            
            Console.WriteLine("\nRecommended Strategies:");
            Console.WriteLine("=" * 40);
            
            foreach (var (scenario, context) in scenarios)
            {
                var strategy = _strategySelector.SelectStrategy<object>(context);
                Console.WriteLine($"{scenario}: {strategy.StrategyId}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error analyzing environment: {ex.Message}");
            _logger.LogError(ex, "Error in analyze command");
        }
    }
    
    [Command("multi-platform")]
    public async Task GenerateMultiPlatformCode(
        [Option] string collection = "items",
        [Option] string action = "ProcessItem({item})",
        [Option] bool aiEnhance = true)
    {
        try
        {
            var platforms = new[] { PlatformTarget.CSharp, PlatformTarget.JavaScript, PlatformTarget.Python, PlatformTarget.Swift };
            
            var request = new IterationCodeRequest
            {
                Context = new IterationContext
                {
                    DataSize = 1000,
                    Requirements = new IterationRequirements(),
                    EnvironmentProfile = RuntimeEnvironmentDetector.DetectCurrent()
                },
                CodeGeneration = new CodeGenerationContext
                {
                    CollectionName = collection,
                    IterationBodyTemplate = action
                },
                UseAIEnhancement = aiEnhance,
                TargetPlatforms = platforms
            };
            
            var codes = await _codeGenerator.GenerateMultiplePlatformIterationsAsync(request);
            
            Console.WriteLine("Multi-Platform Iteration Code Generation:");
            Console.WriteLine("=" * 50);
            
            foreach (var code in codes)
            {
                Console.WriteLine(code);
                Console.WriteLine();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error generating multi-platform code: {ex.Message}");
            _logger.LogError(ex, "Error in multi-platform command");
        }
    }
    
    [Command("test-strategies")]
    public async Task TestAllStrategies()
    {
        try
        {
            Console.WriteLine("Testing all iteration strategies...");
            
            var testData = new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
            var strategies = new IIterationStrategy<int>[]
            {
                new ForLoopStrategy<int>(),
                new ForeachStrategy<int>(),
                new LinqStrategy<int>(),
                new ParallelLinqStrategy<int>(),
                new UnityOptimizedStrategy<int>(),
                new WasmOptimizedStrategy<int>()
            };
            
            foreach (var strategy in strategies)
            {
                Console.WriteLine($"\nTesting {strategy.StrategyId}:");
                Console.WriteLine($"Platform Compatibility: {strategy.PlatformCompatibility}");
                Console.WriteLine($"CPU Efficiency: {strategy.PerformanceProfile.CpuEfficiency}");
                Console.WriteLine($"Memory Efficiency: {strategy.PerformanceProfile.MemoryEfficiency}");
                Console.WriteLine($"Supports Parallelization: {strategy.PerformanceProfile.SupportsParallelization}");
                
                // Test basic execution
                var results = new List<int>();
                strategy.Execute(testData, x => results.Add(x * 2));
                Console.WriteLine($"Basic execution result: [{string.Join(", ", results)}]");
                
                // Test transformation
                var transformed = strategy.Execute(testData, x => x * 3);
                Console.WriteLine($"Transformation result: [{string.Join(", ", transformed)}]");
                
                // Test filtering and transformation
                var filtered = strategy.ExecuteWhere(testData, x => x % 2 == 0, x => x * 4);
                Console.WriteLine($"Filtered result: [{string.Join(", ", filtered)}]");
                
                // Test code generation
                var codeContext = new CodeGenerationContext
                {
                    PlatformTarget = PlatformTarget.CSharp,
                    CollectionName = "numbers",
                    IterationBodyTemplate = "Console.WriteLine({item});"
                };
                var generatedCode = strategy.GenerateCode(codeContext);
                Console.WriteLine($"Generated code:\n{generatedCode}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error testing strategies: {ex.Message}");
            _logger.LogError(ex, "Error in test-strategies command");
        }
    }
}

/// <summary>
/// Command attribute for CLI commands
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class CommandAttribute : Attribute
{
    public string Name { get; }
    
    public CommandAttribute(string name)
    {
        Name = name;
    }
}

/// <summary>
/// Option attribute for command parameters
/// </summary>
[AttributeUsage(AttributeTargets.Parameter)]
public class OptionAttribute : Attribute
{
    public string? Name { get; set; }
    public string? Description { get; set; }
}
