using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Services.Iteration;
using Nexo.Core.Application.Services.Iteration.Strategies;
using Nexo.Core.Domain.Entities.Iteration;
using Nexo.Feature.AI.Agents;
using Nexo.Feature.Pipeline.Commands.Iteration;
using Nexo.Feature.Pipeline.Behaviors.Iteration;
using Nexo.CLI.Commands;

namespace Nexo.Infrastructure.DependencyInjection;

/// <summary>
/// Extension methods for registering iteration strategy services
/// </summary>
public static class IterationServicesExtensions
{
    /// <summary>
    /// Add Nexo iteration strategies and related services
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddNexoIterationStrategies(this IServiceCollection services)
    {
        // Core services
        services.AddSingleton<IIterationStrategySelector, IterationStrategySelector>();
        services.AddSingleton<RuntimeEnvironmentProfile>(RuntimeEnvironmentDetector.DetectCurrent());
        
        // Strategy implementations
        services.AddTransient(typeof(NexoForLoopStrategy<>));
        services.AddTransient(typeof(NexoForeachStrategy<>));
        services.AddTransient(typeof(NexoLinqStrategy<>));
        services.AddTransient(typeof(NexoParallelLinqStrategy<>));
        services.AddTransient(typeof(NexoUnityOptimizedStrategy<>));
        
        // Register all strategies as IIterationStrategy<object>
        services.AddTransient<IIterationStrategy<object>>(provider =>
        {
            var strategies = new List<IIterationStrategy<object>>
            {
                provider.GetRequiredService<NexoForLoopStrategy<object>>(),
                provider.GetRequiredService<NexoForeachStrategy<object>>(),
                provider.GetRequiredService<NexoLinqStrategy<object>>(),
                provider.GetRequiredService<NexoParallelLinqStrategy<object>>(),
                provider.GetRequiredService<NexoUnityOptimizedStrategy<object>>()
            };
            
            // Return a composite strategy selector that can handle multiple strategies
            return new CompositeIterationStrategy(strategies);
        });
        
        // AI agent integration
        services.AddTransient<IterationOptimizationAgent>();
        services.AddTransient<PlatformIterationAgent>();
        
        // Pipeline integration
        services.AddTransient<SelectIterationStrategyCommand>();
        services.AddTransient<OptimizeIterationCommand>();
        services.AddTransient<IterationOptimizationBehavior>();
        
        // CLI tools
        services.AddTransient<IIterationBenchmarker, IterationBenchmarker>();
        services.AddTransient<IIterationCodeGenerator, IterationCodeGenerator>();
        services.AddTransient<IIterationCodeOptimizer, IterationCodeOptimizer>();
        
        // Configuration
        services.Configure<IterationConfiguration>(config =>
        {
            config.EnableAutoOptimization = true;
            config.DefaultOptimizationLevel = PerformanceLevel.Balanced;
            config.PlatformPreferences = new Dictionary<string, string>
            {
                { "Unity", "Nexo.UnityOptimized" },
                { "DotNet", "Nexo.ForLoop" },
                { "Web", "Nexo.ForLoop" },
                { "Mobile", "Nexo.ForLoop" },
                { "Server", "Nexo.ParallelLinq" }
            };
            config.BenchmarkSampleSize = 10000;
        });
        
        return services;
    }
    
    /// <summary>
    /// Add iteration optimization to pipeline builder
    /// </summary>
    /// <param name="builder">Pipeline builder</param>
    /// <returns>Pipeline builder for chaining</returns>
    public static IPipelineBuilder AddIterationOptimization(this IPipelineBuilder builder)
    {
        return builder.AddBehavior<IterationOptimizationBehavior>();
    }
}

/// <summary>
/// Composite iteration strategy that delegates to multiple strategies
/// </summary>
public class CompositeIterationStrategy : IIterationStrategy<object>
{
    private readonly IEnumerable<IIterationStrategy<object>> _strategies;
    
    public CompositeIterationStrategy(IEnumerable<IIterationStrategy<object>> strategies)
    {
        _strategies = strategies ?? throw new ArgumentNullException(nameof(strategies));
    }
    
    public string StrategyId => "Composite";
    
    public IterationPerformanceProfile PerformanceProfile => new()
    {
        CpuEfficiency = PerformanceLevel.High,
        MemoryEfficiency = PerformanceLevel.High,
        Scalability = PerformanceLevel.High,
        OptimalDataSizeMin = 0,
        OptimalDataSizeMax = int.MaxValue,
        SupportsParallelization = true,
        RequiresIList = false,
        SupportsAsync = true,
        SuitableForRealTime = true
    };
    
    public PlatformCompatibility PlatformCompatibility => PlatformCompatibility.All;
    
    public void Execute(IEnumerable<object> source, Action<object> action)
    {
        // Delegate to the first available strategy
        var strategy = _strategies.FirstOrDefault(s => s.CanHandle(CreateDefaultContext()));
        if (strategy != null)
        {
            strategy.Execute(source, action);
        }
        else
        {
            // Fallback to simple foreach
            foreach (var item in source)
            {
                action(item);
            }
        }
    }
    
