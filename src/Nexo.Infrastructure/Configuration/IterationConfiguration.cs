using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Nexo.Core.Domain.Entities.Iteration;

namespace Nexo.Infrastructure.Configuration;

/// <summary>
/// Configuration for iteration strategy system
/// </summary>
public class IterationConfiguration
{
    /// <summary>
    /// Whether to enable automatic iteration optimization
    /// </summary>
    public bool EnableAutoOptimization { get; set; } = true;
    
    /// <summary>
    /// Default optimization level for iteration strategies
    /// </summary>
    public PerformanceLevel DefaultOptimizationLevel { get; set; } = PerformanceLevel.Balanced;
    
    /// <summary>
    /// Platform-specific strategy preferences
    /// </summary>
    public Dictionary<string, string> PlatformPreferences { get; set; } = new()
    {
        { "Unity", "Nexo.UnityOptimized" },
        { "DotNet", "Nexo.ForLoop" },
        { "Web", "Nexo.ForLoop" },
        { "Mobile", "Nexo.ForLoop" },
        { "Server", "Nexo.ParallelLinq" },
        { "WebAssembly", "Nexo.ForLoop" },
        { "JavaScript", "Nexo.ForLoop" },
        { "Swift", "Nexo.ForLoop" },
        { "Kotlin", "Nexo.ForLoop" }
    };
    
    /// <summary>
    /// Benchmark configuration
    /// </summary>
    public BenchmarkConfiguration Benchmark { get; set; } = new();
    
    /// <summary>
    /// Performance thresholds for strategy selection
    /// </summary>
    public PerformanceThresholds PerformanceThresholds { get; set; } = new();
    
    /// <summary>
    /// Strategy-specific configuration
    /// </summary>
    public Dictionary<string, StrategyConfiguration> StrategyConfigurations { get; set; } = new();
    
    /// <summary>
    /// Environment-specific overrides
    /// </summary>
    public Dictionary<string, IterationConfiguration> EnvironmentOverrides { get; set; } = new();
}

/// <summary>
/// Benchmark configuration
/// </summary>
public class BenchmarkConfiguration
{
    /// <summary>
    /// Default sample size for benchmarking
    /// </summary>
    public int DefaultSampleSize { get; set; } = 10000;
    
    /// <summary>
    /// Number of iterations for benchmarking
    /// </summary>
    public int Iterations { get; set; } = 5;
    
    /// <summary>
    /// Whether to enable warm-up runs
    /// </summary>
    public bool EnableWarmup { get; set; } = true;
    
    /// <summary>
    /// Number of warm-up iterations
    /// </summary>
    public int WarmupIterations { get; set; } = 2;
    
    /// <summary>
    /// Timeout for benchmark operations in milliseconds
    /// </summary>
    public int TimeoutMs { get; set; } = 30000;
    
    /// <summary>
    /// Whether to include memory usage in benchmarks
    /// </summary>
    public bool IncludeMemoryUsage { get; set; } = true;
    
    /// <summary>
    /// Whether to include CPU usage in benchmarks
    /// </summary>
    public bool IncludeCpuUsage { get; set; } = false;
}

/// <summary>
/// Performance thresholds for strategy selection
/// </summary>
public class PerformanceThresholds
{
    /// <summary>
    /// Maximum acceptable execution time in milliseconds
    /// </summary>
    public int MaxExecutionTimeMs { get; set; } = 1000;
    
    /// <summary>
    /// Maximum acceptable memory usage in MB
    /// </summary>
    public int MaxMemoryUsageMB { get; set; } = 100;
    
    /// <summary>
    /// Minimum performance score for strategy selection
    /// </summary>
    public double MinPerformanceScore { get; set; } = 70.0;
    
    /// <summary>
    /// Minimum confidence level for strategy selection
    /// </summary>
    public double MinConfidenceLevel { get; set; } = 0.8;
    
    /// <summary>
    /// Data size thresholds for strategy selection
    /// </summary>
    public DataSizeThresholds DataSizeThresholds { get; set; } = new();
}

/// <summary>
/// Data size thresholds for strategy selection
/// </summary>
public class DataSizeThresholds
{
    /// <summary>
    /// Small dataset threshold
    /// </summary>
    public int Small { get; set; } = 100;
    
    /// <summary>
    /// Medium dataset threshold
    /// </summary>
    public int Medium { get; set; } = 1000;
    
    /// <summary>
    /// Large dataset threshold
    /// </summary>
    public int Large { get; set; } = 10000;
    
    /// <summary>
    /// Very large dataset threshold
    /// </summary>
    public int VeryLarge { get; set; } = 100000;
}

/// <summary>
/// Strategy-specific configuration
/// </summary>
public class StrategyConfiguration
{
    /// <summary>
    /// Whether this strategy is enabled
    /// </summary>
    public bool Enabled { get; set; } = true;
    
    /// <summary>
    /// Default priority for this strategy
    /// </summary>
    public int DefaultPriority { get; set; } = 50;
    
    /// <summary>
    /// Platform-specific priorities
    /// </summary>
    public Dictionary<string, int> PlatformPriorities { get; set; } = new();
    
    /// <summary>
    /// Data size range where this strategy is optimal
    /// </summary>
    public (int Min, int Max) OptimalDataSizeRange { get; set; } = (0, int.MaxValue);
    
    /// <summary>
    /// Whether this strategy supports parallelization
    /// </summary>
    public bool SupportsParallelization { get; set; } = false;
    
