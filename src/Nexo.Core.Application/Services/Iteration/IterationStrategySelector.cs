using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Models.Iteration;
using Nexo.Core.Application.Services.Iteration.Strategies;
using Nexo.Core.Domain.Entities.Iteration;

namespace Nexo.Core.Application.Services.Iteration;

/// <summary>
/// Selects optimal iteration strategy based on runtime environment and requirements
/// </summary>
public interface IIterationStrategySelector
{
    IIterationStrategy<T> SelectStrategy<T>(IterationContext context);
    IIterationStrategy<T> SelectStrategy<T>(IEnumerable<T> source, IterationRequirements requirements);
    void RegisterStrategy<T>(IIterationStrategy<T> strategy);
    void SetEnvironmentProfile(RuntimeEnvironmentProfile profile);
}

public class IterationStrategySelector : IIterationStrategySelector
{
    private readonly Dictionary<string, object> _strategies = new();
    private RuntimeEnvironmentProfile _environmentProfile;
    private readonly ILogger<IterationStrategySelector> _logger;
    
    public IterationStrategySelector(ILogger<IterationStrategySelector> logger)
    {
        _logger = logger;
        _environmentProfile = RuntimeEnvironmentDetector.DetectCurrent();
        RegisterDefaultStrategies();
    }
    
    public IIterationStrategy<T> SelectStrategy<T>(IterationContext context)
    {
        var strategies = GetStrategiesForType<T>();
        
        if (!strategies.Any())
        {
            _logger.LogWarning("No compatible strategies found for type {Type}", typeof(T).Name);
            return new ForeachStrategy<T>(); // Fallback
        }
        
        var scoredStrategies = strategies
            .Select(strategy => new
            {
                Strategy = strategy,
                Score = CalculateStrategyScore(strategy as IIterationStrategy<object>, context)
            })
            .OrderByDescending(x => x.Score)
            .ToList();
        
        var selected = scoredStrategies.First().Strategy;
        
        _logger.LogDebug("Selected iteration strategy {StrategyId} for context {Context}", 
            selected.StrategyId, context);
        
        return selected;
    }
    
    public IIterationStrategy<T> SelectStrategy<T>(IEnumerable<T> source, IterationRequirements requirements)
    {
        var context = new IterationContext
        {
            DataSize = EstimateDataSize(source),
            Requirements = requirements,
            EnvironmentProfile = _environmentProfile
        };
        
        return SelectStrategy<T>(context);
    }
    
    private double CalculateStrategyScore(IIterationStrategy<object> strategy, IterationContext context)
    {
        double score = 0;
        
        // Platform compatibility
        if (strategy.PlatformCompatibility.HasFlag(context.EnvironmentProfile.PlatformType))
            score += 100;
        else
            return 0; // Incompatible platform
        
        // Data size optimization
        var profile = strategy.PerformanceProfile;
        if (context.DataSize >= profile.OptimalDataSizeMin && 
            context.DataSize <= profile.OptimalDataSizeMax)
        {
            score += 50;
        }
        
        // Performance requirements
        score += context.Requirements.PrioritizeCpu ? (int)profile.CpuEfficiency * 10 : 0;
        score += context.Requirements.PrioritizeMemory ? (int)profile.MemoryEfficiency * 10 : 0;
        score += context.Requirements.RequiresParallelization && profile.SupportsParallelization ? 30 : 0;
        
        // Environment characteristics
        score += _environmentProfile.CpuCores > 4 && profile.SupportsParallelization ? 20 : 0;
        score += _environmentProfile.AvailableMemoryMB < 1024 && profile.MemoryEfficiency == PerformanceLevel.Excellent ? 15 : 0;
        
        // Debug mode considerations
        if (_environmentProfile.IsDebugMode && strategy.StrategyId == "Linq")
        {
            score += 10; // Prefer readable code in debug mode
        }
        
        return score;
    }
    
    private void RegisterDefaultStrategies()
    {
        RegisterStrategy(new ForLoopStrategy<object>());
        RegisterStrategy(new ForeachStrategy<object>());
        RegisterStrategy(new LinqStrategy<object>());
        RegisterStrategy(new ParallelLinqStrategy<object>());
        
        // Platform-specific strategies
        if (_environmentProfile.PlatformType.HasFlag(PlatformCompatibility.Unity))
        {
            RegisterStrategy(new UnityOptimizedStrategy<object>());
        }
        
        if (_environmentProfile.PlatformType.HasFlag(PlatformCompatibility.WebAssembly))
        {
            RegisterStrategy(new WasmOptimizedStrategy<object>());
        }
    }
    
    public void RegisterStrategy<T>(IIterationStrategy<T> strategy)
    {
        var key = $"{typeof(T).Name}_{strategy.StrategyId}";
        _strategies[key] = strategy;
    }
    
    public void SetEnvironmentProfile(RuntimeEnvironmentProfile profile)
    {
        _environmentProfile = profile;
    }
    
    private IEnumerable<IIterationStrategy<T>> GetStrategiesForType<T>()
    {
        return _strategies.Values
            .OfType<IIterationStrategy<T>>()
            .Where(s => s.PlatformCompatibility.HasFlag(_environmentProfile.PlatformType));
    }
    
    private int EstimateDataSize<T>(IEnumerable<T> source)
    {
        return source switch
        {
            IList<T> list => list.Count,
            ICollection<T> collection => collection.Count,
            _ => source.Take(1000).Count() // Sample for estimation
        };
    }
}
