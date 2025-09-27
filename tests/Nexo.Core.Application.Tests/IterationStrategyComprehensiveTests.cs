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
using Xunit;

namespace Nexo.Core.Application.Tests;

/// <summary>
/// Comprehensive tests for iteration strategies to achieve 100% coverage.
/// Split into Success/ErrorHandling/Cancellation categories.
/// </summary>
public partial class IterationStrategyComprehensiveTests
{
    private readonly IServiceProvider _serviceProvider;
    
    public IterationStrategyComprehensiveTests()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddIterationStrategies();
        _serviceProvider = services.BuildServiceProvider();
    }
}

/// <summary>
/// Test custom strategy for testing registration
/// </summary>
public class TestCustomStrategy<T> : IIterationStrategy<T>
{
    public string StrategyId => "TestCustom";
    public PlatformCompatibility PlatformCompatibility => PlatformCompatibility.DotNet;
    public IterationPerformanceProfile PerformanceProfile => new()
    {
        CpuEfficiency = PerformanceLevel.Excellent,
        MemoryEfficiency = PerformanceLevel.Excellent,
        Scalability = PerformanceLevel.Excellent,
        OptimalDataSizeMin = 1,
        OptimalDataSizeMax = 10000,
        SupportsParallelization = true,
        RequiresIList = false
    };
    
    public void Execute(IEnumerable<T> source, Action<T> action)
    {
        foreach (var item in source)
        {
            action(item);
        }
    }
    
    public IEnumerable<TResult> Execute<TResult>(IEnumerable<T> source, Func<T, TResult> selector)
    {
        return source.Select(selector);
    }
    
    public IEnumerable<TResult> ExecuteWhere<TResult>(IEnumerable<T> source, Func<T, bool> predicate, Func<T, TResult> selector)
    {
        return source.Where(predicate).Select(selector);
    }
    
    public async Task ExecuteAsync(IEnumerable<T> source, Func<T, Task> asyncAction)
    {
        foreach (var item in source)
        {
            await asyncAction(item);
        }
    }
    
    public string GenerateCode(CodeGenerationContext context)
    {
        return $"// Test custom strategy code for {context.PlatformTarget}";
    }
    
    public bool CanHandle(IIterationPipelineContext context)
    {
        return true; // Test strategy can handle any context
    }
    
    public int GetPriority(IIterationPipelineContext context)
    {
        return 1; // Low priority for test strategy
    }
    
    public Nexo.Core.Domain.Entities.Infrastructure.PerformanceEstimate EstimatePerformance(IterationContext context)
    {
        return new Nexo.Core.Domain.Entities.Infrastructure.PerformanceEstimate
        {
            EstimatedExecutionTimeMs = context.DataSize * 0.1,
            EstimatedMemoryUsageMB = context.DataSize * 0.001,
            Confidence = 0.8,
            PerformanceScore = 0.7,
            MeetsRequirements = true
        };
    }
}