    /// <summary>
    /// Whether this strategy is suitable for real-time scenarios
    /// </summary>
    public bool SuitableForRealTime { get; set; } = false;
    
    /// <summary>
    /// Custom parameters for this strategy
    /// </summary>
    public Dictionary<string, object> Parameters { get; set; } = new();
}

/// <summary>
/// Configuration options for iteration system
/// </summary>
public class IterationConfigurationOptions : IOptions<IterationConfiguration>
{
    public IterationConfiguration Value { get; }
    
    public IterationConfigurationOptions(IConfiguration configuration)
    {
        Value = new IterationConfiguration();
        configuration.GetSection("Iteration").Bind(Value);
        
        // Apply environment-specific overrides
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
        if (Value.EnvironmentOverrides.TryGetValue(environment, out var environmentConfig))
        {
            ApplyEnvironmentOverride(Value, environmentConfig);
        }
    }
    
    private void ApplyEnvironmentOverride(IterationConfiguration baseConfig, IterationConfiguration environmentConfig)
    {
        if (environmentConfig.EnableAutoOptimization != baseConfig.EnableAutoOptimization)
        {
            baseConfig.EnableAutoOptimization = environmentConfig.EnableAutoOptimization;
        }
        
        if (environmentConfig.DefaultOptimizationLevel != baseConfig.DefaultOptimizationLevel)
        {
            baseConfig.DefaultOptimizationLevel = environmentConfig.DefaultOptimizationLevel;
        }
        
        // Merge platform preferences
        foreach (var kvp in environmentConfig.PlatformPreferences)
        {
            baseConfig.PlatformPreferences[kvp.Key] = kvp.Value;
        }
        
        // Merge strategy configurations
        foreach (var kvp in environmentConfig.StrategyConfigurations)
        {
            baseConfig.StrategyConfigurations[kvp.Key] = kvp.Value;
        }
        
        // Override performance thresholds if provided
        if (environmentConfig.PerformanceThresholds != null)
        {
            baseConfig.PerformanceThresholds = environmentConfig.PerformanceThresholds;
        }
        
        // Override benchmark configuration if provided
        if (environmentConfig.Benchmark != null)
        {
            baseConfig.Benchmark = environmentConfig.Benchmark;
        }
    }
}

/// <summary>
/// Extension methods for iteration configuration
/// </summary>
public static class IterationConfigurationExtensions
{
    /// <summary>
    /// Add iteration configuration to the service collection
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="configuration">Configuration instance</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddIterationConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<IterationConfiguration>(configuration.GetSection("Iteration"));
        services.AddSingleton<IOptions<IterationConfiguration>>(provider => 
            new IterationConfigurationOptions(configuration));
        
        return services;
    }
    
    /// <summary>
    /// Get iteration configuration from service provider
    /// </summary>
    /// <param name="serviceProvider">Service provider</param>
    /// <returns>Iteration configuration</returns>
    public static IterationConfiguration GetIterationConfiguration(this IServiceProvider serviceProvider)
    {
        var options = serviceProvider.GetRequiredService<IOptions<IterationConfiguration>>();
        return options.Value;
    }
    
    /// <summary>
    /// Get strategy configuration for a specific strategy
    /// </summary>
    /// <param name="config">Iteration configuration</param>
    /// <param name="strategyId">Strategy ID</param>
    /// <returns>Strategy configuration or default</returns>
    public static StrategyConfiguration GetStrategyConfiguration(this IterationConfiguration config, string strategyId)
    {
        if (config.StrategyConfigurations.TryGetValue(strategyId, out var strategyConfig))
        {
            return strategyConfig;
        }
        
        // Return default configuration
        return new StrategyConfiguration
        {
            Enabled = true,
            DefaultPriority = 50,
            OptimalDataSizeRange = (0, int.MaxValue),
            SupportsParallelization = false,
            SuitableForRealTime = false
        };
    }
    
    /// <summary>
    /// Get platform preference for strategy selection
    /// </summary>
    /// <param name="config">Iteration configuration</param>
    /// <param name="platform">Platform name</param>
    /// <returns>Preferred strategy ID or null</returns>
    public static string? GetPlatformPreference(this IterationConfiguration config, string platform)
    {
        return config.PlatformPreferences.TryGetValue(platform, out var preference) ? preference : null;
    }
    
    /// <summary>
    /// Check if a strategy is enabled
    /// </summary>
    /// <param name="config">Iteration configuration</param>
    /// <param name="strategyId">Strategy ID</param>
    /// <returns>True if strategy is enabled</returns>
    public static bool IsStrategyEnabled(this IterationConfiguration config, string strategyId)
    {
        var strategyConfig = config.GetStrategyConfiguration(strategyId);
        return strategyConfig.Enabled;
    }
    
    /// <summary>
    /// Get priority for a strategy on a specific platform
    /// </summary>
    /// <param name="config">Iteration configuration</param>
    /// <param name="strategyId">Strategy ID</param>
    /// <param name="platform">Platform name</param>
    /// <returns>Priority value</returns>
    public static int GetStrategyPriority(this IterationConfiguration config, string strategyId, string platform)
    {
        var strategyConfig = config.GetStrategyConfiguration(strategyId);
        
        if (strategyConfig.PlatformPriorities.TryGetValue(platform, out var priority))
        {
            return priority;
        }
        
        return strategyConfig.DefaultPriority;
    }
}