    public IEnumerable<TResult> Execute<TResult>(IEnumerable<object> source, Func<object, TResult> transform)
    {
        // Delegate to the first available strategy
        var strategy = _strategies.FirstOrDefault(s => s.CanHandle(CreateDefaultContext()));
        if (strategy != null)
        {
            return strategy.Execute(source, transform);
        }
        else
        {
            // Fallback to simple LINQ
            return source.Select(transform);
        }
    }
    
    public Task ExecuteAsync(IEnumerable<object> source, Func<object, Task> asyncAction)
    {
        // Delegate to the first available strategy that supports async
        var strategy = _strategies.FirstOrDefault(s => s.CanHandle(CreateDefaultContext()) && s.PerformanceProfile.SupportsAsync);
        if (strategy != null)
        {
            return strategy.ExecuteAsync(source, asyncAction);
        }
        else
        {
            // Fallback to sequential async execution
            return Task.Run(async () =>
            {
                foreach (var item in source)
                {
                    await asyncAction(item);
                }
            });
        }
    }
    
    public string GenerateCode(CodeGenerationContext context)
    {
        // Delegate to the best strategy for the context
        var strategy = _strategies
            .Where(s => s.CanHandle(CreateDefaultContext()))
            .OrderByDescending(s => s.GetPriority(CreateDefaultContext()))
            .FirstOrDefault();
        
        if (strategy != null)
        {
            return strategy.GenerateCode(context);
        }
        else
        {
            // Fallback to basic for-loop
            return $@"// Basic for-loop iteration
for (int i = 0; i < {context.CollectionVariableName}.Count; i++)
{{
    var {context.ItemVariableName} = {context.CollectionVariableName}[i];
    {context.ActionCode}
}}";
        }
    }
    
    public bool CanHandle(PipelineContext context)
    {
        return _strategies.Any(s => s.CanHandle(context));
    }
    
    public int GetPriority(PipelineContext context)
    {
        return _strategies
            .Where(s => s.CanHandle(context))
            .Select(s => s.GetPriority(context))
            .DefaultIfEmpty(0)
            .Max();
    }
    
    public PerformanceEstimate EstimatePerformance(IterationContext context)
    {
        var strategy = _strategies
            .Where(s => s.CanHandle(CreateDefaultContext()))
            .OrderByDescending(s => s.GetPriority(CreateDefaultContext()))
            .FirstOrDefault();
        
        if (strategy != null)
        {
            return strategy.EstimatePerformance(context);
        }
        else
        {
            // Fallback estimate
            return new PerformanceEstimate
            {
                EstimatedExecutionTimeMs = context.DataSize * 0.002,
                EstimatedMemoryUsageMB = context.DataSize * 0.001,
                Confidence = 0.5,
                PerformanceScore = 50,
                MeetsRequirements = true
            };
        }
    }
    
    private PipelineContext CreateDefaultContext()
    {
        var context = new PipelineContext();
        context.SetProperty("EstimatedDataSize", 1000);
        context.SetProperty("TargetPlatform", PlatformTarget.DotNet);
        return context;
    }
}

/// <summary>
/// Iteration configuration
/// </summary>
public class IterationConfiguration
{
    /// <summary>
    /// Whether to enable automatic optimization
    /// </summary>
    public bool EnableAutoOptimization { get; set; } = true;
    
    /// <summary>
    /// Default optimization level
    /// </summary>
    public PerformanceLevel DefaultOptimizationLevel { get; set; } = PerformanceLevel.Balanced;
    
    /// <summary>
    /// Platform preferences for strategy selection
    /// </summary>
    public Dictionary<string, string> PlatformPreferences { get; set; } = new();
    
    /// <summary>
    /// Benchmark sample size
    /// </summary>
    public int BenchmarkSampleSize { get; set; } = 10000;
}

/// <summary>
/// Pipeline builder interface
/// </summary>
public interface IPipelineBuilder
{
    IPipelineBuilder AddBehavior<T>() where T : class;
}

/// <summary>
/// Basic implementation of iteration benchmarker
/// </summary>
public class IterationBenchmarker : IIterationBenchmarker
{
    private readonly IIterationStrategySelector _strategySelector;
    private readonly ILogger<IterationBenchmarker> _logger;
    
    public IterationBenchmarker(IIterationStrategySelector strategySelector, ILogger<IterationBenchmarker> logger)
    {
        _strategySelector = strategySelector;
        _logger = logger;
    }
    
    public async Task<IEnumerable<BenchmarkResult>> BenchmarkAllStrategies(int dataSize, string platform, int iterations)
    {
        var results = new List<BenchmarkResult>();
        
        // Create test data
        var testData = Enumerable.Range(0, dataSize).Cast<object>().ToList();
        
        // Get available strategies
        var context = new IterationContext
        {
            DataSize = dataSize,
            Requirements = new PerformanceRequirements(),
            EnvironmentProfile = RuntimeEnvironmentDetector.DetectCurrent(),
            TargetPlatform = ParsePlatform(platform)
        };
        
        var strategies = _strategySelector.GetAvailableStrategies<object>(context);
        
        foreach (var strategy in strategies)
        {
            var benchmarkResult = await BenchmarkStrategy(strategy, testData, iterations);
            results.Add(benchmarkResult);
        }
        
        return results;
    }
    
