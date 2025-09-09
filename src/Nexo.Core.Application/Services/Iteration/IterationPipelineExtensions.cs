using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Services.Iteration.Strategies;
using Nexo.Core.Domain.Entities.Iteration;

namespace Nexo.Core.Application.Services.Iteration;

/// <summary>
/// Extension methods for integrating iteration strategies with Nexo pipeline
/// </summary>
public static class IterationPipelineExtensions
{
    /// <summary>
    /// Add iteration strategy services to DI container
    /// </summary>
    public static IServiceCollection AddIterationStrategies(this IServiceCollection services)
    {
        // Core services
        services.AddSingleton<Nexo.Core.Domain.Interfaces.Infrastructure.IIterationStrategySelector, IterationStrategySelector>();
        services.AddSingleton<Nexo.Core.Domain.Entities.Infrastructure.RuntimeEnvironmentProfile>(provider => 
            RuntimeEnvironmentDetector.DetectCurrent());
        
        // Strategy implementations
        services.AddTransient(typeof(ForLoopStrategy<>));
        services.AddTransient(typeof(ForeachStrategy<>));
        services.AddTransient(typeof(LinqStrategy<>));
        services.AddTransient(typeof(ParallelLinqStrategy<>));
        
        // Platform-specific strategies
        services.AddTransient(typeof(UnityOptimizedStrategy<>));
        services.AddTransient(typeof(WasmOptimizedStrategy<>));
        
        // Code generation integration - will be added in Feature.AI project
        // services.AddTransient<IIterationCodeGenerator, IterationCodeGenerator>();
        
        return services;
    }
    
    /// <summary>
    /// Pipeline behavior for automatic strategy selection
    /// </summary>
    public static IPipelineBuilder<TRequest, TResponse> UseOptimalIteration<TRequest, TResponse>(
        this IPipelineBuilder<TRequest, TResponse> builder)
        where TRequest : IIterationRequest
    {
        return builder.Use(async (context, next) =>
        {
            var selector = context.ServiceProvider.GetRequiredService<Nexo.Core.Domain.Interfaces.Infrastructure.IIterationStrategySelector>();
            
            if (context.Request is IIterationRequest iterationRequest)
            {
                var strategy = selector.SelectStrategy<object>(iterationRequest.IterationContext);
                context.Properties["SelectedIterationStrategy"] = strategy;
            }
            
            return await next();
        });
    }
}

/// <summary>
/// Marker interface for requests that need iteration strategy selection
/// </summary>
public interface IIterationRequest
{
    IterationContext IterationContext { get; }
}

/// <summary>
/// Pipeline builder interface for extension methods
/// </summary>
public interface IPipelineBuilder<TRequest, TResponse>
{
    IPipelineBuilder<TRequest, TResponse> Use(Func<PipelineContext<TRequest, TResponse>, Func<Task<TResponse>>, Task<TResponse>> middleware);
}

/// <summary>
/// Pipeline context for middleware
/// </summary>
public class PipelineContext<TRequest, TResponse>
{
    public TRequest Request { get; set; } = default!;
    public IServiceProvider ServiceProvider { get; set; } = default!;
    public Dictionary<string, object> Properties { get; set; } = new();
}

/// <summary>
/// Iteration code generator interface
/// </summary>
public interface IIterationCodeGenerator
{
    Task<string> GenerateOptimalIterationCodeAsync(IterationCodeRequest request);
    Task<IEnumerable<string>> GenerateMultiplePlatformIterationsAsync(IterationCodeRequest request);
}

/// <summary>
/// Iteration code request model
/// </summary>
public record IterationCodeRequest
{
    public IterationContext Context { get; init; } = new();
    public CodeGenerationContext CodeGeneration { get; init; } = new();
    public bool UseAIEnhancement { get; init; } = true;
    public IEnumerable<PlatformTarget> TargetPlatforms { get; init; } = Array.Empty<PlatformTarget>();
}