    private async Task<BenchmarkResult> BenchmarkStrategy(IIterationStrategy<object> strategy, List<object> testData, int iterations)
    {
        var executionTimes = new List<double>();
        var memoryUsages = new List<double>();
        
        for (int i = 0; i < iterations; i++)
        {
            var startTime = DateTime.UtcNow;
            var startMemory = GC.GetTotalMemory(false);
            
            // Execute the strategy
            strategy.Execute(testData, item => { /* Simple operation */ });
            
            var endTime = DateTime.UtcNow;
            var endMemory = GC.GetTotalMemory(false);
            
            executionTimes.Add((endTime - startTime).TotalMilliseconds);
            memoryUsages.Add((endMemory - startMemory) / 1024.0 / 1024.0); // Convert to MB
        }
        
        return new BenchmarkResult
        {
            StrategyId = strategy.StrategyId,
            ExecutionTime = executionTimes.Average(),
            MemoryUsageMB = memoryUsages.Average(),
            PerformanceScore = CalculatePerformanceScore(executionTimes.Average(), memoryUsages.Average()),
            Platform = "Current",
            IsRecommended = executionTimes.Average() < 100 // Simple recommendation logic
        };
    }
    
    private double CalculatePerformanceScore(double executionTime, double memoryUsage)
    {
        // Simple performance score calculation
        var timeScore = Math.Max(0, 100 - executionTime);
        var memoryScore = Math.Max(0, 100 - memoryUsage * 10);
        return (timeScore + memoryScore) / 2;
    }
    
    private PlatformTarget ParsePlatform(string platform)
    {
        return platform.ToLower() switch
        {
            "unity" => PlatformTarget.Unity2023,
            "web" => PlatformTarget.JavaScript,
            "mobile" => PlatformTarget.Swift,
            "server" => PlatformTarget.Server,
            _ => PlatformTarget.DotNet
        };
    }
}

/// <summary>
/// Basic implementation of iteration code generator
/// </summary>
public class IterationCodeGenerator : IIterationCodeGenerator
{
    private readonly IIterationStrategySelector _strategySelector;
    private readonly ILogger<IterationCodeGenerator> _logger;
    
    public IterationCodeGenerator(IIterationStrategySelector strategySelector, ILogger<IterationCodeGenerator> logger)
    {
        _strategySelector = strategySelector;
        _logger = logger;
    }
    
    public async Task<string> GenerateOptimalIterationAsync(IterationCodeRequest request)
    {
        var context = new IterationContext
        {
            DataSize = request.EstimatedDataSize,
            Requirements = new PerformanceRequirements(),
            EnvironmentProfile = RuntimeEnvironmentDetector.DetectCurrent(),
            TargetPlatform = request.TargetPlatform
        };
        
        var strategy = _strategySelector.SelectStrategy<object>(context);
        
        var codeGenerationContext = new CodeGenerationContext
        {
            PlatformTarget = request.TargetPlatform,
            CollectionVariableName = "items",
            ItemVariableName = "item",
            ActionCode = "// Process item",
            IncludeNullChecks = true,
            IncludeBoundsChecking = true,
            PerformanceRequirements = new PerformanceRequirements()
        };
        
        return strategy.GenerateCode(codeGenerationContext);
    }
}

/// <summary>
/// Basic implementation of iteration code optimizer
/// </summary>
public class IterationCodeOptimizer : IIterationCodeOptimizer
{
    private readonly IIterationStrategySelector _strategySelector;
    private readonly ILogger<IterationCodeOptimizer> _logger;
    
    public IterationCodeOptimizer(IIterationStrategySelector strategySelector, ILogger<IterationCodeOptimizer> logger)
    {
        _strategySelector = strategySelector;
        _logger = logger;
    }
    
    public async Task<IterationOptimizationResult> OptimizeIterationCodeAsync(IterationOptimizationRequest request)
    {
        var context = new IterationContext
        {
            DataSize = 1000, // Default estimate
            Requirements = request.Requirements,
            EnvironmentProfile = request.EnvironmentProfile,
            TargetPlatform = request.TargetPlatform
        };
        
        var strategy = _strategySelector.SelectStrategy<object>(context);
        
        var codeGenerationContext = new CodeGenerationContext
        {
            PlatformTarget = request.TargetPlatform,
            CollectionVariableName = "items",
            ItemVariableName = "item",
            ActionCode = "// Process item",
            IncludeNullChecks = true,
            IncludeBoundsChecking = true,
            PerformanceRequirements = request.Requirements
        };
        
        var optimizedCode = strategy.GenerateCode(codeGenerationContext);
        
        return new IterationOptimizationResult
        {
            OptimizedCode = optimizedCode,
            OptimizationMetrics = new OptimizationMetrics
            {
                PerformanceImprovementPercentage = 25.0, // Placeholder
                MemoryImprovementPercentage = 15.0, // Placeholder
                OptimizationScore = 20.0
            },
            SelectedStrategy = strategy
        };
    }
}
